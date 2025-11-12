using System.Collections.Generic;
using Seaharp.Geometry;
using Seaharp.Geometry.Computation;

namespace Seaharp.World.Tests;

internal static class TestHelpers
{
    public static bool[] BuildAdjacency(IReadOnlyList<Seaharp.Geometry.Tetrahedron> tets)
    {
        int n = tets.Count;
        var adj = new bool[n * n];
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                bool shared = ShareTriangle(tets[i], tets[j]);
                adj[i * n + j] = shared;
                adj[j * n + i] = shared;
            }
        }
        return adj;
    }

    public static bool ShareTriangle(in Seaharp.Geometry.Tetrahedron a, in Seaharp.Geometry.Tetrahedron b)
    {
        var af = new[] { a.ABC, a.ABD, a.ACD, a.BCD };
        var bf = new[] { b.ABC, b.ABD, b.ACD, b.BCD };
        foreach (var x in af)
            foreach (var y in bf)
                if (TrianglePredicates.IsSame(x, y)) return true;
        return false;
    }
}


