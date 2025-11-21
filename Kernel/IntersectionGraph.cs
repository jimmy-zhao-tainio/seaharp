using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

public readonly struct IntersectionVertexId
{
    public int Value { get; }

    public IntersectionVertexId(int value)
    {
        Value = value;
    }

    public override string ToString() => $"v{Value}";
}

public readonly struct IntersectionEdgeId
{
    public int Value { get; }

    public IntersectionEdgeId(int value)
    {
        Value = value;
    }
}

// Represents the global intersection graph between two meshes.
// This is the future home for intersection vertices, edges, loops,
// and per-pair feature boxes. For now it simply wraps an
// IntersectionSet and exposes empty Vertices and Edges collections,
// plus one PairFeatures box per intersecting triangle pair.
// Higher-level boolean logic will eventually operate on this graph
// rather than directly on the raw IntersectionSet.
public sealed class IntersectionGraph
{
    public IntersectionSet IntersectionSet { get; }

    public IReadOnlyList<(IntersectionVertexId Id, RealPoint Position)> Vertices { get; }

    public IReadOnlyList<(IntersectionEdgeId Id, IntersectionVertexId Start, IntersectionVertexId End)> Edges { get; }

    // Per-intersection feature boxes, one for each entry in
    // IntersectionSet.Intersections, in matching index order.
    public IReadOnlyList<PairFeatures> Pairs { get; }

    internal IntersectionGraph(
        IntersectionSet intersectionSet,
        IReadOnlyList<(IntersectionVertexId, RealPoint)> vertices,
        IReadOnlyList<(IntersectionEdgeId, IntersectionVertexId, IntersectionVertexId)> edges,
        IReadOnlyList<PairFeatures> pairs)
    {
        if (intersectionSet.TrianglesA is null)
            throw new ArgumentNullException(nameof(intersectionSet));

        IntersectionSet = intersectionSet;
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        Pairs = pairs ?? throw new ArgumentNullException(nameof(pairs));
    }

    // Creates an IntersectionGraph from an existing IntersectionSet.
    // Currently this only wraps the provided IntersectionSet and exposes
    // empty vertex and edge collections, plus empty PairFeatures boxes
    // for each intersecting triangle pair. In the future,
    // intersection vertices, edges, and loops will live here and
    // higher-level boolean logic will operate on this graph instead
    // of the raw IntersectionSet.
    public static IntersectionGraph FromIntersectionSet(IntersectionSet intersectionSet)
    {
        if (intersectionSet.TrianglesA is null)
            throw new ArgumentNullException(nameof(intersectionSet));

        var vertices = Array.Empty<(IntersectionVertexId, RealPoint)>();
        var edges = Array.Empty<(IntersectionEdgeId, IntersectionVertexId, IntersectionVertexId)>();

        var intersections = intersectionSet.Intersections;
        var pairs = new PairFeatures[intersections.Count];

        for (int i = 0; i < intersections.Count; i++)
        {
            var intersection = intersections[i];
            pairs[i] = PairFeaturesFactory.CreateEmpty(in intersection);
        }

        return new IntersectionGraph(intersectionSet, vertices, edges, pairs);
    }
}
