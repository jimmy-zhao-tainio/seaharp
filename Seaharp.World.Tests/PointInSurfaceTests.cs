using Xunit;
using Seaharp.World;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class PointInSurfaceTests
{
    [Fact]
    public void Box_Contains_Center_And_Excludes_Outside()
    {
        var box = new Box(10, 10, 10);
        var surfaceShape = box.ToClosedSurface();
        Assert.True(Seaharp.ClosedSurface.ClosedSurfacePredicates.IsManifold(surfaceShape));

        var center = new GPoint(5,5,5);
        var outside = new GPoint(20,0,0);
        var surface = new GPoint(0,0,0);

        Assert.True(Seaharp.Geometry.Computational.InsideClosedSurface.ContainsStrict(surfaceShape.Triangles, center));
        Assert.False(Seaharp.Geometry.Computational.InsideClosedSurface.ContainsStrict(surfaceShape.Triangles, surface));
        Assert.True(Seaharp.Geometry.Computational.InsideClosedSurface.ContainsInclusive(surfaceShape.Triangles, surface));
        Assert.False(Seaharp.Geometry.Computational.InsideClosedSurface.ContainsInclusive(surfaceShape.Triangles, outside));
    }

    [Fact]
    public void Sphere_Contains_Center()
    {
        var sphere = new Sphere(radius: 10, subdivisions: 1);
        var surfaceShape = sphere.ToClosedSurface();
        Assert.True(Seaharp.ClosedSurface.ClosedSurfacePredicates.IsManifold(surfaceShape));
        Assert.True(Seaharp.Geometry.Computational.InsideClosedSurface.ContainsStrict(surfaceShape.Triangles, new GPoint(0,0,0)));
        Assert.False(Seaharp.Geometry.Computational.InsideClosedSurface.ContainsInclusive(surfaceShape.Triangles, new GPoint(100,0,0)));
    }
}







