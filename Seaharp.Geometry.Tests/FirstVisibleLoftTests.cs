using System;
using System.Linq;
using Seaharp.Geometry;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class FirstVisibleLoftTests
{
    [Fact]
    public void FindsFirstFullyVisibleTrianglePair_AndLofts()
    {
        var lower = TetraSolid(size: 10, height: 8, zOffset: 0);
        var upper = TetraSolid(size: 10, height: 8, zOffset: 30);

        Assert.True(FirstVisibleLoft.TryFind(lower, upper, out var tA, out var tB, out var tets));
        Assert.Equal(3, tets.Count);
        Assert.All(tets, t => Assert.NotEqual(0, Exact.Orient3D(t.Vertices[0], t.Vertices[1], t.Vertices[2], t.Vertices[3])));

        var union = new Solid(UnitScale.Millimeter, tets);
        var boundary = union.BoundaryFaces().Select(CanonicalFace).ToHashSet();
        Assert.Contains(CanonicalFace(tA), boundary);
        Assert.Contains(CanonicalFace(tB), boundary);
    }

    private static Solid TetraSolid(long size, long height, long zOffset)
    {
        var a = new GridPoint(0, 0, zOffset);
        var b = new GridPoint(size, 0, zOffset);
        var c = new GridPoint(0, size, zOffset);
        var apex = new GridPoint(0, 0, zOffset + height);
        return new Solid(UnitScale.Millimeter, new[] { new Tetrahedron(a, b, c, apex) });
    }

    private static (GridPoint, GridPoint, GridPoint) CanonicalFace(TriangleFace face)
    {
        var vertices = new[] { face.A, face.B, face.C };
        Array.Sort(vertices, (lhs, rhs) =>
        {
            var cmp = lhs.X.CompareTo(rhs.X);
            if (cmp != 0)
            {
                return cmp;
            }
            cmp = lhs.Y.CompareTo(rhs.Y);
            if (cmp != 0)
            {
                return cmp;
            }
            return lhs.Z.CompareTo(rhs.Z);
        });
        return (vertices[0], vertices[1], vertices[2]);
    }
}
