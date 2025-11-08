using Xunit;
using Seaharp.World;
using Seaharp.World.Predicates;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class IntersectionPredicatesTests
{
    [Fact]
    public void Tetrahedra_FarApart_NoIntersect()
    {
        var t1 = new Tetrahedron(new GPoint(0,0,0), new GPoint(5,0,0), new GPoint(0,5,0), new GPoint(0,0,5));
        var t2 = new Tetrahedron(new GPoint(100,0,0), new GPoint(105,0,0), new GPoint(100,5,0), new GPoint(100,0,5));

        Assert.False(IntersectionPredicates.Intersects(t1.Tetrahedrons[0], t2.Tetrahedrons[0]));
    }

    [Fact]
    public void Tetrahedron_VertexInside_Other_Intersect()
    {
        var outer = new Tetrahedron(new GPoint(0,0,0), new GPoint(10,0,0), new GPoint(0,10,0), new GPoint(0,0,10));
        var inner = new Tetrahedron(new GPoint(1,1,1), new GPoint(2,1,1), new GPoint(1,2,1), new GPoint(1,1,2));

        Assert.True(IntersectionPredicates.Intersects(outer.Tetrahedrons[0], inner.Tetrahedrons[0]));
    }

    [Fact]
    public void Shape_Self_NoIntersect_ForBuiltIns()
    {
        var box = new Box(4,3,2);
        var sphere = new Sphere(5, subdivisions: 1);
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);

        Assert.False(IntersectionPredicates.HasSelfIntersections(box));
        Assert.False(IntersectionPredicates.HasSelfIntersections(sphere));
        Assert.False(IntersectionPredicates.HasSelfIntersections(cyl));
    }
}

