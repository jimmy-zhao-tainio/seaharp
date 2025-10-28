using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Seaharp.Geometry;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class TriangleLoftingTests
{
    private static Int128 Vol6(GridPoint a, GridPoint b, GridPoint c, GridPoint d)
        => Exact.Orient3D(a, b, c, d);

    private static Int128 AbsVol6(Tetrahedron t)
        => Int128.Abs(Exact.Orient3D(t.Vertices[0], t.Vertices[1], t.Vertices[2], t.Vertices[3]));

    private static bool UsesOnly(IEnumerable<Tetrahedron> tets, params GridPoint[] pts)
    {
        var allowed = new HashSet<GridPoint>(pts);
        return tets.SelectMany(t => t.Vertices).All(allowed.Contains);
    }

    private static bool BoundaryContains(Solid s, TriangleFace f)
    {
        var target = CanonicalFace(f);
        return s.BoundaryFaces().Any(b => CanonicalFace(b) == target);
    }

    [Fact]
    public void PrismLoftProducesThreeTetrahedra()
    {
        var t1 = MakeTriangle((0, 0, 0), (10, 0, 0), (0, 10, 0));
        var t2 = MakeTriangle((0, 0, 10), (10, 0, 10), (0, 10, 10));

        Assert.Equal(LoftCase.Prism3Tets, TriangleLofting.Explain(t1, t2));

        var tets = TriangleLofting.Loft(t1, t2);
        Assert.Equal(3, tets.Count);
        Assert.True(tets.All(t => AbsVol6(t) > 0));
        Assert.True(UsesOnly(tets, t1.A, t1.B, t1.C, t2.A, t2.B, t2.C));

        var solid = new Solid(UnitScale.Millimeter, tets);
        Assert.True(BoundaryContains(solid, t1));
        Assert.True(BoundaryContains(solid, t2));
    }

    [Fact]
    public void PerpendicularTrianglesReturnEmpty()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var t2 = MakeTriangle((3, 2, 2), (3, 8, 2), (3, 2, 8));

        Assert.Equal(LoftCase.Empty, TriangleLofting.Explain(t1, t2));
        Assert.Empty(TriangleLofting.Loft(t1, t2));
    }

    [Fact]
    public void SharedEdgeProducesSingleTetrahedron()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var t2 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 0, 6));

        var tets = TriangleLofting.Loft(t1, t2);
        Assert.Single(tets);

        var expectedVolume = Int128.Abs(Vol6(t1.A, t1.B, t1.C, t2.C));
        Assert.Equal(expectedVolume, AbsVol6(tets[0]));
    }

    [Fact]
    public void VertexOnEdgeProducesThreeTetrahedra()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var onV = new GridPoint(3, 3, 0);
        var o0 = new GridPoint(0, 0, 5);
        var o1 = new GridPoint(6, 0, 5);
        var t2 = new TriangleFace(onV, o0, o1);

        Assert.Equal(LoftCase.VertexOnEdge, TriangleLofting.Explain(t1, t2));

        var tets = TriangleLofting.Loft(t1, t2);
        Assert.True(tets.Count is 2 or 3);
        Assert.True(tets.All(t => AbsVol6(t) >= 0));
        Assert.True(UsesOnly(tets, t1.A, t1.B, t1.C, onV, o0, o1));

        var solid = new Solid(UnitScale.Millimeter, tets);
        Assert.True(BoundaryContains(solid, t1));
        Assert.True(BoundaryContains(solid, t2));
    }

    [Fact]
    public void SharedVertexProducesCaps()
    {
        var t1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var t2 = MakeTriangle((0, 0, 0), (0, 0, 6), (0, 6, 6));

        var tets = TriangleLofting.Loft(t1, t2);
        Assert.True(tets.Count is 2 or 3);
        Assert.True(tets.All(t => AbsVol6(t) >= 0));
        Assert.True(UsesOnly(tets, t1.A, t1.B, t1.C, t2.B, t2.C));

        var solid = new Solid(UnitScale.Millimeter, tets);
        Assert.True(BoundaryContains(solid, t1));
        Assert.True(BoundaryContains(solid, t2));
    }

    [Fact]
    public void ClassifyEmptyCases()
    {
        var coplanar1 = MakeTriangle((0, 0, 0), (6, 0, 0), (0, 6, 0));
        var coplanar2 = MakeTriangle((2, 2, 0), (8, 2, 0), (2, 8, 0));

        Assert.Equal(LoftCase.Empty, TriangleLofting.Explain(coplanar1, coplanar2));
        Assert.Empty(TriangleLofting.Loft(coplanar1, coplanar2));

        var crossing1 = MakeTriangle((0, 0, 0), (8, 0, 0), (0, 8, 0));
        var crossing2 = MakeTriangle((4, 2, 2), (4, 2, -2), (4, 6, 1));

        Assert.Equal(LoftCase.Empty, TriangleLofting.Explain(crossing1, crossing2));
        Assert.Empty(TriangleLofting.Loft(crossing1, crossing2));
    }

    private static TriangleFace MakeTriangle(
        (long X, long Y, long Z) a,
        (long X, long Y, long Z) b,
        (long X, long Y, long Z) c)
    {
        return new TriangleFace(
            new GridPoint(a.X, a.Y, a.Z),
            new GridPoint(b.X, b.Y, b.Z),
            new GridPoint(c.X, c.Y, c.Z));
    }

    private static (GridPoint, GridPoint, GridPoint) CanonicalFace(TriangleFace face)
    {
        var points = new[] { face.A, face.B, face.C };
        Array.Sort(points, ComparePoints);
        return (points[0], points[1], points[2]);
    }

    private static int ComparePoints(GridPoint left, GridPoint right)
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
