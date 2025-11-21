using System.Collections.Generic;
using Geometry;
using Kernel;
using Topology;
using Xunit;

namespace Kernel.Tests;

public class IntersectionGraphTests
{
    [Fact]
    public void FromIntersectionSet_EmptyIntersections_WrapsSetAndHasNoGraphElements()
    {
        var a0 = new Triangle(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 1));

        var b0 = new Triangle(
            new Point(10, 0, 0),
            new Point(12, 0, 0),
            new Point(10, 2, 0),
            new Point(10, 0, 1));

        var trianglesA = new List<Triangle> { a0 };
        var trianglesB = new List<Triangle> { b0 };

        var set = new IntersectionSet(trianglesA, trianglesB);

        var graph = IntersectionGraph.FromIntersectionSet(set);

        Assert.Same(set.TrianglesA, graph.IntersectionSet.TrianglesA);
        Assert.Same(set.TrianglesB, graph.IntersectionSet.TrianglesB);
        Assert.Equal(set.Intersections.Count, graph.IntersectionSet.Intersections.Count);

        Assert.Empty(graph.Vertices);
        Assert.Empty(graph.Edges);
        Assert.Empty(graph.Pairs);
    }

    [Fact]
    public void FromIntersectionSet_PairsMatchIntersections()
    {
        // Area intersection near origin.
        var a0 = new Triangle(
            new Point(0, 0, 0),
            new Point(6, 0, 0),
            new Point(0, 6, 0),
            new Point(0, 0, 1));

        var b1 = new Triangle(
            new Point(1, 1, 0),
            new Point(4, 1, 0),
            new Point(1, 4, 0),
            new Point(0, 0, 1));

        // Point intersection far away around x=100.
        var a1 = new Triangle(
            new Point(100, 0, 0),
            new Point(104, 0, 0),
            new Point(100, 4, 0),
            new Point(100, 0, 1));

        var b2 = new Triangle(
            new Point(100, 0, 0),
            new Point(96, 0, 0),
            new Point(100, -4, 0),
            new Point(100, 0, 1));

        // Disjoint triangle that should be culled by the bounding box tree.
        var b0 = new Triangle(
            new Point(20, 0, 0),
            new Point(22, 0, 0),
            new Point(20, 2, 0),
            new Point(20, 0, 1));

        var trianglesA = new List<Triangle> { a0, a1 };
        var trianglesB = new List<Triangle> { b0, b1, b2 };

        var set = new IntersectionSet(trianglesA, trianglesB);

        var graph = IntersectionGraph.FromIntersectionSet(set);

        Assert.Equal(set.Intersections.Count, graph.Pairs.Count);

        for (int i = 0; i < set.Intersections.Count; i++)
        {
            var intersection = set.Intersections[i];
            var pair = graph.Pairs[i];

            Assert.Equal(intersection.TriangleIndexA, pair.Intersection.TriangleIndexA);
            Assert.Equal(intersection.TriangleIndexB, pair.Intersection.TriangleIndexB);
            Assert.Equal(intersection.Type, pair.Intersection.Type);

            Assert.Empty(pair.Vertices);
            Assert.Empty(pair.Segments);
        }
    }
}
