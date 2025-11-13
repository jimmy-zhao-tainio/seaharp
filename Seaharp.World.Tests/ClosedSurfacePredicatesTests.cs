using Xunit;
using Seaharp.World;
using Seaharp.Topology;
using Seaharp.Geometry;

namespace Seaharp.World.Tests;

public class ClosedSurfacePredicatesTests
{
    [Fact]
    public void SingleTetrahedron_SurfaceIsManifold()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d = new Point(0, 0, 1);

        var shape = new Tetrahedron(a, b, c, d);
        var surface = ClosedSurface.FromTetrahedra(shape.Tetrahedra);
        Assert.True(ClosedSurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void EdgeSharedTwoTetrahedra_SurfaceIsNotManifold()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(1, 0, 0);
        var c = new Point(0, 1, 0);
        var d = new Point(0, 0, 1);
        var e = new Point(0, -1, 0);
        var f = new Point(0, 0, -1);

        var shape = new TwoTetsShareEdgeShape(a, b, c, d, e, f);
        var surface = ClosedSurface.FromTetrahedra(shape.Tetrahedra);
        Assert.False(ClosedSurfacePredicates.IsManifold(surface));
    }
}







