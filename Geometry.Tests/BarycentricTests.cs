using Geometry;
using Xunit;

namespace Geometry.Tests;

public class BarycentricTests
{
    [Fact]
    public void IsInsideInclusive_BasicCases()
    {
        var center = new Barycentric(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0);
        var vertex = new Barycentric(1.0, 0.0, 0.0);
        var edgeMidpoint = new Barycentric(0.5, 0.5, 0.0);
        var outside = new Barycentric(-0.1, 0.5, 0.6);

        Assert.True(center.IsInsideInclusive());
        Assert.True(vertex.IsInsideInclusive());
        Assert.True(edgeMidpoint.IsInsideInclusive());
        Assert.False(outside.IsInsideInclusive());
    }

    [Fact]
    public void Triangle_BarycentricRoundTrip_PreservesGridPoints()
    {
        var p0 = new Point(0, 0, 0);
        var p1 = new Point(4, 0, 0);
        var p2 = new Point(0, 4, 0);

        var tri = new Triangle(p0, p1, p2, new Point(0, 0, 1));

        var points = new[]
        {
            p0,
            p1,
            p2,
            new Point(2, 0, 0), // edge midpoint on P0-P1
            new Point(0, 2, 0), // edge midpoint on P0-P2
            new Point(1, 1, 0)  // interior point
        };

        foreach (var p in points)
        {
            var bary = tri.ToBarycentric(p);
            Assert.True(bary.IsInsideInclusive(1e-6));

            var real = tri.FromBarycentric(in bary);
            var snapped = GridRounding.Snap(real);

            Assert.Equal(p.X, snapped.X);
            Assert.Equal(p.Y, snapped.Y);
            Assert.Equal(p.Z, snapped.Z);
        }
    }
}

