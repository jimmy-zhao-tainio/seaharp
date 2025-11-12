using Xunit;
using Seaharp.World;
using Seaharp.Topology;
using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

public class IntersectionPredicatesTests
{
    // Geometry-level tests moved to Seaharp.Geometry.Tests.

    [Fact]
    public void Shape_Self_NoIntersect_ForBuiltIns()
    {
        var box = new Box(4,3,2);
        var sphere = new Sphere(5, subdivisions: 1);
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);

        Assert.True(Seaharp.World.ShapePredicates.IsValid(box));
        Assert.True(Seaharp.World.ShapePredicates.IsValid(sphere));
        Assert.True(Seaharp.World.ShapePredicates.IsValid(cyl));
    }
}


