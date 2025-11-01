using System;
using System.Linq;
using Seaharp.Geometry;
using Seaharp.Geometry.Bridging;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class BridgeSearchTests
{
    [Fact]
    public void FindsFirstVisibleTrianglePairAndBuildsBridge()
    {
        var lower = TetraSolid(size: 10, height: 8, zOffset: 0);
        var upper = TetraSolid(size: 10, height: 8, zOffset: 30);

        Assert.True(BridgeSearch.TryFindBridge(lower, upper, out var lowerTriangle, out var upperTriangle, out var bridgeTetrahedrons));
        Assert.Equal(3, bridgeTetrahedrons.Count);
        Assert.All(bridgeTetrahedrons, t => Assert.NotEqual(0, IntegerMath.SignedTetrahedronVolume6(t.Vertices[0], t.Vertices[1], t.Vertices[2], t.Vertices[3])));

        var union = new Solid(UnitScale.Millimeter, bridgeTetrahedrons);
        var boundary = union.BoundaryTriangles().Select(CanonicalFace).ToHashSet();
        Assert.Contains(CanonicalFace(lowerTriangle), boundary);
        Assert.Contains(CanonicalFace(upperTriangle), boundary);
    }

    private static Solid TetraSolid(long size, long height, long zOffset)
    {
        var a = new Point(0, 0, zOffset);
        var b = new Point(size, 0, zOffset);
        var c = new Point(0, size, zOffset);
        var apex = new Point(0, 0, zOffset + height);
        return new Solid(UnitScale.Millimeter, new[] { new Tetrahedron(a, b, c, apex) });
    }

    private static (Point, Point, Point) CanonicalFace(Triangle face)
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
