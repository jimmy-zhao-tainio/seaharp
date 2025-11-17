using System.Collections.Generic;
using Point2D = Geometry.Predicates.Internal.TriangleProjection2D.Point2D;

namespace Geometry.Predicates.Internal;

internal static class TriangleCoplanarIntersection
{
    internal static TriangleContact Classify(in Triangle a, in Triangle b)
    {
        // Project both triangles to 2D (dropping the dominant normal axis),
        // then build the set of candidate intersection points:
        // - vertices of A inside B
        // - vertices of B inside A
        // - edge/edge intersection points
        // Then decide intersection dimension from these candidates.

        int axis = TriangleProjection2D.ChooseProjectionAxis(a.Normal);

        var a0 = TriangleProjection2D.ProjectTo2D(a.P0, axis);
        var a1 = TriangleProjection2D.ProjectTo2D(a.P1, axis);
        var a2 = TriangleProjection2D.ProjectTo2D(a.P2, axis);

        var b0 = TriangleProjection2D.ProjectTo2D(b.P0, axis);
        var b1 = TriangleProjection2D.ProjectTo2D(b.P1, axis);
        var b2 = TriangleProjection2D.ProjectTo2D(b.P2, axis);

        var candidates = new List<Point2D>(12);

        // Vertices of A inside B (including on boundary)
        TriangleProjection2D.AddIfInsideTriangle(a0, b0, b1, b2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(a1, b0, b1, b2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(a2, b0, b1, b2, candidates);

        // Vertices of B inside A
        TriangleProjection2D.AddIfInsideTriangle(b0, a0, a1, a2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(b1, a0, a1, a2, candidates);
        TriangleProjection2D.AddIfInsideTriangle(b2, a0, a1, a2, candidates);

        // Edge-edge intersections
        var aVerts = new[] { a0, a1, a2 };
        var bVerts = new[] { b0, b1, b2 };
        for (int i = 0; i < 3; i++)
        {
            var aStart = aVerts[i];
            var aEnd = aVerts[(i + 1) % 3];
            for (int j = 0; j < 3; j++)
            {
                var bStart = bVerts[j];
                var bEnd = bVerts[(j + 1) % 3];
                if (TriangleProjection2D.TrySegmentIntersection(aStart, aEnd, bStart, bEnd, out var intersection))
                {
                    TriangleProjection2D.AddUnique(candidates, intersection);
                }
            }
        }

        if (candidates.Count == 0)
            return new TriangleContact(TriangleContactKind.None);

        if (TriangleProjection2D.HasNonCollinearTriple(candidates))
        {
            // 2D overlap region has positive area.
            return new TriangleContact(TriangleContactKind.Area | TriangleContactKind.Segment | TriangleContactKind.Point);
        }

        // Intersection is 0D or 1D (point or segment). For now we do not
        // distinguish between them and treat as both.
        return new TriangleContact(TriangleContactKind.Point | TriangleContactKind.Segment);
    }
}

