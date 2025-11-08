using System;
using System.Collections.Generic;

namespace Seaharp.World.Predicates;

public static class SurfacePredicates
{
    // Returns true if every undirected edge is used by exactly two triangles.
    // Treats manifold as closed (no boundary edges allowed).
    public static bool IsManifold(Surface surface)
    {
        if (surface is null) throw new ArgumentNullException(nameof(surface));
        var tris = surface.Triangles;
        if (tris.Count == 0) return false;

        var edgeCounts = new Dictionary<Seaharp.Geometry.EdgeKey, int>(tris.Count * 3);
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            CountEdge(ref edgeCounts, new Seaharp.Geometry.EdgeKey(t.P0, t.P1));
            CountEdge(ref edgeCounts, new Seaharp.Geometry.EdgeKey(t.P1, t.P2));
            CountEdge(ref edgeCounts, new Seaharp.Geometry.EdgeKey(t.P2, t.P0));
        }

        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value != 2) return false;
        }
        return true;
    }

    private static void CountEdge(ref Dictionary<Seaharp.Geometry.EdgeKey, int> map, Seaharp.Geometry.EdgeKey e)
    {
        if (map.TryGetValue(e, out var c)) map[e] = c + 1; else map[e] = 1;
    }

    // Note: Predicates do not return geometry. Intentionally no helpers here
    // that construct triangles; use Shape.GetSurface() or Surface for that.

    // EdgeKey moved into Seaharp.Geometry for reuse across components.
}
