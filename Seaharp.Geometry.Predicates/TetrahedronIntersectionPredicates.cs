using System;
using Seaharp.Geometry;

namespace Seaharp.Geometry.Predicates;

public static class TetrahedronIntersectionPredicates
{
    // Strict volumetric overlap detection for tetrahedra.
    // Contacts along a single face/edge/vertex are NOT counted as intersections.
    public static bool Intersects(in Tetrahedron a, in Tetrahedron b)
    {
        if (!AabbOverlap(a, b)) return false;

        // Any vertex of A strictly inside B?
        if (PointInsideTetStrict(b, a.A) || PointInsideTetStrict(b, a.B) ||
            PointInsideTetStrict(b, a.C) || PointInsideTetStrict(b, a.D))
            return true;

        // Any vertex of B strictly inside A?
        if (PointInsideTetStrict(a, b.A) || PointInsideTetStrict(a, b.B) ||
            PointInsideTetStrict(a, b.C) || PointInsideTetStrict(a, b.D))
            return true;

        // Note: does not yet detect face-face crossings without vertices strictly inside.
        return false;
    }

    private static bool AabbOverlap(in Tetrahedron a, in Tetrahedron b)
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

    private static bool PointInsideTetStrict(in Tetrahedron t, in Point p)
    {
        static bool Neg(in Triangle tri, in Point q)
            => Plane.FromTriangle(tri).Side(q, Tolerances.PlaneSideEpsilon) < 0;

        return Neg(t.ABC, p) && Neg(t.ABD, p) && Neg(t.ACD, p) && Neg(t.BCD, p);
    }
}

