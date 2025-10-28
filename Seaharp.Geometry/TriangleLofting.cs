using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

#if NET8_0_OR_GREATER
using ExactScalar = System.Int128;
#else
using ExactScalar = System.Numerics.BigInteger;
#endif

namespace Seaharp.Geometry;

/// <summary>
/// Enumerates the possible lofting relationships between two triangle faces.
/// </summary>
public enum LoftCase
{
    Empty,
    SharedEdge,
    VertexOnEdge,
    SharedVertex,
    Prism3Tets,
    Octa4Tets
}

/// <summary>
/// Provides utilities for classifying how two triangles interact when attempting to loft between them.
/// </summary>
public static class TriangleLofting
{
    /// <summary>
    /// Counts the number of vertices shared between two triangles.
    /// </summary>
    /// <param name="t1">The first triangle.</param>
    /// <param name="t2">The second triangle.</param>
    /// <returns>The number of vertices common to both triangles.</returns>
    public static int SharedVertexCount(in TriangleFace t1, in TriangleFace t2)
    {
        var vertices = new HashSet<GridPoint> { t1.A, t1.B, t1.C };
        var count = 0;
        if (vertices.Contains(t2.A))
        {
            count++;
        }
        if (vertices.Contains(t2.B))
        {
            count++;
        }
        if (vertices.Contains(t2.C))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Attempts to find a shared edge between two triangles.
    /// </summary>
    /// <param name="t1">The first triangle.</param>
    /// <param name="t2">The second triangle.</param>
    /// <param name="u">The first shared vertex if successful.</param>
    /// <param name="v">The second shared vertex if successful.</param>
    /// <param name="w">The non-shared vertex from the first triangle.</param>
    /// <param name="x">The non-shared vertex from the second triangle.</param>
    /// <returns><see langword="true"/> if a shared edge exists; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSharedEdge(
        in TriangleFace t1,
        in TriangleFace t2,
        out GridPoint u,
        out GridPoint v,
        out GridPoint w,
        out GridPoint x)
    {
        var shared = new List<GridPoint>();
        var t1Vertices = new[] { t1.A, t1.B, t1.C };
        var t2Vertices = new[] { t2.A, t2.B, t2.C };

        foreach (var vertex in t1Vertices)
        {
            if (vertex.Equals(t2.A) || vertex.Equals(t2.B) || vertex.Equals(t2.C))
            {
                shared.Add(vertex);
            }
        }

        if (shared.Count == 2)
        {
            u = shared[0];
            v = shared[1];

            var wFound = false;
            w = default;
            foreach (var point in t1Vertices)
            {
                if (!point.Equals(u) && !point.Equals(v))
                {
                    w = point;
                    wFound = true;
                    break;
                }
            }

            var xFound = false;
            x = default;
            foreach (var point in t2Vertices)
            {
                if (!point.Equals(u) && !point.Equals(v))
                {
                    x = point;
                    xFound = true;
                    break;
                }
            }

            if (wFound && xFound)
            {
                return true;
            }
        }

        u = default;
        v = default;
        w = default;
        x = default;
        return false;
    }

