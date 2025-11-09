using Xunit;
using Seaharp.World;
using Seaharp.World.Predicates;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class PointInPolyhedronTests
{
    [Fact]
    public void Box_Contains_Center_And_Excludes_Outside()
    {
        var box = new Box(10, 10, 10);
        var poly = Polyhedron.FromShape(box);
        Assert.True(PolyhedronPredicates.IsManifold(poly));

        var center = new GPoint(5,5,5);
        var outside = new GPoint(20,0,0);
        var surface = new GPoint(0,0,0);

        Assert.True(PointInPolyhedron.ContainsStrict(poly, center));
        Assert.False(PointInPolyhedron.ContainsStrict(poly, surface));
        Assert.True(PointInPolyhedron.ContainsInclusive(poly, surface));
        Assert.False(PointInPolyhedron.ContainsInclusive(poly, outside));
    }

    [Fact]
    public void Sphere_Contains_Center()
    {
        var sphere = new Sphere(radius: 10, subdivisions: 1);
        var poly = Polyhedron.FromShape(sphere);
        Assert.True(PolyhedronPredicates.IsManifold(poly));
        Assert.True(PointInPolyhedron.ContainsStrict(poly, new GPoint(0,0,0)));
        Assert.False(PointInPolyhedron.ContainsInclusive(poly, new GPoint(100,0,0)));
    }
}

