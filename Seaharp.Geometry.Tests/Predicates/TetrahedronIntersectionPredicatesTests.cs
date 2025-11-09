using Xunit;
using Seaharp.Geometry;
using Seaharp.Geometry.Predicates;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.Geometry.Tests.Predicates;

public class TetrahedronIntersectionPredicatesTests
{
    [Fact]
    public void Tetrahedra_FarApart_NoIntersect()
    {
        var t1 = new Tetrahedron(new GPoint(0,0,0), new GPoint(5,0,0), new GPoint(0,5,0), new GPoint(0,0,5));
        var t2 = new Tetrahedron(new GPoint(100,0,0), new GPoint(105,0,0), new GPoint(100,5,0), new GPoint(100,0,5));
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }

    [Fact]
    public void Tetrahedron_VertexInside_Other_Intersect()
    {
        var outer = new Tetrahedron(new GPoint(0,0,0), new GPoint(10,0,0), new GPoint(0,10,0), new GPoint(0,0,10));
        var inner = new Tetrahedron(new GPoint(1,1,1), new GPoint(2,1,1), new GPoint(1,2,1), new GPoint(1,1,2));
        Assert.True(TetrahedronIntersectionPredicates.Intersects(outer, inner));
    }

    [Fact]
    public void SharedFace_IsContact_NotIntersection()
    {
        var a = new GPoint(0, 0, 0);
        var b = new GPoint(2, 0, 0);
        var c = new GPoint(0, 2, 0);
        var d = new GPoint(0, 0, 1);   // above z=0
        var e = new GPoint(0, 0, -1);  // below z=0
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, c, e);
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }

    [Fact]
    public void SharedEdge_IsContact_NotIntersection()
    {
        var a = new GPoint(0, 0, 0);
        var b = new GPoint(2, 0, 0);
        var c = new GPoint(0, 2, 0);
        var d = new GPoint(0, 0, 2);
        var e = new GPoint(0, -2, 0);
        var f = new GPoint(0, 0, -2);
        var t1 = new Tetrahedron(a, b, c, d);
        var t2 = new Tetrahedron(a, b, e, f); // shares only edge (a,b)
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }

    [Fact]
    public void SharedVertex_IsContact_NotIntersection()
    {
        var a = new GPoint(0, 0, 0);
        var t1 = new Tetrahedron(a, new GPoint(1, 0, 0), new GPoint(0, 1, 0), new GPoint(0, 0, 1));
        var t2 = new Tetrahedron(a, new GPoint(-1, 0, 0), new GPoint(0, -1, 0), new GPoint(0, 0, -1));
        Assert.False(TetrahedronIntersectionPredicates.Intersects(t1, t2));
    }
}

