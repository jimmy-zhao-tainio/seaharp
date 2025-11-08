using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World.Predicates;

public static class ShapePredicates
{
    // Returns true when the shape boundary is manifold (no edge-usage anomalies).
    // Validity here mirrors the manifold condition: every undirected edge is used exactly twice.
    public static bool IsValid(Seaharp.World.Shape shape)
        => SurfacePredicates.IsManifold(new Seaharp.World.Surface(shape));

    // Legacy name retained; now reports true when any non-manifold edge is found.
    public static bool HasCoplanarEdgeConflicts(Seaharp.World.Shape shape)
        => !IsValid(shape);

    private static void AddEdge(Dictionary<EdgeKey, List<int>> map, EdgeKey k, int triIndex)
    {
        if (!map.TryGetValue(k, out var list))
        {
            list = new List<int>(2);
            map[k] = list;
        }
        list.Add(triIndex);
    }

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        private readonly Point A;
        private readonly Point B;
        public EdgeKey(in Point p, in Point q)
        { if (Lt(p, q)) { A = p; B = q; } else { A = q; B = p; } }
        public bool Equals(EdgeKey other) => A.Equals(other.A) && B.Equals(other.B);
        public override bool Equals(object? obj) => obj is EdgeKey e && Equals(e);
        public override int GetHashCode()
        { var hc = new HashCode(); hc.Add(A.X); hc.Add(A.Y); hc.Add(A.Z); hc.Add(B.X); hc.Add(B.Y); hc.Add(B.Z); return hc.ToHashCode(); }
        private static bool Lt(in Point p, in Point q)
        { if (p.X != q.X) return p.X < q.X; if (p.Y != q.Y) return p.Y < q.Y; return p.Z < q.Z; }
    }

    // I128V utility left out since coplanarity test was removed in favor of manifold check.
}
