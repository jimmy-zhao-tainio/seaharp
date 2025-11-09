using System;
using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Geometry;
using Seaharp.Geometry.Predicates;

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

        var adjBefore = TestHelpers.BuildAdjacency(beforeTets);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));

        shape.Rotate(rx, ry, rz);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));

        var afterTets = shape.Tetrahedrons;
        var adjAfter = TestHelpers.BuildAdjacency(afterTets);

        Assert.Equal(beforeTets.Count, afterTets.Count);
        Assert.Equal(adjBefore.Length, adjAfter.Length);
        for (int i = 0; i < adjBefore.Length; i++)
        {
            Assert.Equal(adjBefore[i], adjAfter[i]);
        }
    }

    // Shared helpers moved to TestHelpers.
}
