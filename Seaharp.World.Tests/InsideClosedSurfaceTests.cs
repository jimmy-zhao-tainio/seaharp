using Xunit;
using Seaharp.World;
using Seaharp.Topology;
using Seaharp.Geometry;

namespace Seaharp.World.Tests;

public class InsideClosedSurfaceTests
{
    [Fact]
    public void Box_Contains_Center_And_Excludes_Outside()
    {
        var box = new Box(10, 10, 10);
        var surfaceShape = ClosedSurface.FromTetrahedra(box.Tetrahedra);
        Assert.True(ClosedSurfacePredicates.IsManifold(surfaceShape));

        var center = new Point(5,5,5);
        var outside = new Point(20,0,0);
        var surface = new Point(0,0,0);

        Assert.True(Seaharp.Geometry.Computation.InsideClosedSurface.ContainsStrict(surfaceShape.Triangles, center));
        Assert.False(Seaharp.Geometry.Computation.InsideClosedSurface.ContainsStrict(surfaceShape.Triangles, surface));
        Assert.True(Seaharp.Geometry.Computation.InsideClosedSurface.ContainsInclusive(surfaceShape.Triangles, surface));
        Assert.False(Seaharp.Geometry.Computation.InsideClosedSurface.ContainsInclusive(surfaceShape.Triangles, outside));
    }

    [Fact]
    public void Sphere_Contains_Center()
    {
        var sphere = new Sphere(radius: 10, subdivisions: 1);
        var surfaceShape = ClosedSurface.FromTetrahedra(sphere.Tetrahedra);
        Assert.True(ClosedSurfacePredicates.IsManifold(surfaceShape));
        Assert.True(Seaharp.Geometry.Computation.InsideClosedSurface.ContainsStrict(surfaceShape.Triangles, new Point(0,0,0)));
        Assert.False(Seaharp.Geometry.Computation.InsideClosedSurface.ContainsInclusive(surfaceShape.Triangles, new Point(100,0,0)));
    }
}










