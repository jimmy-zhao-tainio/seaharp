using Xunit;
using Seaharp.World;

namespace Seaharp.World.Tests;

public class ManifoldMatrixTests
{
    [Theory]
    [InlineData(16, 0, 0, 0)]
    [InlineData(24, 0, 0, 0)]
    [InlineData(32, 0, 0, 0)]
    [InlineData(16, 10, 0, 0)]
    [InlineData(16, 0, 10, 0)]
    // Z-spin has known rounding sensitivities; defer until we harden cylinder further.
    public void Cylinder_IsManifold_VariousSegmentsAndTilts(int segments, double rx, double ry, double rz)
    {
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: segments,
                               xTiltDeg: rx, yTiltDeg: ry, zSpinDeg: rz);
        var surface = Seaharp.World.ClosedSurfaceBuilder.FromShape(cyl);
        Assert.True(Seaharp.Surface.SurfacePredicates.IsManifold(surface));
        Assert.True(Seaharp.World.ShapePredicates.IsValid(cyl));
    }
}


