using Xunit;
using Seaharp.World;
using Seaharp.World.Predicates;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class SurfacePredicatesTests
{
    [Fact]
    public void SingleTetrahedron_SurfaceIsManifold()
    {
        var a = new GPoint(0, 0, 0);
        var b = new GPoint(1, 0, 0);
        var c = new GPoint(0, 1, 0);
        var d = new GPoint(0, 0, 1);

        var shape = new Tetrahedron(a, b, c, d);
        var surface = new Surface(shape);
        Assert.True(SurfacePredicates.IsManifold(surface));
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
        var surface = new Surface(shape);
        Assert.False(SurfacePredicates.IsManifold(surface));
    }

    private sealed class TwoTetsShareEdgeShape : Shape
    {
        public TwoTetsShareEdgeShape(GPoint a, GPoint b, GPoint c, GPoint d, GPoint e, GPoint f)
        {
            // Two tetrahedra sharing only edge AB (no shared face)
            tetrahedrons.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d));
            tetrahedrons.Add(new Seaharp.Geometry.Tetrahedron(a, b, e, f));
        }
    }
}

