using Xunit;
using Seaharp.World;
using Seaharp.Topology;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class ClosedSurfacePredicatesTests
{
    [Fact]
    public void SingleTetrahedron_SurfaceIsManifold()
    {
        var a = new GPoint(0, 0, 0);
        var b = new GPoint(1, 0, 0);
        var c = new GPoint(0, 1, 0);
        var d = new GPoint(0, 0, 1);

        var shape = new Tetrahedron(a, b, c, d);
        var surface = shape.ExtractClosedSurface();
        Assert.True(ClosedSurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void EdgeSharedTwoTetrahedra_SurfaceIsNotManifold()
    {
        var a = new GPoint(0, 0, 0);
        var b = new GPoint(1, 0, 0);
        var c = new GPoint(0, 1, 0);
        var d = new GPoint(0, 0, 1);
        var e = new GPoint(0, -1, 0);
        var f = new GPoint(0, 0, -1);

        var shape = new TwoTetsShareEdgeShape(a, b, c, d, e, f);
        var surface = shape.ExtractClosedSurface();
        Assert.False(ClosedSurfacePredicates.IsManifold(surface));
    }
}







