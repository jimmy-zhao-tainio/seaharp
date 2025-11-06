using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Geometry;

namespace Seaharp.World.Tests;

public class RotationDecimalCoherenceTests
{
    [Theory]
    [InlineData(13.3333333333333, 27.7777777777777, 44.4444444444444)]
    [InlineData(89.9999999999, 0.0000000001, 0.0)]
    [InlineData(0.1, 0.2, 0.3)]
    [InlineData(123.456789, 234.567891, 345.678912)]
    [InlineData(-33.3333333333, 66.6666666667, -99.9999999999)]
    public void Box_Coherence_Preserved_Under_Decimal_Rotations(double rx, double ry, double rz)
    {
        var shape = new Box(width: 7, depth: 5, height: 3);
        var beforeTets = shape.Tetrahedrons;

        var adjBefore = BuildAdjacency(beforeTets);
        var uniqueBefore = UniqueVertices(beforeTets);

        shape.Rotate(rx, ry, rz);

        var afterTets = shape.Tetrahedrons;
        var adjAfter = BuildAdjacency(afterTets);
        var uniqueAfter = UniqueVertices(afterTets);

        // Same number of tetrahedrons
        Assert.Equal(beforeTets.Count, afterTets.Count);

        // Topological adjacency should be preserved by consistent rotation + rounding
        Assert.Equal(adjBefore.Length, adjAfter.Length);
        for (int i = 0; i < adjBefore.Length; i++)
        {
            Assert.Equal(adjBefore[i], adjAfter[i]);
        }

        // The 8 unique box vertices should remain 8 unique vertices (no collapse)
        Assert.Equal(uniqueBefore.Count, uniqueAfter.Count);
    }

    private static bool[] BuildAdjacency(IReadOnlyList<Tetrahedron> tets)
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

    private static bool ShareTriangle(in Tetrahedron a, in Tetrahedron b)
    {
        var af = new[] { a.ABC, a.ABD, a.ACD, a.BCD };
        var bf = new[] { b.ABC, b.ABD, b.ACD, b.BCD };
        foreach (var x in af)
            foreach (var y in bf)
                if (SameTri(x, y)) return true;
        return false;
    }

    private static bool SameTri(in Tetrahedron.Triangle t0, in Tetrahedron.Triangle t1)
        => ContainsAll(t0, t1.P0, t1.P1, t1.P2);

    private static bool ContainsAll(
        in Tetrahedron.Triangle tri,
        in Point x,
        in Point y,
        in Point z)
    {
        int found = 0;
        if (tri.P0.Equals(x) || tri.P1.Equals(x) || tri.P2.Equals(x)) found++;
        if (tri.P0.Equals(y) || tri.P1.Equals(y) || tri.P2.Equals(y)) found++;
        if (tri.P0.Equals(z) || tri.P1.Equals(z) || tri.P2.Equals(z)) found++;
        return found == 3;
    }

    private static HashSet<Point> UniqueVertices(IReadOnlyList<Tetrahedron> tets)
    {
        var set = new HashSet<Point>();
        foreach (var t in tets)
        {
            set.Add(t.A);
            set.Add(t.B);
            set.Add(t.C);
            set.Add(t.D);
        }
        return set;
    }
}

