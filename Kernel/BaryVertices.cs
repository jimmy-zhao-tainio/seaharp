using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Internal;

namespace Kernel;

// Collection of 3D intersection samples tied to local PairVertex
// indices. Encapsulates basic operations such as finding the
// farthest-apart pair of vertices. This keeps the non-coplanar
// segment logic testable independently of PairFeaturesFactory.
internal sealed class BaryVertices
{
    private readonly List<PairVertexSample3D> _samples = new();

    public int Count => _samples.Count;

    public IReadOnlyList<PairVertexSample3D> Samples => _samples;

    public void Add(int vertexIndex, in Vector point)
    {
        _samples.Add(new PairVertexSample3D(vertexIndex, point));
    }

    public void FindFarthestPair(
        out int startVertexIndex,
        out int endVertexIndex)
    {
        PairVertexSample3D.FindFarthestPair(_samples, out startVertexIndex, out endVertexIndex);
    }
}

internal readonly struct PairVertexSample3D
{
    public int VertexIndex { get; }

    public Vector Point { get; }

    public PairVertexSample3D(int vertexIndex, Vector point)
    {
        VertexIndex = vertexIndex;
        Point = point;
    }

    // Find the pair of samples whose 3D points are farthest apart
    // in squared-distance sense. Returns the corresponding vertex
    // indices from the PairVertex list.
    public static void FindFarthestPair(
        IReadOnlyList<PairVertexSample3D> samples,
        out int startVertexIndex,
        out int endVertexIndex)
    {
        if (samples is null) throw new ArgumentNullException(nameof(samples));
        if (samples.Count == 0) throw new ArgumentException("Samples must be non-empty.", nameof(samples));

        startVertexIndex = samples[0].VertexIndex;
        endVertexIndex = samples[0].VertexIndex;
        double maxSqDist = 0.0;

        for (int i = 0; i < samples.Count - 1; i++)
        {
            var pi = samples[i].Point;
            for (int j = i + 1; j < samples.Count; j++)
            {
                var pj = samples[j].Point;
                double dx = pj.X - pi.X;
                double dy = pj.Y - pi.Y;
                double dz = pj.Z - pi.Z;
                double d2 = dx * dx + dy * dy + dz * dz;
                if (d2 > maxSqDist)
                {
                    maxSqDist = d2;
                    startVertexIndex = samples[i].VertexIndex;
                    endVertexIndex = samples[j].VertexIndex;
                }
            }
        }
    }
}

// Collection of 2D projected samples (for coplanar pairs) with
// helpers for operations such as finding the farthest-apart
// pair. This mirrors the 3D BaryVertices path but works in the
// projected plane.
internal sealed class BaryVertices2D
{
    private readonly List<PairVertexSample2D> _samples = new();

    public int Count => _samples.Count;

    public IReadOnlyList<PairVertexSample2D> Samples => _samples;

    public void Add(int vertexIndex, in PairIntersectionMath.Point2D point)
    {
        _samples.Add(new PairVertexSample2D(vertexIndex, point));
    }

    public void FindFarthestPair(
        out int startVertexIndex,
        out int endVertexIndex)
    {
        PairVertexSample2D.FindFarthestPair(_samples, out startVertexIndex, out endVertexIndex);
    }
}

internal readonly struct PairVertexSample2D
{
    public int VertexIndex { get; }

    public PairIntersectionMath.Point2D Point { get; }

    public PairVertexSample2D(int vertexIndex, PairIntersectionMath.Point2D point)
    {
        VertexIndex = vertexIndex;
        Point = point;
    }

    public static void FindFarthestPair(
        IReadOnlyList<PairVertexSample2D> samples,
        out int startVertexIndex,
        out int endVertexIndex)
    {
        if (samples is null) throw new ArgumentNullException(nameof(samples));
        if (samples.Count == 0) throw new ArgumentException("Samples must be non-empty.", nameof(samples));

        startVertexIndex = samples[0].VertexIndex;
        endVertexIndex = samples[0].VertexIndex;
        double maxSqDist = 0.0;

        for (int i = 0; i < samples.Count - 1; i++)
        {
            var pi = samples[i].Point;
            for (int j = i + 1; j < samples.Count; j++)
            {
                var pj = samples[j].Point;
                double dx = pj.X - pi.X;
                double dy = pj.Y - pi.Y;
                double d2 = dx * dx + dy * dy;
                if (d2 > maxSqDist)
                {
                    maxSqDist = d2;
                    startVertexIndex = samples[i].VertexIndex;
                    endVertexIndex = samples[j].VertexIndex;
                }
            }
        }
    }
}
