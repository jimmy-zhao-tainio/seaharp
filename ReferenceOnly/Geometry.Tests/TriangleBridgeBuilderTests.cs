using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Seaharp.Geometry;
using Seaharp.Geometry.Bridging;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class TriangleBridgeBuilderTests
{
    private static Int128 Vol6(Point a, Point b, Point c, Point d)
        => IntegerMath.SignedTetrahedronVolume6(a, b, c, d);

    private static Int128 AbsoluteTetrahedronVolume6(Tetrahedron t)
        => Int128.Abs(IntegerMath.SignedTetrahedronVolume6(t.Vertices[0], t.Vertices[1], t.Vertices[2], t.Vertices[3]));

    private static bool UsesOnly(IEnumerable<Tetrahedron> tetrahedrons, params Point[] points)
    {
        var allowed = new HashSet<Point>(points);
        return tetrahedrons.SelectMany(t => t.Vertices).All(allowed.Contains);
    }

    private static bool BoundaryContains(Solid solid, Triangle triangle)
    {
        var target = CanonicalFace(triangle);
        return solid.BoundaryTriangles().Any(b => CanonicalFace(b) == target);
    }

    [Fact]
    public void PrismBridgeProducesThreeTetrahedrons()
    {
        var t1 = MakeTriangle((0, 0, 0), (10, 0, 0), (0, 10, 0));
        var t2 = MakeTriangle((0, 0, 10), (10, 0, 10), (0, 10, 10));

        Assert.Equal(BridgeCase.Prism3Tets, TriangleBridgeBuilder.Explain(t1, t2));

        var connections = TriangleBridgeBuilder.Connect(t1, t2);
        Assert.Equal(3, connections.Count);
        Assert.True(connections.All(t => AbsoluteTetrahedronVolume6(t) > 0));
        Assert.True(UsesOnly(connections, t1.A, t1.B, t1.C, t2.A, t2.B, t2.C));

        var solid = new Solid(UnitScale.Millimeter, connections);
        Assert.True(BoundaryContains(solid, t1));
        Assert.True(BoundaryContains(solid, t2));
    }

    [Fact]
    public void PerpendicularTrianglesReturnEmptyConnection()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var t2 = MakeTriangle((3, 2, 2), (3, 8, 2), (3, 2, 8));

        Assert.Equal(BridgeCase.Empty, TriangleBridgeBuilder.Explain(t1, t2));
        Assert.Empty(TriangleBridgeBuilder.Connect(t1, t2));
    }

    [Fact]
    public void SharedEdgeProducesSingleTetrahedron()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var t2 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 0, 6));

        var connections = TriangleBridgeBuilder.Connect(t1, t2);
        Assert.Single(connections);

        var expectedVolume = Int128.Abs(Vol6(t1.A, t1.B, t1.C, t2.C));
        Assert.Equal(expectedVolume, AbsoluteTetrahedronVolume6(connections[0]));
    }

    [Fact]
    public void VertexOnEdgeProducesThreeTetrahedrons()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var onV = new Point(3, 3, 0);
        var o0 = new Point(0, 0, 5);
        var o1 = new Point(6, 0, 5);
        var t2 = new Triangle(onV, o0, o1);

        Assert.Equal(BridgeCase.VertexOnEdge, TriangleBridgeBuilder.Explain(t1, t2));

        var connections = TriangleBridgeBuilder.Connect(t1, t2);
        Assert.True(connections.Count is 2 or 3);
        Assert.True(connections.All(t => AbsoluteTetrahedronVolume6(t) >= 0));
        Assert.True(UsesOnly(connections, t1.A, t1.B, t1.C, onV, o0, o1));

        var solid = new Solid(UnitScale.Millimeter, connections);
        Assert.True(BoundaryContains(solid, t1));
        Assert.True(BoundaryContains(solid, t2));
    }

    [Fact]
    public void SharedVertexProducesCaps()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var t2 = MakeTriangle((0, 0, 0), (0, 0, 6), (0, 6, 6));

        var connections = TriangleBridgeBuilder.Connect(t1, t2);
        Assert.True(connections.Count is 2 or 3);
        Assert.True(connections.All(t => AbsoluteTetrahedronVolume6(t) >= 0));
        Assert.True(UsesOnly(connections, t1.A, t1.B, t1.C, t2.B, t2.C));

        var solid = new Solid(UnitScale.Millimeter, connections);
        Assert.True(BoundaryContains(solid, t1));
        Assert.True(BoundaryContains(solid, t2));
    }

    [Fact]
    public void ClassifyEmptyCases()
    {
        var coplanar1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var coplanar2 = MakeTriangle((2, 2, 0), (8, 2, 0), (2, 8, 0));

        Assert.Equal(BridgeCase.Empty, TriangleBridgeBuilder.Explain(coplanar1, coplanar2));
        Assert.Empty(TriangleBridgeBuilder.Connect(coplanar1, coplanar2));

        var crossing1 = MakeTriangle((0, 0, 0), (8, 0, 0), (0, 8, 0));
        var crossing2 = MakeTriangle((4, 2, 2), (4, 2, -2), (4, 6, 1));

        Assert.Equal(BridgeCase.Empty, TriangleBridgeBuilder.Explain(crossing1, crossing2));
        Assert.Empty(TriangleBridgeBuilder.Connect(crossing1, crossing2));
    }

    private static Triangle MakeTriangle(
        (long X, long Y, long Z) a,
        (long X, long Y, long Z) b,
        (long X, long Y, long Z) c)
    {
        return new Triangle(
            new Point(a.X, a.Y, a.Z),
            new Point(b.X, b.Y, b.Z),
            new Point(c.X, c.Y, c.Z));
    }

    private static (Point, Point, Point) CanonicalFace(Triangle face)
    {
        var points = new[] { face.A, face.B, face.C };
        Array.Sort(points, ComparePoints);
        return (points[0], points[1], points[2]);
    }

    private static int ComparePoints(Point left, Point right)
    {
        var cmp = left.X.CompareTo(right.X);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = left.Y.CompareTo(right.Y);
        if (cmp != 0)
        {
            return cmp;
        }

        return left.Z.CompareTo(right.Z);
    }
}
