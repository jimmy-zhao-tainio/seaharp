using System;
using System.Collections.Generic;

namespace Seaharp.Geometry.Computation;

public static class TetrahedronPredicates
{
    private static bool TriangleMatchesAny(in Seaharp.Geometry.Triangle triangle, in Seaharp.Geometry.Tetrahedron t)
        => TrianglePredicates.IsSame(triangle, t.ABC) ||
           TrianglePredicates.IsSame(triangle, t.ABD) ||
           TrianglePredicates.IsSame(triangle, t.ACD) ||
           TrianglePredicates.IsSame(triangle, t.BCD);

    public static bool SharesTriangle(in Seaharp.Geometry.Tetrahedron first, in Seaharp.Geometry.Tetrahedron second)
        => TriangleMatchesAny(first.ABC, second) ||
           TriangleMatchesAny(first.ABD, second) ||
           TriangleMatchesAny(first.ACD, second) ||
           TriangleMatchesAny(first.BCD, second);

    public static bool IsSolid(IReadOnlyList<Seaharp.Geometry.Tetrahedron> tetrahedra)
    {
        if (tetrahedra is null) throw new ArgumentNullException(nameof(tetrahedra));
        if (tetrahedra.Count == 0) return false;

        for (int i = 0; i < tetrahedra.Count; i++)
        {
            var tetrahedron = tetrahedra[i];
            if (HasSharedTriangle(tetrahedron, tetrahedra, i)) continue;
            return false;
        }
        return true;
    }

    private static bool HasSharedTriangle(in Seaharp.Geometry.Tetrahedron tetrahedron, IReadOnlyList<Seaharp.Geometry.Tetrahedron> tetrahedra, int selfIndex)
    {
        for (int j = 0; j < tetrahedra.Count; j++)
        {
            if (j == selfIndex) continue;
            if (SharesTriangle(tetrahedron, tetrahedra[j])) return true;
        }
        return false;
    }
}

