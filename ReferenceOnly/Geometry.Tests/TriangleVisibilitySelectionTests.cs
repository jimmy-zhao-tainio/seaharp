using System.Collections.Generic;
using System.Linq;
using Seaharp.Geometry;
using Seaharp.Geometry.Bridging;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class TriangleVisibilitySelectionTests
{
    private const UnitScale Unit = UnitScale.Millimeter;

    [Fact]
    public void IdenticalTrianglesAreNotReported()
    {
        var triangle = new Triangle(
            new Point(0, 0, 0),
            new Point(40, 0, 0),
            new Point(0, 40, 0));

        var solid = new Solid(Unit, new[]
        {
            new Tetrahedron(triangle.A, triangle.B, triangle.C, new Point(0, 0, 40))
        });

        var pairs = BridgeBuilder.GetVisibleTrianglePairs(solid, solid, strict: false).ToList();

        Assert.DoesNotContain(pairs, pair =>
            GeometryChecks.AreTrianglesIdentical(pair.First, pair.Second));
    }

    [Fact]
    public void OutwardNormalsMustFaceEachOther()
    {
        var first = new Triangle(
            new Point(0, 0, 0),
            new Point(40, 0, 0),
            new Point(0, 40, 0));

        var second = new Triangle(
            new Point(0, 0, 60),
            new Point(0, 40, 100),
            new Point(40, 0, 100));

        var firstSolid = new Solid(Unit, new[]
        {
            new Tetrahedron(first.A, first.B, first.C, new Point(0, 0, 40))
        });

        var secondSolid = new Solid(Unit, new[]
        {
            new Tetrahedron(second.A, second.B, second.C, new Point(0, 0, 140))
        });

        var pairs = BridgeBuilder.GetVisibleTrianglePairs(firstSolid, secondSolid, strict: false).ToList();

        Assert.Empty(pairs);
    }

    [Fact]
    public void PlaneCuttingThroughTriangleInteriorIsRejected()
    {
        var first = new Triangle(
            new Point(0, 0, 0),
            new Point(40, 0, 0),
            new Point(0, 40, 0));

        var second = new Triangle(
            new Point(20, -20, 20),
            new Point(20, 20, 20),
            new Point(-20, 20, 20));

        var firstSolid = new Solid(Unit, new[]
        {
            new Tetrahedron(first.A, first.B, first.C, new Point(0, 0, 40))
        });

        var secondSolid = new Solid(Unit, new[]
        {
            new Tetrahedron(second.A, second.B, second.C, new Point(-20, -20, 40))
        });

        var pairs = BridgeBuilder.GetVisibleTrianglePairs(firstSolid, secondSolid, strict: false).ToList();

        Assert.Empty(pairs);
    }

    [Fact]
    public void FacingTrianglesYieldSinglePair()
    {
        var first = new Triangle(
            new Point(0, 0, 0),
            new Point(40, 0, 0),
            new Point(0, 40, 0)); // normal +Z

        var second = new Triangle(
            new Point(0, 0, 60),
            new Point(0, 40, 60),
            new Point(40, 0, 60)); // normal -Z (reordered)

        var firstSolid = TriangleShape(first, new Point(0, 0, 40));
        var secondSolid = TriangleShape(second, new Point(0, 0, 20));

        var pairs = BridgeBuilder.GetVisibleTrianglePairs(firstSolid, secondSolid, strict: false).ToList();

        Assert.Single(pairs);

        var (a, b) = pairs[0];
        AssertTriangleMatches(a, first.A, first.B, first.C);
        AssertTriangleMatches(b, second.A, second.B, second.C);
    }

    [Fact]
    public void AlignedBoxesExposeTopAndBottomTriangles()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(0, 0, 60);

        var pairs = CollectPairs(lower, upper);

        Assert.NotEmpty(pairs);
        Assert.All(pairs, pair =>
        {
            AssertTopTriangle(pair.First, lower);
            AssertBottomTriangle(pair.Second, upper);
        });
    }

    [Fact]
    public void RotatedUpperBoxStillMatchesFacingSurfaces()
    {
        var lower = new Box(Unit, 40, 40, 40)
            .Position(-20, -20, 0);

        var upper = new Box(Unit, 40, 40, 40)
            .Position(-20, -20, 0)
            .Rotate(zDegrees: 30)
            .Position(0, 0, 70);

        var pairs = CollectPairs(lower, upper);

        Assert.NotEmpty(pairs);
        Assert.All(pairs, pair =>
        {
            AssertTopTriangle(pair.First, lower);
            AssertBottomTriangle(pair.Second, upper);
        });
    }

    [Fact]
    public void HorizontalOffsetRemovesVisibility()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(150, 0, 60);

        var pairs = CollectPairs(lower, upper);

        Assert.Empty(pairs);
    }

    [Fact]
    public void TiltedUpperBoxMaintainsVisibility()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40)
            .Rotate(xDegrees: 20)
            .Position(0, 0, 60);

        var pairs = CollectPairs(lower, upper);

        Assert.NotEmpty(pairs);
    }

    [Fact]
    public void FlushStackedBoxesHaveNoVisibility()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(0, 0, 40);

        var pairs = CollectPairs(lower, upper);

        Assert.Empty(pairs);
    }

    [Fact]
    public void VisibilityIsSymmetric()
    {
        var lower = new Box(Unit, 40, 40, 40);
        var upper = new Box(Unit, 40, 40, 40).Position(0, 0, 60);

        var forward = CollectPairs(lower, upper);
        var backward = CollectPairs(upper, lower).Select(p => (p.Second, p.First)).ToList();

        Assert.Equal(forward.Count, backward.Count);
        Assert.All(forward.Zip(backward, (f, b) => (f, b)), pair =>
        {
            Assert.True(CompareTriangles(pair.f.First, pair.b.First));
            Assert.True(CompareTriangles(pair.f.Second, pair.b.Second));
        });
    }

    private static List<(Triangle First, Triangle Second)> CollectPairs(Shape first, Shape second) =>
        BridgeBuilder.GetVisibleTrianglePairs(first.Solid, second.Solid, strict: false).ToList();

    private static void AssertTopTriangle(Triangle triangle, Shape shape)
    {
        var topZ = shape.Solid.GetBounds().Max.Z;
        Assert.All(triangle.Vertices, v => Assert.Equal(topZ, v.Z));
    }

    private static void AssertBottomTriangle(Triangle triangle, Shape shape)
    {
        var bottomZ = shape.Solid.GetBounds().Min.Z;
        Assert.All(triangle.Vertices, v => Assert.Equal(bottomZ, v.Z));
    }

    private static bool CompareTriangles(Triangle first, Triangle second)
    {
        var a = new[] { first.A, first.B, first.C }.OrderBy(p => (p.X, p.Y, p.Z)).ToArray();
        var b = new[] { second.A, second.B, second.C }.OrderBy(p => (p.X, p.Y, p.Z)).ToArray();
        return a.SequenceEqual(b);
    }

    private static void AssertTriangleMatches(Triangle triangle, params Point[] expectedVertices)
    {
        var actual = new[] { triangle.A, triangle.B, triangle.C }.OrderBy(p => (p.X, p.Y, p.Z)).ToArray();
        var expected = expectedVertices.OrderBy(p => (p.X, p.Y, p.Z)).ToArray();
        Assert.True(actual.SequenceEqual(expected));
    }

    private static Solid TriangleShape(Triangle baseTriangle, Point apex) =>
        new Solid(Unit, new[] { new Tetrahedron(baseTriangle.A, baseTriangle.B, baseTriangle.C, apex) });
}
