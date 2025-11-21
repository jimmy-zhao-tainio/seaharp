using System;
using System.Collections.Generic;
using Geometry;
using Geometry.Internal;
using Geometry.Predicates;
using Topology;

namespace Kernel;

// Represents one intersection point for a specific triangle pair,
// expressed in barycentric coordinates on both triangle A and B.
// This is a pure data container; it does not compute barycentric
// coordinates or perform any geometric predicates.
public readonly struct PairVertex
{
    public IntersectionVertexId VertexId { get; }

    public Barycentric OnTriangleA { get; }

    public Barycentric OnTriangleB { get; }

    public PairVertex(
        IntersectionVertexId vertexId,
        Barycentric onTriangleA,
        Barycentric onTriangleB)
    {
        VertexId = vertexId;
        OnTriangleA = onTriangleA;
        OnTriangleB = onTriangleB;
    }
}

// Represents a segment of the intersection curve between two triangles,
// in the local coordinates of a single triangle pair. This is also
// just a container; no geometric computations are performed here.
public readonly struct PairSegment
{
    public PairVertex Start { get; }

    public PairVertex End { get; }

    public PairSegment(
        PairVertex start,
        PairVertex end)
    {
        Start = start;
        End = end;
    }
}

// Holds all intersection vertices and segments for a single triangle pair.
// The pair is identified by an IntersectionSet.Intersection, which carries
// the triangle indices and the high-level IntersectionType classification.
public sealed class PairFeatures
{
    public IntersectionSet.Intersection Intersection { get; }

    public IReadOnlyList<PairVertex> Vertices { get; }

    public IReadOnlyList<PairSegment> Segments { get; }

    public PairFeatures(
        IntersectionSet.Intersection intersection,
        IReadOnlyList<PairVertex> vertices,
        IReadOnlyList<PairSegment> segments)
    {
        Intersection = intersection;
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Segments = segments ?? throw new ArgumentNullException(nameof(segments));
    }
}

// Factory entry point for building PairFeatures instances.
// PairFeaturesFactory.CreateEmpty keeps the original "empty box"
// behaviour; PairFeaturesFactory.Create performs real per-pair
// feature extraction on top of the Geometry helpers in
// Geometry.Internal.PairIntersectionMath.
public static class PairFeaturesFactory
{
    public static PairFeatures CreateEmpty(in IntersectionSet.Intersection intersection)
    {
        var vertices = Array.Empty<PairVertex>();
        var segments = Array.Empty<PairSegment>();
        return new PairFeatures(intersection, vertices, segments);
    }

    // Design notes for PairFeaturesFactory.Create:
    //
    // - Work per pair only: we take one triangle from mesh A and one from mesh B,
    //   look at their IntersectionType, and build a local set of barycentric
    //   vertices (PairVertex) and segments (PairSegment) that describe how those
    //   two triangles touch.
    //
    // - Low-level geometry (plane intersections, coplanar 2D projection, and
    //   barycentric solvers) lives in Geometry.Internal.PairIntersectionMath.
    //   This factory delegates to those helpers to obtain intersection samples
    //   and then:
    //     * converts them to barycentric coordinates on A and B,
    //     * deduplicates vertices using feature-level tolerances,
    //     * degrades noisy inputs so the output is consistent with the
    //       reported IntersectionType without reclassifying it.
    public static PairFeatures Create(
        in IntersectionSet set,
        in IntersectionSet.Intersection intersection)
    {
        if (set.TrianglesA is null) throw new ArgumentNullException(nameof(set.TrianglesA));
        if (set.TrianglesB is null) throw new ArgumentNullException(nameof(set.TrianglesB));

        var triA = set.TrianglesA[intersection.TriangleIndexA];
        var triB = set.TrianglesB[intersection.TriangleIndexB];

        var vertices = new List<PairVertex>();
        var segments = new List<PairSegment>();

        bool coplanar = TrianglePredicates.IsCoplanar(in triA, in triB);

        if (coplanar)
        {
            BuildCoplanarFeatures(in triA, in triB, intersection.Type, vertices, segments);
        }
        else
        {
            BuildNonCoplanarFeatures(in triA, in triB, intersection.Type, vertices, segments);
        }

        return new PairFeatures(intersection, vertices, segments);
    }

