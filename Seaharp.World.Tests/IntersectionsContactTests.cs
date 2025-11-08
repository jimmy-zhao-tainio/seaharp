using Xunit;
using Seaharp.World;
using Seaharp.World.Predicates;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class IntersectionsContactTests
{
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
        Assert.False(IntersectionPredicates.Intersects(t1.Tetrahedrons[0], t2.Tetrahedrons[0]));
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
        Assert.False(IntersectionPredicates.Intersects(t1.Tetrahedrons[0], t2.Tetrahedrons[0]));
    }

    [Fact]
    public void SharedVertex_IsContact_NotIntersection()
    {
        var a = new GPoint(0, 0, 0);
        var t1 = new Tetrahedron(a, new GPoint(1, 0, 0), new GPoint(0, 1, 0), new GPoint(0, 0, 1));
        var t2 = new Tetrahedron(a, new GPoint(-1, 0, 0), new GPoint(0, -1, 0), new GPoint(0, 0, -1));
        Assert.False(IntersectionPredicates.Intersects(t1.Tetrahedrons[0], t2.Tetrahedrons[0]));
    }
}

