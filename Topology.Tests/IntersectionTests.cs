using Geometry;
using Geometry.Predicates;
using Xunit;

namespace Topology.Tests;

public class IntersectionTests
{
    [Fact]
    public void Classify_CoplanarDisjointTriangles_ReturnsNone_AndPredicatesAgree()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(4, 0, 0);
        var b1 = new Point(6, 0, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyCoplanar(in triA, in triB);
        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(TriangleIntersectionType.None, intersection.Type);
        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));

        Assert.False(TrianglePredicates.HasAreaIntersectionCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasPointIntersectionCoplanar(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarOverlappingTriangles_ReturnsArea_AndPredicatesAgree()
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

        var intersection = TrianglePredicates.ClassifyCoplanar(in triA, in triB);
        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(TriangleIntersectionType.Area, intersection.Type);
        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(Intersection.Any(in triA, in triB));
        Assert.True(TrianglePredicates.HasAreaIntersectionCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasSegmentIntersectionCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasPointIntersectionCoplanar(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarSharedEdge_ReturnsSegment_AndPredicatesAgree()
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

        var intersection = TrianglePredicates.ClassifyCoplanar(in triA, in triB);
        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(TriangleIntersectionType.Segment, intersection.Type);
        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(Intersection.Any(in triA, in triB));
        Assert.False(TrianglePredicates.HasAreaIntersectionCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasSegmentIntersectionCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasPointIntersectionCoplanar(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarPointIntersection_ReturnsPoint_AndPredicatesAgree()
    {
        // Non-coplanar triangles intersect in exactly one point at (0,2,0).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(0, 2, 0);
        var a2 = new Point(2, 0, 0);

        var b0 = new Point(0, 2, 0);
        var b1 = new Point(0, 4, 0);
        var b2 = new Point(0, 2, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 2, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var intersection = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(TriangleIntersectionType.Point, intersection.Type);
        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(Intersection.Any(in triA, in triB));
        Assert.True(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarSegmentIntersection_ReturnsSegment_AndPredicatesAgree()
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

        var intersection = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(TriangleIntersectionType.Segment, intersection.Type);
        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(Intersection.Any(in triA, in triB));
        Assert.True(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
    }
}