    private static void BuildNonCoplanarFeatures(
        in Triangle triA,
        in Triangle triB,
        IntersectionType type,
        List<PairVertex> vertices,
        List<PairSegment> segments)
    {
        var rawPoints = PairIntersectionMath.ComputeNonCoplanarIntersectionPoints(
            in triA,
            in triB,
            Tolerances.TrianglePredicateEpsilon);

        if (rawPoints.Count == 0)
        {
            if (type != IntersectionType.None)
            {
                System.Diagnostics.Debug.Assert(false,
                    "Non-empty intersection type but no non-coplanar feature vertices were found.");
            }

            return;
        }

        // Apply an additional feature-layer dedup in world space so
        // downstream barycentric merging operates on a stable set of
        // samples.
        var uniquePoints = new List<Vector>(rawPoints.Count);
        foreach (var p in rawPoints)
        {
            AddUniqueWorldPoint(uniquePoints, in p);
        }

        if (uniquePoints.Count == 0)
        {
            if (type != IntersectionType.None)
            {
                System.Diagnostics.Debug.Assert(false,
                    "Non-empty intersection type but no non-coplanar feature vertices were found after dedup.");
            }

            return;
        }

        var baryVertices = new BaryVertices();

        for (int i = 0; i < uniquePoints.Count; i++)
        {
            var v = uniquePoints[i];
            var baryA = PairIntersectionMath.ToBarycentric(in triA, in v);
            var baryB = PairIntersectionMath.ToBarycentric(in triB, in v);

            int idx = AddOrGetVertex(vertices, baryA, baryB);
            baryVertices.Add(idx, in v);
        }

        if (vertices.Count == 0)
        {
            if (type != IntersectionType.None)
            {
                System.Diagnostics.Debug.Assert(false,
                    "Non-empty intersection type but no non-coplanar feature vertices were created.");
            }

            return;
        }

        // Degrade geometry to remain consistent with the reported
        // IntersectionType without reclassifying it.
        if (type == IntersectionType.Point)
        {
            // Classifier says "point", but numerics may have produced
            // multiple nearby samples. Keep a single representative
            // vertex and drop any segments.
            if (vertices.Count == 0)
            {
                System.Diagnostics.Debug.Assert(false,
                    "IntersectionType.Point but no feature vertices were found.");
                return;
            }

            var v0 = vertices[0];
            vertices.Clear();
            vertices.Add(v0);
            segments.Clear();
            return;
        }

        if (type == IntersectionType.Segment || type == IntersectionType.Area)
        {
            // Non-coplanar triangles should not report Area, but if they
            // do we treat it as at most a segment.
            if (vertices.Count < 2)
            {
                // Segment collapsed to a point; keep at most one vertex
                // and emit no segments.
                if (vertices.Count > 1)
                {
                    var v0 = vertices[0];
                    vertices.Clear();
                    vertices.Add(v0);
                }

                segments.Clear();
                return;
            }

            // Genuine segment: connect the two farthest vertices using
            // the 3D samples as distance metric.
            baryVertices.FindFarthestPair(out int startIndex, out int endIndex);

            segments.Clear();
            if (startIndex != endIndex)
            {
                segments.Add(new PairSegment(vertices[startIndex], vertices[endIndex]));
            }

            return;
        }
    }

