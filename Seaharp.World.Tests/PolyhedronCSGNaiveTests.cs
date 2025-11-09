using Xunit;
using Seaharp.World;
using Seaharp.World.Predicates;

namespace Seaharp.World.Tests;

public class PolyhedronCSGNaiveTests
{
    [Fact]
    public void Union_DisjointSpheres_Manifold_CombinedTriangleCount()
    {
        var s1 = new Sphere(radius: 8, subdivisions: 1, center: new Seaharp.Geometry.Point(-30, 0, 0));
        var s2 = new Sphere(radius: 8, subdivisions: 1, center: new Seaharp.Geometry.Point(30, 0, 0));
        var p1 = Polyhedron.FromShape(s1);
        var p2 = Polyhedron.FromShape(s2);
        var u = PolyhedronCSG.UnionNaive(p1, p2);

        Assert.True(PolyhedronPredicates.IsManifold(p1));
        Assert.True(PolyhedronPredicates.IsManifold(p2));
        Assert.True(PolyhedronPredicates.IsManifold(u));
        Assert.True(u.Triangles.Count >= p1.Triangles.Count + p2.Triangles.Count - 4); // allow minor dedup
    }

    [Fact]
    public void Union_ContainedSphere_EqualsOuter()
    {
        var outer = new Sphere(radius: 12, subdivisions: 1, center: new Seaharp.Geometry.Point(0, 0, 0));
        var inner = new Sphere(radius: 4, subdivisions: 1, center: new Seaharp.Geometry.Point(0, 0, 0));
        var po = Polyhedron.FromShape(outer);
        var pi = Polyhedron.FromShape(inner);
        var u = PolyhedronCSG.UnionNaive(po, pi);

        Assert.True(PolyhedronPredicates.IsManifold(u));
        Assert.True(PolyhedronPredicates.IsManifold(po));
        Assert.Equal(po.Triangles.Count, u.Triangles.Count);
    }
}

