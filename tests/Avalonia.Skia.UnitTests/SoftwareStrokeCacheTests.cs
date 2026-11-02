using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class SoftwareStrokeCacheTests
{
    [Theory]
    [InlineData(PenLineCap.Flat, PenLineJoin.Bevel)]
    [InlineData(PenLineCap.Flat, PenLineJoin.Miter)]
    [InlineData(PenLineCap.Flat, PenLineJoin.Round)]
    [InlineData(PenLineCap.Round, PenLineJoin.Bevel)]
    [InlineData(PenLineCap.Round, PenLineJoin.Miter)]
    [InlineData(PenLineCap.Round, PenLineJoin.Round)]
    [InlineData(PenLineCap.Square, PenLineJoin.Bevel)]
    [InlineData(PenLineCap.Square, PenLineJoin.Miter)]
    [InlineData(PenLineCap.Square, PenLineJoin.Round)]
    public void Thick_Stroke_Matches_Native_Caps_And_Joins(PenLineCap cap, PenLineJoin join)
    {
        Assert.True(GeometryImpl.StrokeCacheEnabled, "Run cache qualification without AVALONIA_STROKE_CACHE=0.");
        var geometry = CreateGeometry();
        var pen = new ImmutablePen(0xff205090, 3, lineCap: cap, lineJoin: join);
        AssertMatchesNative(geometry, pen, Matrix.Identity, true);
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(0.99)]
    [InlineData(1)]
    [InlineData(1.00000001)]
    public void Native_Hairline_Coverage_Is_Preserved(double thickness)
    {
        var geometry = CreateGeometry();
        AssertMatchesNative(geometry, new ImmutablePen(0xff205090, thickness), Matrix.Identity, false);
    }

    [Fact]
    public void Width_Above_Native_Hairline_Threshold_Uses_Cache()
    {
        AssertMatchesNative(CreateGeometry(), new ImmutablePen(0xff205090, 1.001), Matrix.Identity, true);
    }

    [Fact]
    public void Fractional_Translation_Preserves_Coverage()
    {
        AssertMatchesNative(CreateGeometry(), new ImmutablePen(0xb0205090, 3), Matrix.CreateTranslation(.25, .75), true);
    }

    [Theory]
    [InlineData(EdgeMode.Unspecified)]
    [InlineData(EdgeMode.Aliased)]
    public void Closed_Geometry_With_A_Hole_Matches_Native(EdgeMode edgeMode)
    {
        var geometry = new StreamGeometryImpl();
        using (var path = geometry.Open())
        {
            path.SetFillRule(FillRule.EvenOdd);
            path.BeginFigure(new Point(30.25, 25.75), true);
            path.LineTo(new Point(290.25, 25.75));
            path.LineTo(new Point(290.25, 215.75));
            path.LineTo(new Point(30.25, 215.75));
            path.EndFigure(true);
            path.BeginFigure(new Point(100.25, 80.75), true);
            path.LineTo(new Point(220.25, 80.75));
            path.LineTo(new Point(220.25, 180.75));
            path.LineTo(new Point(100.25, 180.75));
            path.EndFigure(true);
        }

        var pen = new ImmutablePen(0xff205090, 3);
        using var actual = Render(geometry, pen, Matrix.Identity, true, 1, edgeMode, Brushes.SkyBlue);
        using var expected = Render(geometry, pen, Matrix.Identity, false, 1, edgeMode, Brushes.SkyBlue);
        Assert.Equal(expected.Bytes, actual.Bytes);
        Assert.Equal(SKColors.White, actual.GetPixel(160, 120));
        Assert.NotEqual(SKColors.White, actual.GetPixel(60, 60));
        Assert.Equal(1, CachedOutlineCount(geometry));
    }

    [Fact]
    public void Gradient_And_Opacity_Are_Not_Baked_Into_The_Outline()
    {
        var geometry = CreateGeometry();
        var brush = new LinearGradientBrush
        {
            GradientStops = new GradientStops { new(Colors.CornflowerBlue, 0), new(Color.FromArgb(130, 220, 30, 70), 1) }
        };
        AssertMatchesNative(geometry, new Pen(brush, 3), Matrix.Identity, true, .65);
        AssertMatchesNative(geometry, new ImmutablePen(0x902020c0, 3), Matrix.Identity, true);
    }

    [Fact]
    public void Scale_And_Dash_Retain_The_Native_Drawing_Path()
    {
        AssertMatchesNative(CreateGeometry(), new ImmutablePen(0xff205090, 3), Matrix.CreateScale(1.2, .8), false);
        AssertMatchesNative(CreateGeometry(), new Pen(Brushes.DarkBlue, 3, new DashStyle([2d, 1d], .5)), Matrix.Identity, false);
    }

    [Fact]
    public void Bounds_And_Hit_Test_Use_The_Upstream_Cache()
    {
        var geometry = CreateGeometry();
        var pen = new ImmutablePen(0xff205090, 3);
        _ = geometry.GetRenderBounds(pen);
        _ = geometry.StrokeContains(pen, new Point(40, 40));
        Assert.Equal(0, CachedOutlineCount(geometry));
    }

    [Fact]
    public void Mutation_Clears_The_Outline_And_Eviction_Preserves_Pixels()
    {
        var geometry = CreateGeometry();
        foreach (var width in new[] { 2d, 3d, 4d, 5d, 6d })
            AssertMatchesNative(geometry, new ImmutablePen(0xff205090, width), Matrix.Identity, true);
        Assert.InRange(CachedOutlineCount(geometry), 1, 4);
        using (var path = geometry.Open())
        {
            path.BeginFigure(new Point(30, 190), false);
            path.LineTo(new Point(250, 190));
            path.EndFigure(false);
        }
        Assert.Equal(0, CachedOutlineCount(geometry));
        AssertMatchesNative(geometry, new ImmutablePen(0xff205090, 3), Matrix.Identity, true);
    }

    [Fact]
    public void Eviction_On_Another_Worker_Keeps_The_Native_Path_Alive()
    {
        var geometry = CreateGeometry();
        using var first = CreateSurface();
        using var second = CreateSurface();
        using var firstContext = CreateContext(first, true);
        using var secondContext = CreateContext(second, true);
        var pen = new ImmutablePen(0xff205090, 3);
        Parallel.Invoke(
            () =>
            {
                for (var i = 0; i < 200; i++)
                {
                    firstContext.Clear(Colors.White);
                    firstContext.DrawGeometry(null, pen, geometry);
                }
            },
            () =>
            {
                for (var i = 0; i < 200; i++)
                {
                    secondContext.Clear(Colors.White);
                    secondContext.DrawGeometry(null, new ImmutablePen(0xff205090, i % 5 + 2), geometry);
                }
            });
        using var expected = Render(geometry, pen, Matrix.Identity, false, 1);
        using var actual = Pixels(first);
        Assert.Equal(expected.Bytes, actual.Bytes);
        Assert.InRange(CachedOutlineCount(geometry), 1, 4);
    }

    private static void AssertMatchesNative(StreamGeometryImpl geometry, IPen pen, Matrix matrix, bool cached, double opacity = 1)
    {
        using var actual = Render(geometry, pen, matrix, true, opacity);
        using var expected = Render(geometry, pen, matrix, false, opacity);
        Assert.Equal(expected.Bytes, actual.Bytes);
        Assert.Equal(cached, CachedOutlineCount(geometry) > 0);
    }

    private static SKBitmap Render(StreamGeometryImpl geometry, IPen pen, Matrix matrix, bool qualifiedSurface, double opacity,
        EdgeMode edgeMode = EdgeMode.Unspecified, IBrush? fill = null)
    {
        using var surface = CreateSurface();
        using (var context = CreateContext(surface, qualifiedSurface))
        {
            context.Clear(Colors.White);
            context.RenderOptions = new RenderOptions { EdgeMode = edgeMode };
            context.Transform = matrix;
            context.PushOpacity(opacity, null);
            context.DrawGeometry(fill, pen, geometry);
            context.PopOpacity();
        }
        return Pixels(surface);
    }

    private static DrawingContextImpl CreateContext(SKSurface surface, bool qualifiedSurface) =>
        new(new DrawingContextImpl.CreateInfo
        {
            Surface = qualifiedSurface ? surface : null,
            Canvas = surface.Canvas,
            Dpi = new Vector(96, 96)
        });

    private static SKSurface CreateSurface() => SKSurface.Create(new SKImageInfo(320, 240, SKColorType.Bgra8888, SKAlphaType.Premul))!;

    private static SKBitmap Pixels(SKSurface surface)
    {
        var result = new SKBitmap(new SKImageInfo(320, 240, SKColorType.Bgra8888, SKAlphaType.Premul));
        Assert.True(surface.ReadPixels(result.Info, result.GetPixels(), result.RowBytes, 0, 0));
        return result;
    }

    private static int CachedOutlineCount(GeometryImpl geometry) =>
        (typeof(GeometryImpl).GetField("_strokeFills", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(geometry) as IDictionary)?.Count ?? 0;

    private static StreamGeometryImpl CreateGeometry()
    {
        var geometry = new StreamGeometryImpl();
        using var path = geometry.Open();
        path.BeginFigure(new Point(35.25, 50.75), false);
        path.LineTo(new Point(145.75, 115.25));
        path.CubicBezierTo(new Point(260.5, 5.25), new Point(15.75, 210.5), new Point(255.25, 155.75));
        path.EndFigure(false);
        return geometry;
    }
}
