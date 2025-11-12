using Xunit;
using Seaharp.World;

namespace Seaharp.World.Tests;

public class SurfaceManifoldShapesTests
{
    [Fact]
    public void Box_Surface_IsManifold()
    {
        var box = new Box(width: 4, depth: 3, height: 2);
        var surface = Seaharp.World.ClosedSurfaceBuilder.FromShape(box);
        Assert.True(Seaharp.Surface.SurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void Sphere_Surface_IsManifold()
    {
        var sphere = new Sphere(radius: 5, subdivisions: 1);
        var surface = Seaharp.World.ClosedSurfaceBuilder.FromShape(sphere);
        Assert.True(Seaharp.Surface.SurfacePredicates.IsManifold(surface));
    }

    [Fact]
    public void Cylinder_Surface_IsManifold()
    {
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);
        var surface = Seaharp.World.ClosedSurfaceBuilder.FromShape(cyl);
        Assert.True(Seaharp.Surface.SurfacePredicates.IsManifold(surface));
    }
}


