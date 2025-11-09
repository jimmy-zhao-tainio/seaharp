using System;
using Seaharp.Geometry;

namespace Seaharp.World.Predicates;

public static class IntersectionPredicates
{
    // First step: strict volumetric overlap detection for tetrahedra and shapes.
    // Contacts along a single face/edge/vertex are NOT counted as intersections.

    // Note: tetrahedron-level intersection lives in Seaharp.Geometry.Predicates.

    public static bool Intersects(Seaharp.World.Shape first, Seaharp.World.Shape second)
    {
        if (first is null) throw new ArgumentNullException(nameof(first));
        if (second is null) throw new ArgumentNullException(nameof(second));

        var a = first.Tetrahedrons;
        var b = second.Tetrahedrons;
        for (int i = 0; i < a.Count; i++)
        {
            for (int j = 0; j < b.Count; j++)
            {
                if (Seaharp.Geometry.Predicates.TetrahedronIntersectionPredicates.Intersects(a[i], b[j])) return true;
            }
        }
        return false;
    }

    public static bool HasSelfIntersections(Seaharp.World.Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        var tets = shape.Tetrahedrons;
        for (int i = 0; i < tets.Count; i++)
        {
            for (int j = i + 1; j < tets.Count; j++)
            {
                if (Seaharp.Geometry.Predicates.TetrahedronIntersectionPredicates.Intersects(tets[i], tets[j])) return true;
            }
        }
        return false;
    }

    // Geometry-level helpers moved to Seaharp.Geometry.Predicates.
}

