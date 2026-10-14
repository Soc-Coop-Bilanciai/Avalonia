using System;
using System.Threading.Tasks;
using SkiaSharp;

namespace Avalonia.Skia;

// Local experiment: immutable pictures are shared; each tile owns its mutable canvas and pixels.
internal sealed class ParallelRaster : IDisposable
{
    private readonly int _tileCount;
    private readonly bool _vertical;
    private readonly ParallelOptions _options;
    private Tile[]? _tiles;
    private SKImageInfo _info;
    private const int Padding = 8;

    private sealed class Tile : IDisposable
    {
        public required SKSurface Surface;
        public required SKPixmap Pixels;
        public int Left, Right, Top, Bottom, PaddedLeft, PaddedTop;
        public void Dispose() { Pixels.Dispose(); Surface.Dispose(); }
    }

    private ParallelRaster(int workers, int tileCount, bool vertical)
    {
        _tileCount = tileCount;
        _vertical = vertical;
        _options = new ParallelOptions { MaxDegreeOfParallelism = workers };
    }

    public static ParallelRaster? Create()
    {
        var value = Environment.GetEnvironmentVariable("AVALONIA_RASTER_WORKERS");
        if (string.IsNullOrEmpty(value)) return null;
        if (!int.TryParse(value, out var count) || count < 1 || count > 4)
            throw new ArgumentException("AVALONIA_RASTER_WORKERS must be 1..4.");
        var tiles = Environment.GetEnvironmentVariable("AVALONIA_RASTER_TILES") ?? "8";
        if (!int.TryParse(tiles, out var tileCount) || tileCount < 1 || tileCount > 32)
            throw new ArgumentException("AVALONIA_RASTER_TILES must be 1..32.");
        var axis = Environment.GetEnvironmentVariable("AVALONIA_RASTER_AXIS") ?? "horizontal";
        if (axis != "horizontal" && axis != "vertical")
            throw new ArgumentException("AVALONIA_RASTER_AXIS must be horizontal or vertical.");
        return new ParallelRaster(count, tileCount, axis == "vertical");
    }

    public unsafe void Render(SKPicture picture, SKImageInfo info, IntPtr destination, int rowBytes)
    {
        if (_tiles == null || !_info.Equals(info))
        {
            Dispose();
            _info = info;
            _tiles = new Tile[Math.Min(_tileCount, _vertical ? info.Width : info.Height)];
            for (var i = 0; i < _tiles.Length; i++)
            {
                var left = _vertical ? info.Width * i / _tiles.Length : 0;
                var right = _vertical ? info.Width * (i + 1) / _tiles.Length : info.Width;
                var top = _vertical ? 0 : info.Height * i / _tiles.Length;
                var bottom = _vertical ? info.Height : info.Height * (i + 1) / _tiles.Length;
                var paddedLeft = Math.Max(0, left - Padding);
                var paddedRight = Math.Min(info.Width, right + Padding);
                var paddedTop = Math.Max(0, top - Padding);
                var paddedBottom = Math.Min(info.Height, bottom + Padding);
                var tileInfo = info.WithSize(paddedRight - paddedLeft, paddedBottom - paddedTop);
                var surface = SKSurface.Create(tileInfo, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal))
                    ?? throw new InvalidOperationException("Tile allocation failed.");
                _tiles[i] = new Tile { Surface = surface, Pixels = surface.PeekPixels(),
                    Left = left, Right = right, Top = top, Bottom = bottom,
                    PaddedLeft = paddedLeft, PaddedTop = paddedTop };
            }
        }

        Parallel.For(0, _tiles.Length, _options, i =>
        {
            var tile = _tiles[i];
            var canvas = tile.Surface.Canvas;
            canvas.RestoreToCount(1);
            canvas.ResetMatrix();
            canvas.Clear(SKColors.Transparent);
            canvas.Save();
            canvas.Translate(-tile.PaddedLeft, -tile.PaddedTop);
            picture.Playback(canvas);
            canvas.Restore();
            canvas.Flush();
            var source = (byte*)tile.Pixels.GetPixels();
            for (var y = tile.Top; y < tile.Bottom; y++)
                Buffer.MemoryCopy(source + (y - tile.PaddedTop) * tile.Pixels.RowBytes
                    + (tile.Left - tile.PaddedLeft) * info.BytesPerPixel,
                    (byte*)destination + y * rowBytes + tile.Left * info.BytesPerPixel,
                    rowBytes - tile.Left * info.BytesPerPixel, (tile.Right - tile.Left) * info.BytesPerPixel);
        });
        // shortcut: full-frame repaint and a 8-pixel filter halo are required; add damage history and
        // effect-bound analysis before enabling this prototype for arbitrary application content.
    }

    public void Dispose()
    {
        if (_tiles != null)
            foreach (var tile in _tiles) tile?.Dispose();
        _tiles = null;
    }
}
