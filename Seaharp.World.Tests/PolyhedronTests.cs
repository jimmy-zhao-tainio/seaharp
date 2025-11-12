using Xunit;
using Seaharp.World;

namespace Seaharp.World.Tests;

public class SurfaceManifoldTests
{
    [Fact]
    public void Box_Surface_IsManifold()
    {
        var box = new Box(width: 4, depth: 3, height: 2);
        var surface = box.ToClosedSurface();
        Assert.True(Seaharp.ClosedSurface.ClosedSurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void Sphere_Surface_IsManifold()
    {
        var sphere = new Sphere(radius: 5, subdivisions: 1);
        var surface = sphere.ToClosedSurface();
        Assert.True(Seaharp.ClosedSurface.ClosedSurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void Cylinder_Surface_IsManifold()
    {
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);
        var surface = cyl.ToClosedSurface();
        Assert.True(Seaharp.ClosedSurface.ClosedSurfacePredicates.IsManifold(surface));
    }
}






