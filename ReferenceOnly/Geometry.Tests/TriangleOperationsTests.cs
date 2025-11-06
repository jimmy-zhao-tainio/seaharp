using System;
using Seaharp.Geometry;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class TriangleOperationsTests
{
    private static Triangle MakeTriangle(
        (long X, long Y, long Z) a,
        (long X, long Y, long Z) b,
        (long X, long Y, long Z) c) =>
        new Triangle(
            new Point(a.X, a.Y, a.Z),
            new Point(b.X, b.Y, b.Z),
            new Point(c.X, c.Y, c.Z));

    [Fact]
    public void TryGetSharedEdgeReturnsCanonicalEdge()
    {
        var t1 = MakeTriangle((0, 0, 0), (5, 0, 0), (0, 5, 0));
        var t2 = MakeTriangle((0, 0, 0), (5, 0, 0), (2, 2, 4));

        Assert.True(TriangleOperations.TryGetSharedEdge(t1, t2, out var edge));

        Assert.Equal(new Point(0, 0, 0), edge.Start);
        Assert.Equal(new Point(5, 0, 0), edge.End);
    }

    [Fact]
    public void TryGetSharedEdgeReturnsFalseWhenNoEdge()
    {
        var t1 = MakeTriangle((0, 0, 0), (5, 0, 0), (0, 5, 0));
        var t2 = MakeTriangle((0, 0, 6), (5, 0, 6), (0, 5, 6));

        Assert.False(TriangleOperations.TryGetSharedEdge(t1, t2, out _));
    }

    [Fact]
    public void GetRemainingVerticesReturnsOpposites()
    {
        var triangle = MakeTriangle((0, 0, 0), (5, 0, 0), (0, 5, 0));
        var (first, second) = TriangleOperations.GetRemainingVertices(triangle, new Point(0, 0, 0));

        Assert.Equal(new Point(5, 0, 0), first);
        Assert.Equal(new Point(0, 5, 0), second);
    }

    [Fact]
    public void GetOppositeVertexReturnsThirdVertex()
    {
        var triangle = MakeTriangle((0, 0, 0), (5, 0, 0), (0, 5, 0));

        var opposite = TriangleOperations.GetOppositeVertex(triangle, new Point(0, 0, 0), new Point(5, 0, 0));

        Assert.Equal(new Point(0, 5, 0), opposite);
    }

    [Fact]
    public void TryGetVertexOnEdgeFindsInteriorVertex()
    {
        var host = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var sharedVertex = new Point(0, 3, 0);
        var other = new Triangle(sharedVertex, new Point(0, 0, 4), new Point(6, 0, 4));

        Assert.True(TriangleOperations.TryGetVertexOnEdge(host, other, out var match));

        Assert.Equal(sharedVertex, match.VertexOnEdge);
        Assert.Contains(match.HostEdge.Start, new[] { host.A, host.C });
        Assert.Contains(match.HostEdge.End, new[] { host.A, host.C });
        Assert.NotEqual(match.HostEdge.Start, match.HostEdge.End);
        Assert.Contains(match.OtherVertex0, other.Vertices);
        Assert.Contains(match.OtherVertex1, other.Vertices);
    }

    [Fact]
    public void TryGetVertexOnEdgeReturnsFalseWhenNoInteriorVertex()
    {
        var host = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var other = MakeTriangle((0, 0, 4), (6, 0, 4), (0, 6, 4));

        Assert.False(TriangleOperations.TryGetVertexOnEdge(host, other, out _));
    }
}
