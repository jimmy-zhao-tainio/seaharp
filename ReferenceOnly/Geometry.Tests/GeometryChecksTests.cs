using System;
using System.Linq;
using Seaharp.Geometry;
using Seaharp.Geometry.Bridging;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class GeometryChecksTests
{
    [Fact]
    public void IsPointInTriangleInterior_IsStrict()
    {
        var tri = new Triangle(new Point(0, 0, 0), new Point(10, 0, 0), new Point(0, 10, 0));

        Assert.True(GeometryChecks.IsPointInTriangleInterior(new Point(1, 1, 0), tri));
        Assert.False(GeometryChecks.IsPointInTriangleInterior(new Point(0, 0, 0), tri));
        Assert.False(GeometryChecks.IsPointInTriangleInterior(new Point(5, 0, 0), tri));
        Assert.False(GeometryChecks.IsPointInTriangleInterior(new Point(1, 1, 1), tri));
    }

    [Fact]
    public void DoesLineIntersectTriangleInterior_IsStrict()
    {
        var tri = new Triangle(new Point(0, 0, 0), new Point(10, 0, 0), new Point(0, 10, 0));

        // Vertical line through interior
        Assert.True(GeometryChecks.DoesLineIntersectTriangleInterior(new Point(1, 1, -5), new Point(1, 1, 5), tri));

        // Vertical line through a vertex (boundary only)
        Assert.False(GeometryChecks.DoesLineIntersectTriangleInterior(new Point(0, 0, -5), new Point(0, 0, 5), tri));
    }

    [Fact]
    public void DoTrianglesIntersectInterior_SeparatedPlanes_False()
    {
        var t1 = new Triangle(new Point(0, 0, 0), new Point(10, 0, 0), new Point(0, 10, 0));
        var t2 = new Triangle(new Point(0, 0, 20), new Point(10, 0, 20), new Point(0, 10, 20));

        Assert.False(GeometryChecks.DoTrianglesIntersectInterior(t1, t2));
        Assert.False(GeometryChecks.DoTrianglesIntersectInterior(t2, t1));
    }

    [Fact]
    public void TriangleBridge_PrismBetweenParallelSeparatedTriangles()
    {
        var t1 = new Triangle(new Point(0, 0, 0), new Point(10, 0, 0), new Point(0, 10, 0));
        var t2 = new Triangle(new Point(0, 0, 20), new Point(0, 10, 20), new Point(10, 0, 20));

        Assert.Equal(BridgeCase.Prism3Tets, TriangleBridgeBuilder.Explain(t1, t2));
        var conn = TriangleBridgeBuilder.Connect(t1, t2);
        Assert.Equal(3, conn.Count);
        Assert.All(conn, t => Assert.NotEqual(0, IntegerMath.SignedTetrahedronVolume6(t.Vertices[0], t.Vertices[1], t.Vertices[2], t.Vertices[3])));
    }

    // Removed mutually-facing heuristic test: algorithm now uses half-space filters
}
