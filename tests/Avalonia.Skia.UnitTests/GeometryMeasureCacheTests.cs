using Xunit;

namespace Avalonia.Skia.UnitTests;

public class GeometryMeasureCacheTests
{
    [Fact]
    public void Populating_An_Empty_Geometry_Invalidates_Previous_Path_Measurement()
    {
        var geometry = new StreamGeometryImpl();
        Assert.Equal(0, geometry.ContourLength);

        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(0, 0), false);
            context.LineTo(new Point(30, 0));
            context.EndFigure(false);
        }

        Assert.Equal(30, geometry.ContourLength);
        Assert.True(geometry.TryGetPointAtDistance(15, out var point));
        Assert.Equal(new Point(15, 0), point);
        Assert.True(geometry.TryGetSegment(5, 15, true, out var segment));
        Assert.Equal(10, segment!.ContourLength);
    }
}
