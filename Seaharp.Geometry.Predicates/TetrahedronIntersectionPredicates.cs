using System;
using Seaharp.Geometry;

namespace Seaharp.Geometry.Predicates;

public static class TetrahedronIntersectionPredicates
{
    // Strict volumetric overlap detection for tetrahedra.
    // Contacts along a single face/edge/vertex are NOT counted as intersections.
    public static bool Intersects(in Tetrahedron a, in Tetrahedron b)
    {
        if (!BoundingBoxOverlap(a, b)) return false;

        // Any vertex of A strictly inside B?
        if (PointInsideTetrahedronStrict(b, a.A) || PointInsideTetrahedronStrict(b, a.B) ||
            PointInsideTetrahedronStrict(b, a.C) || PointInsideTetrahedronStrict(b, a.D))
            return true;

        // Any vertex of B strictly inside A?
        if (PointInsideTetrahedronStrict(a, b.A) || PointInsideTetrahedronStrict(a, b.B) ||
            PointInsideTetrahedronStrict(a, b.C) || PointInsideTetrahedronStrict(a, b.D))
            return true;

        // FIXME: Naive implementation, silent returns are dangerous!
        // Note: does not yet detect face-face crossings without vertices strictly inside.
        return false;
    }

    // FIXME: Use BoundingBoxPredicates.TetrahedronOverlap?
    private static bool BoundingBoxOverlap(in Tetrahedron a, in Tetrahedron b)
    {
        (long minX, long minY, long minZ, long maxX, long maxY, long maxZ) Box(in Tetrahedron t)
        {
            long minX = Math.Min(Math.Min(t.A.X, t.B.X), Math.Min(t.C.X, t.D.X));
            long minY = Math.Min(Math.Min(t.A.Y, t.B.Y), Math.Min(t.C.Y, t.D.Y));
            long minZ = Math.Min(Math.Min(t.A.Z, t.B.Z), Math.Min(t.C.Z, t.D.Z));
            long maxX = Math.Max(Math.Max(t.A.X, t.B.X), Math.Max(t.C.X, t.D.X));
            long maxY = Math.Max(Math.Max(t.A.Y, t.B.Y), Math.Max(t.C.Y, t.D.Y));
            long maxZ = Math.Max(Math.Max(t.A.Z, t.B.Z), Math.Max(t.C.Z, t.D.Z));
            return (minX, minY, minZ, maxX, maxY, maxZ);
        }

        var ab = Box(a); var bb = Box(b);
        if (ab.maxX <= bb.minX || bb.maxX <= ab.minX) return false;
        if (ab.maxY <= bb.minY || bb.maxY <= ab.minY) return false;
        if (ab.maxZ <= bb.minZ || bb.maxZ <= ab.minZ) return false;
        return true;
    }

    // FIXME: Possibly TetrahedronPredicates.IsPointInsideStrict?
    private static bool PointInsideTetrahedronStrict(in Tetrahedron t, in Point p)
    {
        // Do not abbreviate: clearer name than "Neg".
        static bool IsOnNegativeSide(in Triangle tri, in Point q)
            => Plane.FromTriangle(tri).Side(q, Tolerances.PlaneSideEpsilon) < 0;

        return IsOnNegativeSide(t.ABC, p) && IsOnNegativeSide(t.ABD, p) && IsOnNegativeSide(t.ACD, p) && IsOnNegativeSide(t.BCD, p);
    }
}
