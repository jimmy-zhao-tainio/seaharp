using Xunit;
using Seaharp.World;
using Seaharp.Topology;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class ClosedSurfaceShapeValidityTests
{
    [Fact]
    public void BuiltInShapes_AreValid()
    {
        var box = new Box(width: 4, depth: 3, height: 2);
        var sphere = new Sphere(radius: 5, subdivisions: 1);
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);

        Assert.True(ClosedSurfacePredicates.IsManifold(ClosedSurface.FromTetrahedra(box.Tetrahedrons)));
        Assert.True(ClosedSurfacePredicates.IsManifold(ClosedSurface.FromTetrahedra(sphere.Tetrahedrons)));
        Assert.True(ClosedSurfacePredicates.IsManifold(ClosedSurface.FromTetrahedra(cyl.Tetrahedrons)));
    }

    [Fact]
    public void NonManifoldEdge_IsInvalid()
    {
        // Two tetrahedra that only share an edge (not a face)
        // produce a non-manifold boundary (edge degree > 2).
        var a = new GPoint(0, 0, 0);
        var b = new GPoint(1, 0, 0);
        var c = new GPoint(0, 1, 0);
        var d = new GPoint(0, 0, 1);
        var e = new GPoint(0, -1, 0);
        var f = new GPoint(0, 0, -1);

        var shape = new TwoTetsShareEdgeShape(a, b, c, d, e, f);
        Assert.False(ClosedSurfacePredicates.IsManifold(ClosedSurface.FromTetrahedra(shape.Tetrahedrons)));
}
}

