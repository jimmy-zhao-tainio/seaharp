using System;
using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Geometry;

namespace Seaharp.World.Tests;

public class RotationCoherenceTests
{
    [Theory]
    [InlineData(90, 0, 0)]
    [InlineData(0, 90, 0)]
    [InlineData(0, 0, 90)]
    [InlineData(180, 0, 0)]
    [InlineData(0, 180, 0)]
    [InlineData(0, 0, 180)]
    [InlineData(270, 0, 0)]
    [InlineData(0, 270, 0)]
    [InlineData(0, 0, 270)]
    [InlineData(90, 90, 0)]
    [InlineData(0, 90, 90)]
    [InlineData(90, 0, 90)]
    [InlineData(90, 90, 90)]
    public void Box_CoherencePreserved_UnderOrthogonalRotations(int rx, int ry, int rz)
    {
        // Use a rectangular box that creates 5 tetrahedrons.
        var shape = new Box(width: 4, depth: 3, height: 2);
        var beforeTets = shape.Tetrahedrons;

        var adjBefore = BuildAdjacency(beforeTets);

        shape.Rotate(rx, ry, rz);

        var afterTets = shape.Tetrahedrons;
        var adjAfter = BuildAdjacency(afterTets);

        Assert.Equal(beforeTets.Count, afterTets.Count);
        Assert.Equal(adjBefore.Length, adjAfter.Length);
        for (int i = 0; i < adjBefore.Length; i++)
        {
            Assert.Equal(adjBefore[i], adjAfter[i]);
        }
    }

    private static bool[] BuildAdjacency(IReadOnlyList<Seaharp.Geometry.Tetrahedron> tets)
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

    private static bool ShareTriangle(in Seaharp.Geometry.Tetrahedron a, in Seaharp.Geometry.Tetrahedron b)
    {
        var af = new[] { a.ABC, a.ABD, a.ACD, a.BCD };
        var bf = new[] { b.ABC, b.ABD, b.ACD, b.BCD };
        foreach (var x in af)
            foreach (var y in bf)
                if (SameTri(x, y)) return true;
        return false;
    }

    private static bool SameTri(in Seaharp.Geometry.Tetrahedron.Triangle t0, in Seaharp.Geometry.Tetrahedron.Triangle t1)
    {
        return ContainsAll(t0, t1.P0, t1.P1, t1.P2);
    }

    private static bool ContainsAll(
        in Seaharp.Geometry.Tetrahedron.Triangle tri,
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
}