    private static void BuildCoplanarFeatures(
        in Triangle triA,
        in Triangle triB,
        IntersectionType type,
        List<PairVertex> vertices,
        List<PairSegment> segments)
    {
        var candidates = PairIntersectionMath.ComputeCoplanarIntersectionPoints(
            in triA,
            in triB,
            out int axis);

        if (candidates.Count == 0)
        {
            if (type != IntersectionType.None)
            {
                System.Diagnostics.Debug.Assert(false,
                    "Non-empty intersection type but no coplanar feature vertices were found.");
            }

            return;
        }

        // Map 2D intersection samples to barycentric coordinates on A and B.
        PairIntersectionMath.ProjectTriangleTo2D(in triA, axis, out var a0, out var a1, out var a2);
        PairIntersectionMath.ProjectTriangleTo2D(in triB, axis, out var b0, out var b1, out var b2);

        var baryVertices2D = new BaryVertices2D();
        for (int i = 0; i < candidates.Count; i++)
        {
            var p = candidates[i];
            var baryA = PairIntersectionMath.ToBarycentric2D(in p, in a0, in a1, in a2);
            var baryB = PairIntersectionMath.ToBarycentric2D(in p, in b0, in b1, in b2);

            int idx = AddOrGetVertex(vertices, baryA, baryB);
            baryVertices2D.Add(idx, in p);
        }

        if (vertices.Count == 0)
        {
            if (type != IntersectionType.None)
            {
                System.Diagnostics.Debug.Assert(false,
                    "Non-empty intersection type but no coplanar feature vertices were created.");
            }

            return;
        }

        // Degrade geometry according to the reported IntersectionType.
        if (type == IntersectionType.Point)
        {
            if (vertices.Count > 1)
            {
                // Classifier says "point" but we sampled multiple
                // nearby locations; keep a single representative.
                var v0 = vertices[0];
                vertices.Clear();
                vertices.Add(v0);
            }

            segments.Clear();
            return;
        }

        if (type == IntersectionType.Segment)
        {
            if (vertices.Count < 2)
            {
                // Segment collapsed to a point; keep at most one
                // vertex and emit no segments.
                if (vertices.Count > 1)
                {
                    var v0 = vertices[0];
                    vertices.Clear();
                    vertices.Add(v0);
                }

                segments.Clear();
                return;
            }

            // Find the two farthest 2D samples and connect them.
            baryVertices2D.FindFarthestPair(out int startIndex, out int endIndex);

            segments.Clear();
            if (startIndex != endIndex)
            {
                segments.Add(new PairSegment(vertices[startIndex], vertices[endIndex]));
            }

            return;
        }

        if (type == IntersectionType.Area)
        {
            // Area intersection: build a convex boundary loop from all samples.
            var orderedVertexIndices = baryVertices2D.BuildOrderedUniqueLoop();
            int uniqueCount = orderedVertexIndices.Count;
            if (uniqueCount < 3)
            {
                // Area collapsed to a lower-dimensional feature.
                if (uniqueCount == 2)
                {
                    segments.Clear();
                    segments.Add(new PairSegment(
                        vertices[orderedVertexIndices[0]],
                        vertices[orderedVertexIndices[1]]));
                }
                else
                {
                    // Single point (or none); represent as point only.
                    segments.Clear();
                }

                return;
            }

            segments.Clear();
            for (int i = 0; i < uniqueCount; i++)
            {
                int current = orderedVertexIndices[i];
                int next = orderedVertexIndices[(i + 1) % uniqueCount];
                if (current != next)
                {
                    segments.Add(new PairSegment(vertices[current], vertices[next]));
                }
            }

            return;
        }
    }

    // Two vertices are considered identical only if both their
    // barycentric coordinates on A and on B are close within
    // BarycentricEpsilon. This keeps the local vertex set stable
    // even if world-space computations produce slightly different
    // samples for the same geometric point.
    private static int AddOrGetVertex(
        List<PairVertex> vertices,
        Barycentric onA,
        Barycentric onB)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            if (v.OnTriangleA.IsCloseTo(in onA) &&
                v.OnTriangleB.IsCloseTo(in onB))
            {
                return i;
            }
        }

        var id = new IntersectionVertexId(vertices.Count);
        var vertex = new PairVertex(id, onA, onB);
        vertices.Add(vertex);
        return vertices.Count - 1;
    }

    private static void AddUniqueWorldPoint(
        List<Vector> points,
        in Vector candidate)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var existing = points[i];
            double dx = existing.X - candidate.X;
            double dy = existing.Y - candidate.Y;
            double dz = existing.Z - candidate.Z;
            double squaredDistance = dx * dx + dy * dy + dz * dz;
            if (squaredDistance <= Tolerances.FeatureWorldDistanceEpsilonSquared)
            {
                return;
            }
        }

        points.Add(candidate);
    }
}
