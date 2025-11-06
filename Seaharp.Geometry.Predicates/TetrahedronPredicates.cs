using System;
using System.Collections.Generic;

namespace Seaharp.Geometry.Predicates;

public static class TetrahedronPredicates
{
    private static bool TriangleMatchesAny(in Seaharp.Geometry.Tetrahedron.Triangle triangle, in Seaharp.Geometry.Tetrahedron t)
        => TrianglePredicates.IsSame(triangle, t.ABC) ||
           TrianglePredicates.IsSame(triangle, t.ABD) ||
           TrianglePredicates.IsSame(triangle, t.ACD) ||
           TrianglePredicates.IsSame(triangle, t.BCD);

    public static bool SharesTriangle(in Seaharp.Geometry.Tetrahedron first, in Seaharp.Geometry.Tetrahedron second)
        => TriangleMatchesAny(first.ABC, second) ||
           TriangleMatchesAny(first.ABD, second) ||
           TriangleMatchesAny(first.ACD, second) ||
           TriangleMatchesAny(first.BCD, second);

    // A list of tetrahedrons is solid if every tetrahedron shares at least one triangle
    // with another tetrahedron in the same list.
    public static bool IsSolid(IReadOnlyList<Seaharp.Geometry.Tetrahedron> tetrahedrons)
    {
        if (tetrahedrons is null) throw new ArgumentNullException(nameof(tetrahedrons));
        if (tetrahedrons.Count == 0) return false;

        for (int i = 0; i < tetrahedrons.Count; i++)
        {
            var tetrahedron = tetrahedrons[i];
            if (HasSharedTriangle(tetrahedron, tetrahedrons, i)) continue;
            return false;
        }
        return true;
    }

    private static bool HasSharedTriangle(in Seaharp.Geometry.Tetrahedron tetrahedron, IReadOnlyList<Seaharp.Geometry.Tetrahedron> tetrahedrons, int selfIndex)
    {
        for (int j = 0; j < tetrahedrons.Count; j++)
        {
            if (j == selfIndex) continue;
            if (SharesTriangle(tetrahedron, tetrahedrons[j])) return true;
        }
        return false;
    }
}
