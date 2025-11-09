using Xunit;
using Seaharp.World;
using Seaharp.World.Predicates;

namespace Seaharp.World.Tests;

public class PolyhedronTests
{
    [Fact]
    public void Box_ToPolyhedron_IsManifold()
    {
        var box = new Box(width: 4, depth: 3, height: 2);
        var poly = Polyhedron.FromShape(box);
        Assert.True(PolyhedronPredicates.IsManifold(poly));
    }

    [Fact]
    public void Sphere_ToPolyhedron_IsManifold()
    {
        var sphere = new Sphere(radius: 5, subdivisions: 1);
        var poly = Polyhedron.FromShape(sphere);
        Assert.True(PolyhedronPredicates.IsManifold(poly));
    }

    [Fact]
    public void Cylinder_ToPolyhedron_IsManifold()
    {
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);
        var poly = Polyhedron.FromShape(cyl);
        Assert.True(PolyhedronPredicates.IsManifold(poly));
    }
}

