using Xunit;
using Seaharp.World;
using Seaharp.Topology;

namespace Seaharp.World.Tests;

public class ClosedSurfaceManifoldTests
{
    [Fact]
    public void Box_Surface_IsManifold()
    {
        var box = new Box(width: 4, depth: 3, height: 2);
        var surface = box.ExtractClosedSurface();
        Assert.True(ClosedSurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void Sphere_Surface_IsManifold()
    {
        var sphere = new Sphere(radius: 5, subdivisions: 1);
        var surface = sphere.ExtractClosedSurface();
        Assert.True(ClosedSurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void Cylinder_Surface_IsManifold()
    {
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);
        var surface = cyl.ExtractClosedSurface();
        Assert.True(ClosedSurfacePredicates.IsManifold(surface));
    }
}








