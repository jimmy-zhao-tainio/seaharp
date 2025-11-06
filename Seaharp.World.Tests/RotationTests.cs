using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Geometry;
using Seaharp.Geometry.Predicates;

namespace Seaharp.World.Tests;

public class RotationTests
{
    [Fact]
    public void Rotate_NoOp_ZeroAngles_NoChange()
    {
        var shape = new Box(width: 3, depth: 2, height: 4);
        var before = Snapshot(shape.Tetrahedrons);

        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));
        shape.Rotate(0, 0, 0);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));

        var after = shape.Tetrahedrons;
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].A, after[i].A);
            Assert.Equal(before[i].B, after[i].B);
            Assert.Equal(before[i].C, after[i].C);
            Assert.Equal(before[i].D, after[i].D);
        }
    }

    [Theory]
    [InlineData(90, 0, 0)]
    [InlineData(0, 90, 0)]
    [InlineData(0, 0, 90)]
    [InlineData(180, 0, 0)]
    [InlineData(0, 180, 0)]
    [InlineData(0, 0, 180)]
    public void Rotate_Orthogonal_Reversible_Exact(int rx, int ry, int rz)
    {
        var shape = new Box(width: 4, depth: 3, height: 2);
        var before = Snapshot(shape.Tetrahedrons);

        shape.Rotate(rx, ry, rz);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));
        shape.Rotate(-rx, -ry, -rz);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));

        var after = shape.Tetrahedrons;
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].A, after[i].A);
            Assert.Equal(before[i].B, after[i].B);
            Assert.Equal(before[i].C, after[i].C);
            Assert.Equal(before[i].D, after[i].D);
        }
    }

    [Theory]
    [InlineData(30, 0, 0)]
    [InlineData(0, 30, 0)]
    [InlineData(0, 0, 30)]
    [InlineData(25, 15, 10)]
    public void Rotate_Arbitrary_Reversible_Close(int rx, int ry, int rz)
    {
        var shape = new Box(width: 5, depth: 4, height: 3);
        var before = Snapshot(shape.Tetrahedrons);

        shape.Rotate(rx, ry, rz);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));
        shape.Rotate(-rx, -ry, -rz);
        Assert.True(TetrahedronPredicates.IsSolid(shape.Tetrahedrons));

        var after = shape.Tetrahedrons;
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            AssertClose(before[i].A, after[i].A, 1);
            AssertClose(before[i].B, after[i].B, 1);
            AssertClose(before[i].C, after[i].C, 1);
            AssertClose(before[i].D, after[i].D, 1);
        }
    }

    private static void AssertClose(Point expected, Point actual, long tol)
    {
        Assert.InRange(System.Math.Abs(actual.X - expected.X), 0, tol);
        Assert.InRange(System.Math.Abs(actual.Y - expected.Y), 0, tol);
        Assert.InRange(System.Math.Abs(actual.Z - expected.Z), 0, tol);
    }

    private static List<(Point A, Point B, Point C, Point D)> Snapshot(IReadOnlyList<Seaharp.Geometry.Tetrahedron> tets)
    {
        var copy = new List<(Point, Point, Point, Point)>(tets.Count);
        for (int i = 0; i < tets.Count; i++)
        {
            var t = tets[i];
            copy.Add((t.A, t.B, t.C, t.D));
        }
        return copy;
    }
}

