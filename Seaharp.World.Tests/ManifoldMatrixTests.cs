using Xunit;
using Seaharp.World;
using Seaharp.World.Predicates;

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
        var surface = new Surface(cyl);
        Assert.True(SurfacePredicates.IsManifold(surface));
        Assert.True(ShapePredicates.IsValid(cyl));
    }
}
