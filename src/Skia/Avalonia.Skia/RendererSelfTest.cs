using System;
using Avalonia.Media;
using Avalonia.Rendering;
using SkiaSharp;

namespace Avalonia.Skia;

internal static class RendererSelfTest
{
    public static void Run()
    {
        VerifyProfile();
        var geometry = new StreamGeometryImpl();
        static void SetLine(StreamGeometryImpl geometry, double end)
        {
            using var ctx = geometry.Open();
            ctx.BeginFigure(new Point(0, 0), false);
            ctx.LineTo(new Point(end, 0));
            ctx.EndFigure(false);
        }
        SetLine(geometry, 10);
        var pen = new Pen(Brushes.Red, 2);
        using var bitmap = new SKBitmap(64, 32);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { Color = SKColors.Red };
        for (var i = 0; i < 2; i++)
            if (!geometry.TryDrawCachedStroke(canvas, pen, paint))
                throw new Exception("Stroke cache drawing failed.");
        SetLine(geometry, 30);
        bitmap.Erase(SKColors.Transparent);
        if (!geometry.TryDrawCachedStroke(canvas, pen, paint) || bitmap.GetPixel(25, 0).Alpha == 0)
            throw new Exception("Geometry mutation did not invalidate the stroke cache.");
        if (geometry.TryDrawCachedStroke(canvas, new Pen(Brushes.Red, 0), paint)
            || geometry.TryDrawCachedStroke(canvas, new Pen(Brushes.Red, 2, new DashStyle(new[] { 1d, 1d }, 0)), paint))
            throw new Exception("Hairlines and dashed strokes must retain the original drawing path.");
        System.Threading.Tasks.Parallel.Invoke(
            () => { for (var i = 0; i < 2000; i++) geometry.TryDrawCachedStroke(canvas, pen, paint); },
            () => { for (var i = 0; i < 2000; i++) geometry.GetRenderBounds(new Pen(Brushes.Blue, i % 10 + 1)); });
        Console.WriteLine("RENDERER SELF-TEST PASS: drawing, invalidation, fallback, concurrent cache eviction.");
        var timer = new SleepLoopRenderTimer(60) { FrameRateLimit = 24 };
        timer.DesiredFps = 120;
        if (timer.DesiredFps != 24) throw new Exception("Display change lost the frame-rate limit.");
        timer.DesiredFps = 12;
        if (timer.DesiredFps != 12) throw new Exception("Frame-rate limit raised the platform rate.");
        Console.WriteLine("TIMER SELF-TEST PASS: display-rate changes preserve the cap.");
        VerifyTiles();
    }

    private static void VerifyProfile()
    {
        var qualified = OperatingSystem.IsLinux() && System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture
            == System.Runtime.InteropServices.Architecture.Arm64;
        var names = new[] { "AVALONIA_MASK_SHADOWS", "AVALONIA_STROKE_CACHE", "AVALONIA_RASTER_WORKERS",
            "AVALONIA_RASTER_TILES", "AVALONIA_RASTER_AXIS", "AVALONIA_RENDER_FPS" };
        var expected = new[] { "1", "1", "3", "4", "horizontal", "24" };
        var previous = Array.ConvertAll(names, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in names) Environment.SetEnvironmentVariable(name, null);
            BilanciaiSoftwareProfile.Configure();
            for (var i = 0; i < names.Length; i++)
                if (Environment.GetEnvironmentVariable(names[i]) != (qualified ? expected[i] : null))
                    throw new Exception($"Profile default differs: {names[i]}.");
            Environment.SetEnvironmentVariable("AVALONIA_RASTER_WORKERS", "2");
            BilanciaiSoftwareProfile.Configure();
            if (Environment.GetEnvironmentVariable("AVALONIA_RASTER_WORKERS") != "2")
                throw new Exception("Explicit profile override was lost.");
        }
        finally
        {
            for (var i = 0; i < names.Length; i++) Environment.SetEnvironmentVariable(names[i], previous[i]);
        }
        Console.WriteLine("PROFILE SELF-TEST PASS: qualified defaults and explicit overrides.");
    }

    private static unsafe void VerifyTiles()
    {
        var info = new SkiaSharp.SKImageInfo(101, 79, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
        var stride = info.Width * 4 + 16;
        var expected = new byte[stride * info.Height];
        var actual = new byte[expected.Length];
        using var recorder = new SkiaSharp.SKPictureRecorder();
        var canvas = recorder.BeginRecording(new SkiaSharp.SKRect(0, 0, info.Width, info.Height), true);
        canvas.Clear(SkiaSharp.SKColors.DarkBlue);
        using var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Orange, IsAntialias = true };
        using var blur = SkiaSharp.SKMaskFilter.CreateBlur(SkiaSharp.SKBlurStyle.Normal, 1.65f);
        paint.MaskFilter = blur;
        canvas.DrawRoundRect(new SkiaSharp.SKRect(12.3f, 13.7f, 85.5f, 65.2f), 7, 7, paint);
        using var picture = recorder.EndRecording();
        var oldWorkers = Environment.GetEnvironmentVariable("AVALONIA_RASTER_WORKERS");
        var oldTiles = Environment.GetEnvironmentVariable("AVALONIA_RASTER_TILES");
        var oldAxis = Environment.GetEnvironmentVariable("AVALONIA_RASTER_AXIS");
        try
        {
            fixed (byte* expectedAddress = expected, actualAddress = actual)
            {
                using var surface = SkiaSharp.SKSurface.Create(info, (IntPtr)expectedAddress, stride,
                    new SkiaSharp.SKSurfaceProperties(SkiaSharp.SKPixelGeometry.RgbHorizontal));
                picture.Playback(surface.Canvas);
                surface.Canvas.Flush();
                foreach (var axis in new[] { "horizontal", "vertical" })
                foreach (var count in new[] { 1, 3, 4, 6, 8, 12 })
                {
                    Environment.SetEnvironmentVariable("AVALONIA_RASTER_WORKERS", "3");
                    Environment.SetEnvironmentVariable("AVALONIA_RASTER_TILES", count.ToString());
                    Environment.SetEnvironmentVariable("AVALONIA_RASTER_AXIS", axis);
                    using var raster = ParallelRaster.Create()!;
                    for (var repeat = 0; repeat < 2; repeat++)
                    {
                        Array.Fill(actual, (byte)0xa5);
                        raster.Render(picture, info, (IntPtr)actualAddress, stride);
                        for (var y = 0; y < info.Height; y++)
                        for (var x = 0; x < stride; x++)
                        {
                            var offset = y * stride + x;
                            if (x < info.Width * 4 ? Math.Abs(actual[offset] - expected[offset]) > 1 : actual[offset] != 0xa5)
                                throw new Exception($"Tile pixels or row padding differ: {axis}, tiles={count}, x={x}, y={y}.");
                        }
                    }
                }
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("AVALONIA_RASTER_WORKERS", oldWorkers);
            Environment.SetEnvironmentVariable("AVALONIA_RASTER_TILES", oldTiles);
            Environment.SetEnvironmentVariable("AVALONIA_RASTER_AXIS", oldAxis);
        }
        Console.WriteLine("TILE SELF-TEST PASS: both axes, six partitions, odd dimensions, repeated buffers, blur, row padding.");
    }
}
