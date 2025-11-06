using Seaharp.Geometry;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class TetrahedronOperationsTests
{
    [Fact]
    public void AbsoluteVolumeMatchesDirectCalculation()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(6, 0, 0);
        var c = new Point(0, 6, 0);
        var d = new Point(0, 0, 6);

        var tetra = new Tetrahedron(a, b, c, d);

        var expected = IntegerMath.AbsoluteTetrahedronVolume6(a, b, c, d);
        var actual = TetrahedronOperations.AbsoluteVolume6(tetra);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AbsoluteVolumeDetectsDegenerateTetrahedron()
    {
        var a = new Point(0, 0, 0);
        var b = new Point(6, 0, 0);
        var c = new Point(0, 6, 0);
        var d = new Point(6, 6, 0); // coplanar with base

        var tetra = new Tetrahedron(a, b, c, d);

        Assert.Equal(0, TetrahedronOperations.AbsoluteVolume6(tetra));
    }
}