    /// <summary>
    /// Attempts to find a vertex of one triangle that lies strictly inside an edge of another triangle.
    /// </summary>
    /// <param name="host">The triangle providing the edge.</param>
    /// <param name="other">The triangle whose vertex may lie on the edge.</param>
    /// <param name="onV">The vertex lying inside the host edge, if any.</param>
    /// <param name="e0">The first endpoint of the host edge.</param>
    /// <param name="e1">The second endpoint of the host edge.</param>
    /// <param name="o0">A remaining vertex from <paramref name="other"/>.</param>
    /// <param name="o1">The other remaining vertex from <paramref name="other"/>.</param>
    /// <returns><see langword="true"/> if such a vertex exists; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindVertexOnEdge(
        in TriangleFace host,
        in TriangleFace other,
        out GridPoint onV,
        out GridPoint e0,
        out GridPoint e1,
        out GridPoint o0,
        out GridPoint o1)
    {
        var hostEdges = new (GridPoint, GridPoint)[]
        {
            (host.A, host.B),
            (host.B, host.C),
            (host.C, host.A)
        };

        var otherVertices = new[] { other.A, other.B, other.C };

        foreach (var vertex in otherVertices)
        {
            foreach (var (a, b) in hostEdges)
            {
                if (vertex.Equals(a) || vertex.Equals(b))
                {
                    continue;
                }

                if (!Exact.Collinear(a, b, vertex))
                {
                    continue;
                }

                if (!Exact.OnSegment(a, b, vertex))
                {
                    continue;
                }

                onV = vertex;
                e0 = a;
                e1 = b;

                (o0, o1) = GetOtherVertices(other, vertex);
                return true;
            }
        }

        onV = default;
        e0 = default;
        e1 = default;
        o0 = default;
        o1 = default;
        return false;
    }

    /// <summary>
    /// Determines whether three points lie strictly on the same side of the plane containing the given triangle.
    /// </summary>
    /// <param name="face">The reference triangle defining the plane.</param>
    /// <param name="p">The first point.</param>
    /// <param name="q">The second point.</param>
    /// <param name="r">The third point.</param>
    /// <returns><see langword="true"/> if all points are strictly on the same side; otherwise, <see langword="false"/>.</returns>
    public static bool StrictSameSide(in TriangleFace face, in GridPoint p, in GridPoint q, in GridPoint r)
    {
        var orientP = Exact.Orient3D(face.A, face.B, face.C, p);
        if (orientP == default)
        {
            return false;
        }

        var signPositive = orientP > 0;

        var orientQ = Exact.Orient3D(face.A, face.B, face.C, q);
        if (orientQ == default || (orientQ > 0) != signPositive)
        {
            return false;
        }

        var orientR = Exact.Orient3D(face.A, face.B, face.C, r);
        if (orientR == default || (orientR > 0) != signPositive)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Classifies the relationship between two triangle faces.
    /// </summary>
    /// <param name="t1">The first triangle.</param>
    /// <param name="t2">The second triangle.</param>
    /// <returns>The <see cref="LoftCase"/> classification.</returns>
    public static LoftCase Classify(in TriangleFace t1, in TriangleFace t2)
    {
        if (TryFindSharedEdge(t1, t2, out _, out _, out _, out _))
        {
            return LoftCase.SharedEdge;
        }

        if (TryFindVertexOnEdge(t1, t2, out _, out _, out _, out _, out _) ||
            TryFindVertexOnEdge(t2, t1, out _, out _, out _, out _, out _))
        {
            return LoftCase.VertexOnEdge;
        }

        if (SharedVertexCount(t1, t2) == 1)
        {
            return LoftCase.SharedVertex;
        }

        if (TrianglesIntersect(t1, t2))
        {
            return LoftCase.Empty;
        }

        if (StrictSameSide(t1, t2.A, t2.B, t2.C) && StrictSameSide(t2, t1.A, t1.B, t1.C))
        {
            return LoftCase.Prism3Tets;
        }

        if (!AreCoplanar(t1, t2))
        {
            return LoftCase.Octa4Tets;
        }

        return LoftCase.Empty;
    }

    /// <summary>
    /// Generates tetrahedra that loft between two triangles based on their relationship.
    /// </summary>
    /// <param name="t1">The first triangle.</param>
    /// <param name="t2">The second triangle.</param>
    /// <returns>A read-only collection of tetrahedra connecting the triangles.</returns>
    public static IReadOnlyList<Tetrahedron> Loft(in TriangleFace t1, in TriangleFace t2)
    {
        var classification = Classify(t1, t2);
        return LoftInternal(t1, t2, classification);
    }

    /// <summary>
    /// Attempts to generate a loft between two triangles.
    /// </summary>
    /// <param name="t1">The first triangle.</param>
    /// <param name="t2">The second triangle.</param>
    /// <param name="tets">The resulting tetrahedra when successful.</param>
    /// <returns><see langword="true"/> when a non-empty loft is produced; otherwise, <see langword="false"/>.</returns>
    public static bool TryLoft(in TriangleFace t1, in TriangleFace t2, out List<Tetrahedron> tets)
    {
        var classification = Classify(t1, t2);
        if (classification == LoftCase.Empty)
        {
            tets = new List<Tetrahedron>();
            return false;
        }

        var result = LoftInternal(t1, t2, classification);
        tets = new List<Tetrahedron>(result);
        return true;
    }

    /// <summary>
    /// Provides the loft case used for explanation or debugging purposes.
    /// </summary>
    /// <param name="t1">The first triangle.</param>
    /// <param name="t2">The second triangle.</param>
    /// <returns>The determined <see cref="LoftCase"/>.</returns>
    public static LoftCase Explain(in TriangleFace t1, in TriangleFace t2) => Classify(t1, t2);

    private static IReadOnlyList<Tetrahedron> LoftInternal(in TriangleFace t1, in TriangleFace t2, LoftCase classification)
    {
        return classification switch
        {
            LoftCase.SharedEdge => LoftSharedEdge(t1, t2),
            LoftCase.VertexOnEdge => LoftVertexOnEdge(t1, t2),
            LoftCase.SharedVertex => LoftSharedVertex(t1, t2),
            LoftCase.Prism3Tets => LoftPrism3(t1, t2),
            LoftCase.Octa4Tets => LoftOcta4(t1, t2),
            _ => Array.Empty<Tetrahedron>()
        };
    }

    private static bool AreCoplanar(in TriangleFace t1, in TriangleFace t2)
    {
        if (Exact.Orient3D(t1.A, t1.B, t1.C, t2.A) != default)
        {
            return false;
        }
        if (Exact.Orient3D(t1.A, t1.B, t1.C, t2.B) != default)
        {
            return false;
        }
        if (Exact.Orient3D(t1.A, t1.B, t1.C, t2.C) != default)
        {
            return false;
        }

        return true;
    }

    private static (GridPoint, GridPoint) GetOtherVertices(in TriangleFace triangle, in GridPoint excluded)
    {
        GridPoint? first = null;
        GridPoint? second = null;

        if (!triangle.A.Equals(excluded))
        {
            first = triangle.A;
        }

        if (!triangle.B.Equals(excluded))
        {
            if (first is null)
            {
                first = triangle.B;
            }
            else
            {
                second = triangle.B;
            }
        }

        if (!triangle.C.Equals(excluded))
        {
            if (first is null)
            {
                first = triangle.C;
            }
            else if (second is null)
            {
                second = triangle.C;
            }
        }

        if (first is null || second is null)
        {
            throw new InvalidOperationException("Expected exactly two remaining vertices.");
        }

        return (first.Value, second.Value);
    }

    private static IReadOnlyList<Tetrahedron> LoftSharedEdge(in TriangleFace t1, in TriangleFace t2)
    {
        if (TryFindSharedEdge(t1, t2, out var u, out var v, out var w, out var x))
        {
            return new List<Tetrahedron> { new Tetrahedron(u, v, w, x) };
        }

        return Array.Empty<Tetrahedron>();
    }

    private static IReadOnlyList<Tetrahedron> LoftVertexOnEdge(in TriangleFace t1, in TriangleFace t2)
    {
        TriangleFace host = t1;
        GridPoint onV;
        GridPoint e0;
        GridPoint e1;
        GridPoint o0;
        GridPoint o1;

        if (!TryFindVertexOnEdge(host, t2, out onV, out e0, out e1, out o0, out o1))
        {
            host = t2;
            if (!TryFindVertexOnEdge(host, t1, out onV, out e0, out e1, out o0, out o1))
            {
                return Array.Empty<Tetrahedron>();
            }
        }

        var hostThird = GetOppositeVertex(host, e0, e1);

        var equatorVolume = Exact.AbsVol6(e0, e1, o0, o1);
        var includeEquator = equatorVolume != default;

        var hostCapO0Volume = Exact.AbsVol6(host.A, host.B, host.C, o0);
        var hostCapO1Volume = Exact.AbsVol6(host.A, host.B, host.C, o1);
        var otherCapVolume = Exact.AbsVol6(hostThird, onV, o0, o1);

        var candidate0Min = MinVolumes(hostCapO0Volume, otherCapVolume, includeEquator ? equatorVolume : (ExactScalar?)null);
        var candidate1Min = MinVolumes(hostCapO1Volume, otherCapVolume, includeEquator ? equatorVolume : (ExactScalar?)null);

        var result = new List<Tetrahedron>();

        if (includeEquator)
        {
            result.Add(new Tetrahedron(e0, e1, o0, o1));
        }

        if (candidate1Min > candidate0Min)
        {
            result.Add(new Tetrahedron(host.A, host.B, host.C, o1));
        }
        else
        {
            result.Add(new Tetrahedron(host.A, host.B, host.C, o0));
        }

        result.Add(new Tetrahedron(hostThird, onV, o0, o1));

        return result;
    }

    private static IReadOnlyList<Tetrahedron> LoftSharedVertex(in TriangleFace t1, in TriangleFace t2)
    {
        if (!TryExtractSharedVertexConfiguration(t1, t2, out var shared, out var b, out var c, out var d, out var e))
        {
            return Array.Empty<Tetrahedron>();
        }

        var equatorVolume = Exact.AbsVol6(b, c, d, e);
        var includeEquator = equatorVolume != default;

        var capDVolume = Exact.AbsVol6(shared, b, c, d);
        var capEVolume = Exact.AbsVol6(shared, b, c, e);
        var complementDVolume = Exact.AbsVol6(shared, b, d, e);
        var complementEVolume = Exact.AbsVol6(shared, c, d, e);

        var candidateDMin = MinVolumes(capDVolume, complementDVolume, includeEquator ? equatorVolume : (ExactScalar?)null);
        var candidateEMin = MinVolumes(capEVolume, complementEVolume, includeEquator ? equatorVolume : (ExactScalar?)null);

        var result = new List<Tetrahedron>();
        if (includeEquator)
        {
            result.Add(new Tetrahedron(b, c, d, e));
        }

        if (candidateEMin > candidateDMin)
        {
            result.Add(new Tetrahedron(shared, b, c, e));
            result.Add(new Tetrahedron(shared, c, d, e));
        }
        else
        {
            result.Add(new Tetrahedron(shared, b, c, d));
            result.Add(new Tetrahedron(shared, b, d, e));
        }

        return result;
    }

    private static IReadOnlyList<Tetrahedron> LoftPrism3(in TriangleFace t1, in TriangleFace t2)
    {
        var cycles = new[]
        {
            new[] { t2.A, t2.B, t2.C },
            new[] { t2.B, t2.C, t2.A },
            new[] { t2.C, t2.A, t2.B }
        };

        Tetrahedron[]? bestPack = null;
        ExactScalar? bestScore = null;

        foreach (var cycle in cycles)
        {
            var pack = SelectPrismPack(t1, cycle[0], cycle[1], cycle[2]);
            var score = PackMinVolume(pack);

            if (bestPack is null || score > bestScore)
            {
                bestPack = pack;
                bestScore = score;
            }
        }

        return bestPack ?? Array.Empty<Tetrahedron>();
    }

    private static IReadOnlyList<Tetrahedron> LoftOcta4(in TriangleFace t1, in TriangleFace t2)
    {
        var apex1 = SelectApex(t1, t2);
        var apex2 = SelectApex(t2, t1);

        var (t1Eq0, t1Eq1) = GetOtherVertices(t1, apex1);
        var (t2Eq0, t2Eq1) = GetOtherVertices(t2, apex2);

        var p0 = t1Eq0;
        var p1 = t1Eq1;
        var q0 = t2Eq0;
        var q1 = t2Eq1;

        var diag1Vol1 = Exact.AbsVol6(apex1, p0, q0, p1);
        var diag1Vol2 = Exact.AbsVol6(apex1, p0, p1, q1);
        var diag1Vol3 = Exact.AbsVol6(apex2, p0, q0, p1);
        var diag1Vol4 = Exact.AbsVol6(apex2, p0, p1, q1);
        var diag1Min = MinVolumes(diag1Vol1, diag1Vol2, diag1Vol3, diag1Vol4);

        var diag2Vol1 = Exact.AbsVol6(apex1, p0, q0, q1);
        var diag2Vol2 = Exact.AbsVol6(apex1, q1, p1, p0);
        var diag2Vol3 = Exact.AbsVol6(apex2, p0, q0, q1);
        var diag2Vol4 = Exact.AbsVol6(apex2, q1, p1, p0);
        var diag2Min = MinVolumes(diag2Vol1, diag2Vol2, diag2Vol3, diag2Vol4);

        var result = new List<Tetrahedron>(4);

        if (diag2Min > diag1Min)
        {
            result.Add(new Tetrahedron(apex1, p0, q0, q1));
            result.Add(new Tetrahedron(apex1, q1, p1, p0));
            result.Add(new Tetrahedron(apex2, p0, q0, q1));
            result.Add(new Tetrahedron(apex2, q1, p1, p0));
        }
        else
        {
            result.Add(new Tetrahedron(apex1, p0, q0, p1));
            result.Add(new Tetrahedron(apex1, p0, p1, q1));
            result.Add(new Tetrahedron(apex2, p0, q0, p1));
            result.Add(new Tetrahedron(apex2, p0, p1, q1));
        }

        return result;
    }

    private static Tetrahedron[] SelectPrismPack(in TriangleFace baseTriangle, in GridPoint d, in GridPoint e, in GridPoint f)
    {
        var pack1 = new[]
        {
            new Tetrahedron(baseTriangle.A, baseTriangle.B, baseTriangle.C, d),
            new Tetrahedron(baseTriangle.B, baseTriangle.C, d, e),
            new Tetrahedron(baseTriangle.C, d, e, f),
        };

        var pack2 = new[]
        {
            new Tetrahedron(baseTriangle.A, baseTriangle.B, baseTriangle.C, e),
            new Tetrahedron(baseTriangle.A, baseTriangle.C, e, f),
            new Tetrahedron(baseTriangle.A, e, f, d),
        };

        return ChooseBestPack(pack1, pack2);
    }

    private static Tetrahedron[] ChooseBestPack(Tetrahedron[] pack1, Tetrahedron[] pack2)
    {
        var candidates = new List<(Tetrahedron[] Pack, ExactScalar Score)>();

        if (AllPositive(pack1))
        {
            candidates.Add((pack1, PackMinVolume(pack1)));
        }

        if (AllPositive(pack2))
        {
            candidates.Add((pack2, PackMinVolume(pack2)));
        }

        if (candidates.Count > 0)
        {
            var best = candidates[0];
            for (var i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].Score > best.Score)
                {
                    best = candidates[i];
                }
            }

            return best.Pack;
        }

        var score1 = PackMinVolume(pack1);
        var score2 = PackMinVolume(pack2);
        return score1 >= score2 ? pack1 : pack2;
    }

    private static bool AllPositive(IEnumerable<Tetrahedron> pack)
    {
        foreach (var tetra in pack)
        {
            if (TetraAbsVolume6(tetra) <= Zero)
            {
                return false;
            }
        }

        return true;
    }

    private static ExactScalar PackMinVolume(IEnumerable<Tetrahedron> pack)
    {
        ExactScalar? min = null;
        foreach (var tetrahedron in pack)
        {
            var volume = TetraAbsVolume6(tetrahedron);
            if (min is null || volume < min.Value)
            {
                min = volume;
            }
        }

        return min ?? Zero;
    }

    private static ExactScalar TetraAbsVolume6(Tetrahedron tetra)
    {
        var vertices = tetra.Vertices;
        return Exact.AbsVol6(vertices[0], vertices[1], vertices[2], vertices[3]);
    }

    private static GridPoint GetOppositeVertex(in TriangleFace triangle, GridPoint edge0, GridPoint edge1)
    {
        if (!triangle.A.Equals(edge0) && !triangle.A.Equals(edge1))
        {
            return triangle.A;
        }
        if (!triangle.B.Equals(edge0) && !triangle.B.Equals(edge1))
        {
            return triangle.B;
        }
        return triangle.C;
    }

    private static bool TryExtractSharedVertexConfiguration(
        in TriangleFace t1,
        in TriangleFace t2,
        out GridPoint shared,
        out GridPoint b,
        out GridPoint c,
        out GridPoint d,
        out GridPoint e)
    {
        shared = default;
        b = default;
        c = default;
        d = default;
        e = default;

        var vertices1 = new[] { t1.A, t1.B, t1.C };
        var vertices2 = new[] { t2.A, t2.B, t2.C };

        GridPoint? sharedCandidate = null;
        foreach (var v in vertices1)
        {
            if (v.Equals(vertices2[0]) || v.Equals(vertices2[1]) || v.Equals(vertices2[2]))
            {
                sharedCandidate = v;
                break;
            }
        }

        if (sharedCandidate is null)
        {
            return false;
        }

        shared = sharedCandidate.Value;
        var others1 = GetOtherVertices(t1, shared);
        var others2 = GetOtherVertices(t2, shared);

        b = others1.Item1;
        c = others1.Item2;
        d = others2.Item1;
        e = others2.Item2;
        return true;
    }

    private static GridPoint SelectApex(in TriangleFace source, in TriangleFace target)
    {
        var vertices = new[] { source.A, source.B, source.C };
        var targetVertices = new[] { target.A, target.B, target.C };

        var bestIndex = 0;
        var bestValue = Exact.AbsVol6(targetVertices[0], targetVertices[1], targetVertices[2], vertices[0]);

        for (var i = 1; i < 3; i++)
        {
            var volume = Exact.AbsVol6(targetVertices[0], targetVertices[1], targetVertices[2], vertices[i]);
            if (volume > bestValue)
            {
                bestValue = volume;
                bestIndex = i;
            }
        }

        return vertices[bestIndex];
    }

    private static bool SegmentIntersectsTriangle(in GridPoint p, in GridPoint q, in TriangleFace tri)
    {
        var s0 = Exact.Orient3D(tri.A, tri.B, tri.C, p);
        var s1 = Exact.Orient3D(tri.A, tri.B, tri.C, q);

        if (s0 == Zero && s1 == Zero)
        {
            if (Exact.OnSegment(tri.A, tri.B, p) || Exact.OnSegment(tri.B, tri.C, p) || Exact.OnSegment(tri.C, tri.A, p))
            {
                return true;
            }

            if (Exact.OnSegment(tri.A, tri.B, q) || Exact.OnSegment(tri.B, tri.C, q) || Exact.OnSegment(tri.C, tri.A, q))
            {
                return true;
            }

            return false;
        }

        if ((s0 > Zero && s1 > Zero) || (s0 < Zero && s1 < Zero))
        {
            return false;
        }

        var u0 = Exact.Orient3D(p, q, tri.A, tri.B);
        var u1 = Exact.Orient3D(p, q, tri.B, tri.C);
        var u2 = Exact.Orient3D(p, q, tri.C, tri.A);

        if (u0 == Zero || u1 == Zero || u2 == Zero)
        {
            return true;
        }

        var pos = 0;
        var neg = 0;

        if (u0 > Zero) pos++; else if (u0 < Zero) neg++;
        if (u1 > Zero) pos++; else if (u1 < Zero) neg++;
        if (u2 > Zero) pos++; else if (u2 < Zero) neg++;

        return pos == 3 || neg == 3;
    }

    private static bool TrianglesIntersect(in TriangleFace t1, in TriangleFace t2)
    {
        if (SegmentIntersectsTriangle(t1.A, t1.B, t2)) return true;
        if (SegmentIntersectsTriangle(t1.B, t1.C, t2)) return true;
        if (SegmentIntersectsTriangle(t1.C, t1.A, t2)) return true;

        if (SegmentIntersectsTriangle(t2.A, t2.B, t1)) return true;
        if (SegmentIntersectsTriangle(t2.B, t2.C, t1)) return true;
        if (SegmentIntersectsTriangle(t2.C, t2.A, t1)) return true;

        return false;
    }

    private static ExactScalar MinVolumes(ExactScalar first, ExactScalar second, ExactScalar? third = null, ExactScalar? fourth = null)
    {
        var min = first < second ? first : second;
        if (third.HasValue && third.Value < min)
        {
            min = third.Value;
        }
        if (fourth.HasValue && fourth.Value < min)
        {
            min = fourth.Value;
        }
        return min;
    }

    private static ExactScalar MinVolumes(ExactScalar first, ExactScalar second, ExactScalar third)
    {
        var min = first < second ? first : second;
        if (third < min)
        {
            min = third;
        }
        return min;
    }

    private static ExactScalar Zero => default;
}
