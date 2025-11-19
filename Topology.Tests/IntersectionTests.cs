using Geometry;
using Geometry.Predicates;
using Xunit;

namespace Topology.Tests;

public class IntersectionTests
{
    // ----------------------------------------------------------------------
    // Coplanar: no intersection (None)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarDisjointTriangles_ReturnsNone()
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

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarDisjointTriangles_Shifted_ReturnsNone()
    {
        // Two congruent triangles in the plane z = 0, translated apart.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 1, 0);
        var a2 = new Point(0, 3, 0);

        var b0 = new Point(5, 0, 0);
        var b1 = new Point(7, 1, 0);
        var b2 = new Point(5, 3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarBoundingBoxesOverlapButDisjoint_ReturnsNone()
    {
        // Triangles lie in the same plane; their axis-aligned bounding boxes overlap,
        // but the triangles themselves are disjoint.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(4, 4, 0);
        var b1 = new Point(2, 4, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarCollinearEdgesDisjoint_ReturnsNone()
    {
        // Two triangles whose bottom edges lie on the same line (y = 0)
        // but the segments are disjoint, so there is no contact.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(3, 0, 0);
        var a2 = new Point(0, 3, 0);

        var b0 = new Point(5, 0, 0);
        var b1 = new Point(8, 0, 0);
        var b2 = new Point(5, 3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Coplanar: area intersections (Area)
    // ----------------------------------------------------------------------

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
    public void Classify_CoplanarTriangleInsideTriangle_NoSharedEdges_ReturnsArea()
    {
        // Small triangle completely inside a larger one, with no shared edges.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(8, 0, 0);
        var a2 = new Point(0, 8, 0);

        var b0 = new Point(3, 3, 0);
        var b1 = new Point(4, 3, 0);
        var b2 = new Point(3, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarSharedEdgeFullOverlap_ReturnsSegment()
    {
        // Two coplanar triangles sharing one full edge, no area overlap.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(5, 0, 0);
        var a2 = new Point(0, 5, 0);

        var b0 = new Point(5, 5, 0);
        var b1 = new Point(5, 0, 0);
        var b2 = new Point(0, 5, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarSharedVertexAndAreaOverlap_ReturnsArea()
    {
        // Triangles share the origin as a vertex and overlap in area near that corner.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(3, 1, 0);
        var b2 = new Point(1, 3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Area, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Coplanar: segment / edge intersections (Segment)
    // ----------------------------------------------------------------------

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
    public void Classify_CoplanarPartiallyOverlappingEdges_ReturnsSegment()
    {
        // Edge of A from (0,0,0) to (4,0,0); edge of B from (2,0,0) to (6,0,0).
        // Overlap is the segment [2,4] on the x-axis; triangles lie on opposite sides.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(2, 0, 0);
        var b1 = new Point(6, 0, 0);
        var b2 = new Point(6, -2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarLongerEdgeContainsShorterEdge_ReturnsSegment()
    {
        // Edge of B lies entirely inside the edge of A; triangles are on opposite sides.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 3, 0);

        var b0 = new Point(2, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(4, -3, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Segment, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Coplanar: point intersections (Point)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarSharedVertexOnly_ReturnsPoint()
    {
        // Two triangles share only the origin as a common vertex.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(-4, 0, 0);
        var b2 = new Point(0, -4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarVertexOfAOnEdgeInteriorOfB_ReturnsPoint()
    {
        // Vertex of A lies on the interior of an edge of B, with no area overlap.
        var b0 = new Point(0, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(0, 4, 0);

        var a0 = new Point(1, 3, 0); // lies on the edge from (4,0,0) to (0,4,0)
        var a1 = new Point(3, 5, 0);
        var a2 = new Point(1, 5, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarVertexOfBOnEdgeInteriorOfA_ReturnsPoint()
    {
        // Same configuration as above, with roles of A and B swapped.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(4, 0, 0);
        var a2 = new Point(0, 4, 0);

        var b0 = new Point(1, 3, 0); // lies on the edge from (4,0,0) to (0,4,0)
        var b1 = new Point(3, 5, 0);
        var b2 = new Point(1, 5, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_CoplanarCollinearEdgesMeetingAtEndpoint_ReturnsPoint()
    {
        // Two edges lie on the same line and meet at a single endpoint.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(2, 0, 0);
        var b1 = new Point(4, 0, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        Assert.True(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Non-coplanar: point and segment intersections
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_NonCoplanarPointIntersection_ReturnsPoint()
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

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
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

    [Fact]
    public void Classify_NonCoplanarSharedVertexOnly_ReturnsPoint()
    {
        // Triangles share only the origin; their planes are different.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(0, 2, 2);
        var b2 = new Point(0, -2, 2);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.Point, kind);
        Assert.True(Intersection.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Non-coplanar: no intersection (None)
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_NonCoplanarParallelPlanesDisjoint_ReturnsNone()
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

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarVertexOnPlaneOutsideOtherTriangle_ReturnsNone()
    {
        var a0 = new Point(0, 10, 10);
        var a1 = new Point(2, 10, 10);
        var a2 = new Point(0, 11, 10);

        var b0 = new Point(0, 0, 0);
        var b1 = new Point(0, 10, 0);
        var b2 = new Point(0, 0, -10);

        var triA = new Triangle(a0, a1, a2, new Point(0, 10, 11));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));
    }

    [Fact]
    public void Classify_NonCoplanarStraddlingButNoOverlap_ReturnsNone()
    {
        // Both triangles cross each other's planes, but the clipped intervals do not overlap.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 10, -1);
        var b1 = new Point(0, 10, 1);
        var b2 = new Point(0, 12, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 10, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kind = Intersection.Classify(in triA, in triB);

        Assert.Equal(IntersectionType.None, kind);
        Assert.False(Intersection.Any(in triA, in triB));
    }

    // ----------------------------------------------------------------------
    // Orientation / normal variations
    // ----------------------------------------------------------------------

    [Fact]
    public void Classify_CoplanarOrientationFlips_DoNotChangeResult()
    {
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(1, 1, 0);
        var b1 = new Point(4, 1, 0);
        var b2 = new Point(1, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        var kindBase = Intersection.Classify(in triA, in triB);

        var triAFlipped = new Triangle(a0, a2, a1, new Point(0, 0, 1));
        var triBFlipped = new Triangle(b0, b2, b1, new Point(0, 0, 1));

        var kindAFlipped = Intersection.Classify(in triAFlipped, in triB);
        var kindBFlipped = Intersection.Classify(in triA, in triBFlipped);
        var kindBothFlipped = Intersection.Classify(in triAFlipped, in triBFlipped);

        Assert.Equal(kindBase, kindAFlipped);
        Assert.Equal(kindBase, kindBFlipped);
        Assert.Equal(kindBase, kindBothFlipped);
    }

    [Fact]
    public void Classify_NonCoplanarOrientationFlips_DoNotChangeResult()
    {
        // Base configuration: non-coplanar segment intersection along y-axis.
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        Assert.False(TrianglePredicates.IsCoplanar(in triA, in triB));

        var kindBase = Intersection.Classify(in triA, in triB);

        var triAFlipped = new Triangle(a0, a2, a1, new Point(0, 0, 1));
        var triBFlipped = new Triangle(b0, b2, b1, new Point(1, 0, 0));

        var kindAFlipped = Intersection.Classify(in triAFlipped, in triB);
        var kindBFlipped = Intersection.Classify(in triA, in triBFlipped);
        var kindBothFlipped = Intersection.Classify(in triAFlipped, in triBFlipped);

        Assert.Equal(kindBase, kindAFlipped);
        Assert.Equal(kindBase, kindBFlipped);
        Assert.Equal(kindBase, kindBothFlipped);
    }
}
