using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World.Predicates;

public static class PolyhedronPredicates
{
    // Returns true if every undirected edge is used by exactly two triangles.
    public static bool IsManifold(Seaharp.World.Polyhedron poly)
    {
        if (poly is null) throw new ArgumentNullException(nameof(poly));
        var verts = poly.Vertices;
        var faces = poly.Triangles;
        if (faces.Count == 0) return false;

        var edgeCounts = new Dictionary<EdgeKey, int>(faces.Count * 3);
        for (int i = 0; i < faces.Count; i++)
        {
            var (a, b, c) = faces[i];
            var p0 = verts[a];
            var p1 = verts[b];
            var p2 = verts[c];
            CountEdge(ref edgeCounts, new EdgeKey(p0, p1));
            CountEdge(ref edgeCounts, new EdgeKey(p1, p2));
            CountEdge(ref edgeCounts, new EdgeKey(p2, p0));
        }

        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value != 2) return false;
        }
        return true;
    }

    private static void CountEdge(ref Dictionary<EdgeKey, int> map, EdgeKey e)
    {
        if (map.TryGetValue(e, out var c)) map[e] = c + 1; else map[e] = 1;
    }
}

