using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Geometry;
using Seaharp.Geometry.Predicates;

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

        var adjBefore = TestHelpers.BuildAdjacency(beforeTets);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));
        var uniqueBefore = UniqueVertices(beforeTets);

        shape.Rotate(rx, ry, rz);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));

        var afterTets = shape.Tetrahedrons;
        var adjAfter = TestHelpers.BuildAdjacency(afterTets);
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

    // Shared helpers moved to TestHelpers.

    private static HashSet<Point> UniqueVertices(IReadOnlyList<Seaharp.Geometry.Tetrahedron> tets)
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
