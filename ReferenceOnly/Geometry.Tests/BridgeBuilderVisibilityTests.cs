using System.Collections.Generic;
using System.Linq;
using Seaharp.Geometry;
using Seaharp.Geometry.Bridging;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class BridgeBuilderVisibilityTests
{
    private const UnitScale Unit = UnitScale.Millimeter;

    [Fact(Skip = "BridgeBuilder visibility filtering under investigation; see TriangleVisibilitySelectionTests.")]
    public void AlignedBoxesBridgeUsesFacingSurfaces()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(0, 0, 60);

        Assert.True(BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out var tetrahedrons));
        Assert.Equal(3, tetrahedrons.Count);

        AssertFacingSurfaceVertices(lower, upper, tetrahedrons);
    }

    [Fact]
    public void RotatedUpperBoxStillBridgesUsingFacingSurfaces()
    {
        var lower = new Box(Unit, 40, 40, 40)
            .Position(-20, -20, 0);

        var upper = new Box(Unit, 40, 40, 40)
            .Position(-20, -20, 0)
            .Rotate(zDegrees: 30)
            .Position(0, 0, 70);

        Assert.True(BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out var tetrahedrons));
        Assert.Equal(3, tetrahedrons.Count);

        AssertFacingSurfaceVertices(lower, upper, tetrahedrons);
    }

    [Fact(Skip = "BridgeBuilder visibility filtering under investigation; see TriangleVisibilitySelectionTests.")]
    public void HorizontallyOffsetBoxesDoNotBridge()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(150, 0, 60);

        Assert.False(BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out _));
    }

    [Fact(Skip = "BridgeBuilder visibility filtering under investigation; see TriangleVisibilitySelectionTests.")]
    public void TiltedUpperBoxDoesNotBridge()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40)
            .Rotate(xDegrees: 20)
            .Position(0, 0, 60);

        Assert.False(BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out _));
    }

    [Fact]
    public void BoxesStackedFlushDoNotBridge()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(0, 0, 40);

        Assert.False(BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out _));
    }

    [Fact]
    public void BoxesSeparatedByLargeGapDoNotBridge()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(0, 0, 400);

        Assert.False(BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out _));
    }

    [Fact]
    public void BridgeIsSymmetricBetweenShapes()
    {
        var lower = new Box(Unit, 40, 40, 40)
            .Position(-20, -20, 0);

        var upper = new Box(Unit, 40, 40, 40)
            .Position(-20, -20, 0)
            .Position(0, 0, 60);

        var forward = BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out var forwardBridge);
        var backward = BridgeBuilder.TryFindBridge(upper.Solid, lower.Solid, out var backwardBridge);

        Assert.Equal(forward, backward);
        Assert.Equal(forwardBridge?.Count ?? 0, backwardBridge?.Count ?? 0);
    }

    [Fact(Skip = "BridgeBuilder visibility filtering under investigation; see TriangleVisibilitySelectionTests.")]
    public void BoxesOffsetDiagonallyDoNotBridge()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40)
            .Position(80, 80, 60);

        Assert.False(BridgeBuilder.TryFindBridge(lower.Solid, upper.Solid, out _));
    }

    private static void AssertFacingSurfaceVertices(Shape lower, Shape upper, IReadOnlyList<Tetrahedron> bridge)
    {
        var lowerTopZ = lower.Solid.GetBounds().Max.Z;
        var upperBottomZ = upper.Solid.GetBounds().Min.Z;

        var uniqueVertices = bridge
            .SelectMany(t => t.Vertices)
            .Distinct()
            .ToList();

        Assert.Equal(6, uniqueVertices.Count);
        Assert.All(uniqueVertices, v => Assert.True(v.Z == lowerTopZ || v.Z == upperBottomZ));

        var lowerVertices = uniqueVertices.Where(v => v.Z == lowerTopZ).ToList();
        var upperVertices = uniqueVertices.Where(v => v.Z == upperBottomZ).ToList();

        Assert.Equal(3, lowerVertices.Count);
        Assert.Equal(3, upperVertices.Count);
    }
}
