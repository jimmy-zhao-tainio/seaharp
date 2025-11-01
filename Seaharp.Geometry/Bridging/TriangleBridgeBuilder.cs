using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Seaharp.Geometry.Bridging;
public enum BridgeCase
{
    Empty,
    SharedEdge,
    VertexOnEdge,
    SharedVertex,
    Prism3Tets,
    Octa4Tets
}

public static class TriangleBridgeBuilder
{
    public static int SharedVertexCount(in Triangle t1, in Triangle t2)
    {
        var vertices = new HashSet<Point> { t1.A, t1.B, t1.C };
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

    public static bool TryFindSharedEdge(
        in Triangle t1,
        in Triangle t2,
        out Point u,
        out Point v,
        out Point w,
        out Point x)
    {
        var shared = new List<Point>();
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

    public static bool TryFindVertexOnEdge(
        in Triangle host,
        in Triangle other,
        out Point onV,
        out Point e0,
        out Point e1,
        out Point o0,
        out Point o1)
    {
        var hostEdges = new (Point, Point)[]
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

                if (!GeometryChecks.ArePointsOnSameLine(a, b, vertex))
                {
                    continue;
                }

                if (!GeometryChecks.IsPointOnLine(a, b, vertex))
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

    public static BridgeCase Classify(in Triangle t1, in Triangle t2)
    {
        if (TryFindSharedEdge(t1, t2, out _, out _, out _, out _))
        {
            return BridgeCase.SharedEdge;
        }

        if (TryFindVertexOnEdge(t1, t2, out _, out _, out _, out _, out _) ||
            TryFindVertexOnEdge(t2, t1, out _, out _, out _, out _, out _))
        {
            return BridgeCase.VertexOnEdge;
        }

        if (SharedVertexCount(t1, t2) == 1)
        {
            return BridgeCase.SharedVertex;
        }

        if (GeometryChecks.DoesTriangleIntersectTriangle(t1, t2))
        {
            return BridgeCase.Empty;
        }

        var t2Strict = GeometryChecks.ArePointsStrictlyOnSameSideOfTrianglePlane(t1, t2.A, t2.B, t2.C);
        var t1Strict = GeometryChecks.ArePointsStrictlyOnSameSideOfTrianglePlane(t2, t1.A, t1.B, t1.C);

        if (t1Strict && t2Strict)
        {
            return BridgeCase.Prism3Tets;
        }

        return BridgeCase.Empty;
    }

    public static IReadOnlyList<Tetrahedron> Connect(in Triangle t1, in Triangle t2)
    {
        var classification = Classify(t1, t2);
        return BuildConnection(t1, t2, classification);
    }

    public static bool TryConnect(in Triangle t1, in Triangle t2, out List<Tetrahedron> tets)
    {
        var classification = Classify(t1, t2);
        if (classification == BridgeCase.Empty)
        {
            tets = new List<Tetrahedron>();
            return false;
        }

        var result = BuildConnection(t1, t2, classification);
        tets = new List<Tetrahedron>(result);
        return true;
    }

    public static BridgeCase Explain(in Triangle t1, in Triangle t2) => Classify(t1, t2);

    private static IReadOnlyList<Tetrahedron> BuildConnection(in Triangle t1, in Triangle t2, BridgeCase classification)
    {
        return classification switch
        {
            BridgeCase.SharedEdge => BuildSharedEdgeConnection(t1, t2),
            BridgeCase.VertexOnEdge => BuildVertexOnEdgeConnection(t1, t2),
            BridgeCase.SharedVertex => BuildSharedVertexConnection(t1, t2),
            BridgeCase.Prism3Tets => BuildPrismConnection(t1, t2),
            _ => Array.Empty<Tetrahedron>()
        };
    }

    private static (Point, Point) GetOtherVertices(in Triangle triangle, in Point excluded)
    {
        Point? first = null;
        Point? second = null;

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

    private static IReadOnlyList<Tetrahedron> BuildSharedEdgeConnection(in Triangle t1, in Triangle t2)
    {
        if (TryFindSharedEdge(t1, t2, out var u, out var v, out var w, out var x))
        {
            return new List<Tetrahedron> { new Tetrahedron(u, v, w, x) };
        }

        return Array.Empty<Tetrahedron>();
    }

    private static IReadOnlyList<Tetrahedron> BuildVertexOnEdgeConnection(in Triangle t1, in Triangle t2)
    {
        Triangle host = t1;
        Point onV;
        Point e0;
        Point e1;
        Point o0;
        Point o1;

        if (!TryFindVertexOnEdge(host, t2, out onV, out e0, out e1, out o0, out o1))
        {
            host = t2;
            if (!TryFindVertexOnEdge(host, t1, out onV, out e0, out e1, out o0, out o1))
            {
                return Array.Empty<Tetrahedron>();
            }
        }

        var hostThird = GetOppositeVertex(host, e0, e1);

        var equatorVolume = IntegerMath.AbsoluteTetrahedronVolume6(e0, e1, o0, o1);
        var includeEquator = equatorVolume != default;

        var hostCapO0Volume = IntegerMath.AbsoluteTetrahedronVolume6(host.A, host.B, host.C, o0);
        var hostCapO1Volume = IntegerMath.AbsoluteTetrahedronVolume6(host.A, host.B, host.C, o1);
        var otherCapVolume = IntegerMath.AbsoluteTetrahedronVolume6(hostThird, onV, o0, o1);

        var candidate0Min = MinVolumes(hostCapO0Volume, otherCapVolume, includeEquator ? equatorVolume : (Int128?)null);
        var candidate1Min = MinVolumes(hostCapO1Volume, otherCapVolume, includeEquator ? equatorVolume : (Int128?)null);

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

    private static IReadOnlyList<Tetrahedron> BuildSharedVertexConnection(in Triangle t1, in Triangle t2)
    {
        if (!TryExtractSharedVertexConfiguration(t1, t2, out var shared, out var b, out var c, out var d, out var e))
        {
            return Array.Empty<Tetrahedron>();
        }

        var equatorVolume = IntegerMath.AbsoluteTetrahedronVolume6(b, c, d, e);
        var includeEquator = equatorVolume != default;

        var capDVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, b, c, d);
        var capEVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, b, c, e);
        var complementDVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, b, d, e);
        var complementEVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, c, d, e);

        var candidateDMin = MinVolumes(capDVolume, complementDVolume, includeEquator ? equatorVolume : (Int128?)null);
        var candidateEMin = MinVolumes(capEVolume, complementEVolume, includeEquator ? equatorVolume : (Int128?)null);

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

        private static IReadOnlyList<Tetrahedron> BuildPrismConnection(in Triangle t1, in Triangle t2)
    {
        // Try both orientation directions for t2 (cycles and reversed cycles)
        var cycles = new[]
        {
            new[] { t2.A, t2.B, t2.C },
            new[] { t2.B, t2.C, t2.A },
            new[] { t2.C, t2.A, t2.B },
            new[] { t2.A, t2.C, t2.B },
            new[] { t2.C, t2.B, t2.A },
            new[] { t2.B, t2.A, t2.C }
        };

        Tetrahedron[]? bestPack = null;
        Int128? bestScore = null;
        long bestDistance = long.MaxValue;

        foreach (var cycle in cycles)
        {
            // Prefer the mapping that minimises total squared distances between paired vertices
            var dist = TotalDistanceSquared(t1.A, t1.B, t1.C, cycle[0], cycle[1], cycle[2]);
            var pack = SelectPrismPack(t1, cycle[0], cycle[1], cycle[2]);
            var score = PackMinVolume(pack);

            if (bestPack is null || dist < bestDistance || (dist == bestDistance && score > bestScore))
            {
                bestPack = pack;
                bestScore = score;
                bestDistance = dist;
            }
        }

        return bestPack ?? Array.Empty<Tetrahedron>();
    }

    private static long TotalDistanceSquared(Point a, Point b, Point c, Point d, Point e, Point f)
    {
        static long DS(Point p, Point q)
        {
            var dx = p.X - q.X; var dy = p.Y - q.Y; var dz = p.Z - q.Z;
            // coordinates are small in our use; checked avoids silent overflow in extreme cases
            checked
            {
                return (long)(dx * dx + dy * dy + dz * dz);
            }
        }
        return DS(a, d) + DS(b, e) + DS(c, f);
    }

    private static Tetrahedron[] SelectPrismPack(in Triangle baseTriangle, in Point d, in Point e, in Point f)
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
        var candidates = new List<(Tetrahedron[] Pack, Int128 Score)>();

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

    private static Int128 PackMinVolume(IEnumerable<Tetrahedron> pack)
    {
        Int128? min = null;
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

    private static Int128 TetraAbsVolume6(Tetrahedron tetra)
    {
        var vertices = tetra.Vertices;
        return IntegerMath.AbsoluteTetrahedronVolume6(vertices[0], vertices[1], vertices[2], vertices[3]);
    }

    private static Point GetOppositeVertex(in Triangle triangle, Point edge0, Point edge1)
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
        in Triangle t1,
        in Triangle t2,
        out Point shared,
        out Point b,
        out Point c,
        out Point d,
        out Point e)
    {
        shared = default;
        b = default;
        c = default;
        d = default;
        e = default;

        var vertices1 = new[] { t1.A, t1.B, t1.C };
        var vertices2 = new[] { t2.A, t2.B, t2.C };

        Point? sharedCandidate = null;
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

    private static Int128 MinVolumes(Int128 first, Int128 second, Int128? third = null, Int128? fourth = null)
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

    private static Int128 MinVolumes(Int128 first, Int128 second, Int128 third)
    {
        var min = first < second ? first : second;
        if (third < min)
        {
            min = third;
        }
        return min;
    }

    private static Int128 Zero => default;
}

