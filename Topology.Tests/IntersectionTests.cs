using Geometry;
using Geometry.Predicates;
using Xunit;

namespace Topology.Tests;

public class IntersectionTests
{
    [Fact]
    public void Classify_CoplanarOverlappingTriangles_ReturnsArea()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(1, 1, 0);
        var b1 = new Point(4, 1, 0);
        var b2 = new Point(1, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarSharedEdge_ReturnsSegment()
    {
        // Two coplanar triangles sharing the edge from (0,0,0) to (4,0,0).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(4, -4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarSegmentIntersection_ReturnsSegment()
    {
        // Non-coplanar triangles intersect along a segment on the y-axis.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }
}
