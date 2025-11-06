using System;
using System.Collections.Generic;

namespace Seaharp.Geometry.Bridging;

internal static class PrismBridgeStrategy
{
    public static IReadOnlyList<Tetrahedron> Build(in Triangle t1, in Triangle t2)
    {
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

        if (candidates.Count == 0)
        {
            return pack1;
        }

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

    private static bool AllPositive(IEnumerable<Tetrahedron> pack)
    {
        foreach (var tetra in pack)
        {
            if (TetrahedronOperations.AbsoluteVolume6(tetra) <= BridgeVolumeUtilities.Zero)
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
            var volume = TetrahedronOperations.AbsoluteVolume6(tetrahedron);
            if (min is null || volume < min.Value)
            {
                min = volume;
            }
        }

        return min ?? BridgeVolumeUtilities.Zero;
    }

    private static long TotalDistanceSquared(Point a, Point b, Point c, Point d, Point e, Point f)
    {
        static long DS(Point p, Point q)
        {
            var dx = p.X - q.X; var dy = p.Y - q.Y; var dz = p.Z - q.Z;
            checked
            {
                return (long)(dx * dx + dy * dy + dz * dz);
            }
        }
        return DS(a, d) + DS(b, e) + DS(c, f);
    }
}
