using System;
using Avalonia.Media;
using Avalonia.Skia.Helpers;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class BoxShadowFidelityTests
{
    // The navigator uses 0 0 12 0 #AA000000 on a square, 60-DIP-high bar.
    [Theory]
    [InlineData(12, 0, 0, 0, 0, 1, 1, 1)]
    [InlineData(12, 0, 0, 0, 16, 1, 1, 1)]
    [InlineData(72, 0, 0, 0, 0, 1, 1, 1)]
    [InlineData(12, 0.5, 3.25, 2, 0, 1, 1, 1)]
    [InlineData(12, -2.25, 4.5, -3, 16, 1, 1, 0.65)]
    [InlineData(12, 0, 0, 0, 0, 1.2, 1.7, 1)]
    [InlineData(12, 0, 0, 0, 16, 1.5, 1.5, 0.65)]
    [InlineData(0, 0, 3, 2, 0, 1, 1, 1)]
    public void Outer_Shadow_Matches_Upstream_Image_Filter(
        double blur, double offsetX, double offsetY, double spread, double radius,
        double scaleX, double scaleY, double opacity)
    {
        var info = new SKImageInfo(1024, 600, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var actual = new SKBitmap(info);
        using var expected = new SKBitmap(info);
        var rect = new Rect(0, 0, 700, 60);
        var shadow = new BoxShadow
        {
            Blur = blur,
            OffsetX = offsetX,
            OffsetY = offsetY,
            Spread = spread,
            Color = Color.FromArgb(170, 0, 0, 0)
        };

        using (var canvas = new SKCanvas(actual))
        using (var context = DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96)))
        {
            canvas.Clear(new SKColor(245, 245, 245));
            context.Transform = Matrix.CreateScale(scaleX, scaleY);
            context.PushOpacity(opacity, null);
            context.DrawRectangle(Brushes.LightGray, null, new RoundedRect(rect, radius), new BoxShadows(shadow));
            context.PopOpacity();
        }

        // Keep upstream's image-filter semantics as the reference, including alpha
        // quantization, clipping before offset, spread and device-space scaling.
        using (var canvas = new SKCanvas(expected))
        {
            canvas.Clear(new SKColor(245, 245, 245));
            canvas.Scale((float)scaleX, (float)scaleY);
            var sigma = blur <= 0 ? 0 : 0.288675f * (float)blur + 0.5f;
            using var filter = SKImageFilter.CreateBlur(sigma, sigma);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, (byte)(170 * opacity)),
                ImageFilter = filter
            };
            using var original = new SKRoundRect(new SKRect(0, 0, 700, 60), (float)radius);
            using var expanded = new SKRoundRect();
            expanded.SetRectRadii(original.Rect, original.Radii);
            expanded.Inflate((float)spread, (float)spread);
            canvas.Save();
            if (radius > 0)
                canvas.ClipRoundRect(original, SKClipOperation.Difference, true);
            else
                canvas.ClipRect(original.Rect, SKClipOperation.Difference);
            canvas.Translate((float)offsetX, (float)offsetY);
            if (radius > 0)
                canvas.DrawRoundRect(expanded, paint);
            else
                canvas.DrawRect(expanded.Rect, paint);
            canvas.Restore();
            using var fill = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(211, 211, 211, (byte)(255 * opacity))
            };
            if (radius > 0)
                canvas.DrawRoundRect(original, fill);
            else
                canvas.DrawRect(original.Rect, fill);
        }

        var actualPixels = actual.Bytes;
        var expectedPixels = expected.Bytes;
        for (var i = 0; i < actualPixels.Length; i++)
            if (actualPixels[i] != expectedPixels[i])
                Assert.Fail($"Shadow pixel differs at byte {i}: expected {expectedPixels[i]}, actual {actualPixels[i]}.");
    }
}
