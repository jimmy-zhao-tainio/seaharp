using Geometry.Predicates;
using Xunit;

namespace Geometry.Tests.Predicates;

public class TriangleNonCoplanarIntersectionTests
{
    [Fact]
    public void ClassifyNonCoplanar_DisjointTriangles_ReturnsNone()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(0, 0, 4);
        var b1 = new Point(2, 0, 4);
        var b2 = new Point(0, 2, 4);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 5));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var contact = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        Assert.Equal(TriangleContactKind.None, contact.Kind);
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_PointIntersection_ReturnsPoint()
    {
        // Triangles intersect in exactly one point at (0,2,0).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(0, 2, 0);
        var a2 = new Point(2, 0, 0);

        var b0 = new Point(0, 2, 0);
        var b1 = new Point(0, 4, 0);
        var b2 = new Point(0, 2, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 2, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var contact = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        Assert.Equal(TriangleContactKind.Point, contact.Kind);
        Assert.True(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_SegmentIntersection_ReturnsSegment()
    {
        // Intersection is a segment along the y-axis from (0,0,0) to (0,1,0).
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var contact = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        Assert.True((contact.Kind & TriangleContactKind.Segment) != 0);
        Assert.True((contact.Kind & TriangleContactKind.Point) != 0);
        Assert.True(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
        Assert.True(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_SymmetricUnderSwap_ReturnsSameKind()
    {
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var contactAB = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);
        var contactBA = TrianglePredicates.ClassifyNonCoplanar(in triB, in triA);

        Assert.Equal(contactAB.Kind, contactBA.Kind);
    }

    [Fact]
    public void ClassifyNonCoplanar_VertexOnPlaneButOutsideOtherTriangle_NoIntersection()
    {
        // Triangle A lies in plane z = 10; one vertex lies on plane of B (x = 0),
        // but that vertex is outside the area of B. The triangles do not intersect.
        var a0 = new Point(0, 10, 10);
        var a1 = new Point(2, 10, 10);
        var a2 = new Point(0, 11, 10);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(0, 10, 0);
        var b2 = new Point(0, 0, -10);

        var triA = new Triangle(a0, a1, a2, new Point(0, 10, 11));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var contact = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);

        Assert.Equal(TriangleContactKind.None, contact.Kind);
        Assert.False(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
    }

    [Fact]
    public void ClassifyNonCoplanar_OrientationFlips_DoNotChangeResult()
    {
        // Base configuration: segment intersection along y-axis.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        var contactBase = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);

        // Flip winding of A and B separately; classification should not change.
        var triAFlipped = new Triangle(a0, a2, a1, new Point(0, 0, 1));
        var triBFlipped = new Triangle(b0, b2, b1, new Point(1, 0, 0));

        var contactAFlipped = TrianglePredicates.ClassifyNonCoplanar(in triAFlipped, in triB);
        var contactBFlipped = TrianglePredicates.ClassifyNonCoplanar(in triA, in triBFlipped);
        var contactBothFlipped = TrianglePredicates.ClassifyNonCoplanar(in triAFlipped, in triBFlipped);

        Assert.Equal(contactBase.Kind, contactAFlipped.Kind);
        Assert.Equal(contactBase.Kind, contactBFlipped.Kind);
        Assert.Equal(contactBase.Kind, contactBothFlipped.Kind);
    }

    [Fact]
    public void ClassifyNonCoplanar_StraddlingPlanesButNoOverlap_ReturnsNone()
    {
        // Both triangles cross each other's planes, but their clipped intervals
        // along the intersection line do not overlap, so there is no contact.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 10, -1);
        var b1 = new Point(0, 10, 1);
        var b2 = new Point(0, 12, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 10, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var contact = TrianglePredicates.ClassifyNonCoplanar(in triA, in triB);

        Assert.Equal(TriangleContactKind.None, contact.Kind);
        Assert.False(TrianglePredicates.HasPointIntersectionNonCoplanar(in triA, in triB));
        Assert.False(TrianglePredicates.HasSegmentIntersectionNonCoplanar(in triA, in triB));
    }
}
