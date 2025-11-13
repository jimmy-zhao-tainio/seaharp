using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.Topology;

// Computes intersection polylines (closed loops) between two closed surfaces by
// intersecting their constituent triangles pairwise and stitching resulting segments.
// This is a first stepping stone toward mesh booleans: it only extracts the loops.
// IMPORTANT: All intersection vertices are snapped to the integer grid (Point).
public static class IntersectionSegments
{
    // Returns a list of closed loops. Each loop is expressed as an ordered list of
    // grid points (Geometry.Point). The last vertex equals the first.
    // Coplanar triangle overlaps are currently ignored.
    public static List<List<Point>> BuildLoops(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var segments = new List<(Point P, Point Q)>();
        var trisA = a.Triangles; var trisB = b.Triangles;
        for (int i = 0; i < trisA.Count; i++)
        {
            var ta = trisA[i];
            for (int j = 0; j < trisB.Count; j++)
            {
                var tb = trisB[j];
                if (TryTriangleTriangleSegment(ta, tb, epsilon, out var p0, out var p1))
                {
                    if (!(p0.X == p1.X && p0.Y == p1.Y && p0.Z == p1.Z))
                        segments.Add((p0, p1));
                }
            }
        }

        return BuildClosedLoopsFromSegments(segments);
    }

    // Builds per-triangle cut segments: for every triangle in A (and B), collects the
    // snapped segment that lies inside the opposite surface. Each entry is a list because
    // a triangle may be intersected more than once in complex cases. Endpoints are on the
    // triangle edges (snapped Points). Degenerate zero-length segments are omitted.
    public sealed class IntersectionCutsResult
    {
        public required List<(Point P, Point Q)>[] CutsA { get; init; }
        public required List<(Point P, Point Q)>[] CutsB { get; init; }
    }

    public static IntersectionCutsResult BuildCuts(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var trisA = a.Triangles; var trisB = b.Triangles;
        var cutsA = new List<(Point, Point)>[trisA.Count];
        var cutsB = new List<(Point, Point)>[trisB.Count];
        for (int i = 0; i < trisA.Count; i++) cutsA[i] = new List<(Point, Point)>();
        for (int j = 0; j < trisB.Count; j++) cutsB[j] = new List<(Point, Point)>();

        for (int i = 0; i < trisA.Count; i++)
        {
            var ta = trisA[i];
            var planeB = Plane.FromTriangle(ta); // not used; avoid premature alloc
        }

        for (int i = 0; i < trisA.Count; i++)
        {
            var ta = trisA[i];
            var planeB = Plane.FromTriangle(ta); // dummy for symmetry
            for (int j = 0; j < trisB.Count; j++)
            {
                var tb = trisB[j];

                if (TryEndpointsOnTriangleEdges(ta, tb, epsilon, out var a0, out var a1, out var b0, out var b1))
                {
                    if (!(a0.Equals(a1))) cutsA[i].Add((a0, a1));
                    if (!(b0.Equals(b1))) cutsB[j].Add((b0, b1));
                }
            }
        }

        return new IntersectionCutsResult { CutsA = cutsA, CutsB = cutsB };
    }

    // For a triangle pair, returns the two endpoints on triangle A (lying on A's edges and inside B)
    // and the two endpoints on triangle B (lying on B's edges and inside A). Points are snapped.
    private static bool TryEndpointsOnTriangleEdges(in Triangle a, in Triangle b, double eps, out Point a0, out Point a1, out Point b0, out Point b1)
    {
        a0 = a1 = b0 = b1 = default;
        var at = a; var bt = b;

        static (double x, double y, double z) LerpD(in Point p, in Point q, double t)
            => (p.X + (q.X - p.X) * t, p.Y + (q.Y - p.Y) * t, p.Z + (q.Z - p.Z) * t);

        static Point Snap(double x, double y, double z)
            => new Point((long)Math.Round(x, MidpointRounding.AwayFromZero),
                         (long)Math.Round(y, MidpointRounding.AwayFromZero),
                         (long)Math.Round(z, MidpointRounding.AwayFromZero));

        bool EdgePlaneHitD(in Point p, in Point q, in Plane plane, out Vector hit)
        {
            var s0 = plane.Evaluate(p);
            var s1 = plane.Evaluate(q);
            hit = default;

            bool s0On = Math.Abs(s0) <= eps;
            bool s1On = Math.Abs(s1) <= eps;

            if (s0On && s1On) return false; // coplanar edge: skip for cuts here
            if (s0On) { hit = new Vector(p.X, p.Y, p.Z); return true; }
            if (s1On) { hit = new Vector(q.X, q.Y, q.Z); return true; }
            if ((s0 > 0 && s1 > 0) || (s0 < 0 && s1 < 0)) return false;
            double t = s0 / (s0 - s1);
            if (t < -1e-12 || t > 1.0 + 1e-12) return false;
            var hd = LerpD(p, q, t);
            hit = new Vector(hd.x, hd.y, hd.z);
            return true;
        }

        bool CollectOnA(in Triangle tb, in Plane planeB, out List<Point> pts)
        {
            pts = new List<Point>(2);
            if (EdgePlaneHitD(at.P0, at.P1, planeB, out var h01) && PointInTriangleD(h01, tb, eps)) pts.Add(Snap(h01.X, h01.Y, h01.Z));
            if (EdgePlaneHitD(at.P1, at.P2, planeB, out var h12) && PointInTriangleD(h12, tb, eps)) pts.Add(Snap(h12.X, h12.Y, h12.Z));
            if (EdgePlaneHitD(at.P2, at.P0, planeB, out var h20) && PointInTriangleD(h20, tb, eps)) pts.Add(Snap(h20.X, h20.Y, h20.Z));
            // Dedup
            if (pts.Count > 1 && pts[0].Equals(pts[1])) pts.RemoveAt(1);
            return pts.Count >= 2;
        }

        bool CollectOnB(in Triangle ta, in Plane planeA, out List<Point> pts)
        {
            pts = new List<Point>(2);
            if (EdgePlaneHitD(bt.P0, bt.P1, planeA, out var h01) && PointInTriangleD(h01, ta, eps)) pts.Add(Snap(h01.X, h01.Y, h01.Z));
            if (EdgePlaneHitD(bt.P1, bt.P2, planeA, out var h12) && PointInTriangleD(h12, ta, eps)) pts.Add(Snap(h12.X, h12.Y, h12.Z));
            if (EdgePlaneHitD(bt.P2, bt.P0, planeA, out var h20) && PointInTriangleD(h20, ta, eps)) pts.Add(Snap(h20.X, h20.Y, h20.Z));
            if (pts.Count > 1 && pts[0].Equals(pts[1])) pts.RemoveAt(1);
            return pts.Count >= 2;
        }

        var planeA = Plane.FromTriangle(at);
        var planeB = Plane.FromTriangle(bt);

        bool okA = CollectOnA(b, planeB, out var ptsA);
        bool okB = CollectOnB(a, planeA, out var ptsB);

        if (!(okA && okB)) return false;

        // If more than two due to degeneracy, choose farthest two
        (Point, Point) PickPair(List<Point> list)
        {
            if (list.Count == 2) return (list[0], list[1]);
            double best = -1; int ia = 0, ib = 1;
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    double d2 = Dist2(list[i], list[j]);
                    if (d2 > best) { best = d2; ia = i; ib = j; }
                }
            return (list[ia], list[ib]);
        }

        (a0, a1) = PickPair(ptsA);
        (b0, b1) = PickPair(ptsB);
        return true;
    }

    // --- Segment stitching helpers ---
    private static List<List<Point>> BuildClosedLoopsFromSegments(List<(Point P, Point Q)> segments)
    {
        var adj = new Dictionary<Point, List<Point>>();
        var edgeMulti = new Dictionary<EdgeKey, int>();

        void AddEdge(Point a, Point b)
        {
            if (a.Equals(b)) return;
            if (!adj.TryGetValue(a, out var la)) { la = new List<Point>(2); adj[a] = la; }
            if (!adj.TryGetValue(b, out var lb)) { lb = new List<Point>(2); adj[b] = lb; }
            la.Add(b); lb.Add(a);
            var key = new EdgeKey(a, b);
            edgeMulti.TryGetValue(key, out int c); edgeMulti[key] = c + 1;
        }

        foreach (var s in segments) AddEdge(s.P, s.Q);

        var loops = new List<List<Point>>();

        // Iterate while any edge remains unused
        while (true)
        {
            EdgeKey? seed = null;
            foreach (var kv in edgeMulti)
            {
                if (kv.Value > 0) { seed = kv.Key; break; }
            }
            if (seed is null) break;

            var loop = WalkLoop(seed.Value.A, seed.Value.B);
            if (loop != null) loops.Add(loop);
            else break; // if we failed to build a loop, stop to avoid infinite work
        }

        return loops;

        List<Point>? WalkLoop(Point start, Point next)
        {
            var loop = new List<Point>(16) { start };
            Point prev = start; Point curr = next;
            MarkUsed(prev, curr);
            while (true)
            {
                loop.Add(curr);
                if (curr.Equals(start))
                {
                    // Closed
                    if (!loop[^1].Equals(loop[0])) loop.Add(loop[0]);
                    return loop;
                }

                if (!adj.TryGetValue(curr, out var nbrs) || nbrs.Count == 0) return null; // open chain

                // Choose next unused edge, prefer not going back to prev unless it's the only option
                Point? chosen = null;
                for (int i = 0; i < nbrs.Count; i++)
                {
                    var nb = nbrs[i];
                    if (nb.Equals(prev)) continue;
                    if (HasAvailable(curr, nb)) { chosen = nb; break; }
                }
                if (chosen is null)
                {
                    // Try going back if that's the only remaining unused
                    for (int i = 0; i < nbrs.Count; i++)
                    {
                        var nb = nbrs[i];
                        if (HasAvailable(curr, nb)) { chosen = nb; break; }
                    }
                    if (chosen is null) return null; // stuck
                }
                prev = curr; curr = chosen.Value; MarkUsed(prev, curr);
            }
        }

        void MarkUsed(Point a, Point b)
        {
            var key = new EdgeKey(a, b);
            if (!edgeMulti.TryGetValue(key, out int left) || left <= 0) return;
            edgeMulti[key] = left - 1;
        }

        bool HasAvailable(Point a, Point b)
        {
            var key = new EdgeKey(a, b);
            return edgeMulti.TryGetValue(key, out int left) && left > 0;
        }
    }

    private static bool TryTriangleTriangleSegment(in Triangle a, in Triangle b, double eps, out Point p0, out Point p1)
    {
        var hitsD = new List<Vector>(4);
        var pa0 = a.P0; var pa1 = a.P1; var pa2 = a.P2;
        var pb0 = b.P0; var pb1 = b.P1; var pb2 = b.P2;

        var planeA = Plane.FromTriangle(a);
        var planeB = Plane.FromTriangle(b);

        // Helper local
        static (double x, double y, double z) LerpD(in Point p, in Point q, double t)
            => (p.X + (q.X - p.X) * t, p.Y + (q.Y - p.Y) * t, p.Z + (q.Z - p.Z) * t);

        static Point Snap(double x, double y, double z)
            => new Point((long)Math.Round(x, MidpointRounding.AwayFromZero),
                         (long)Math.Round(y, MidpointRounding.AwayFromZero),
                         (long)Math.Round(z, MidpointRounding.AwayFromZero));

        bool EdgePlaneHitD(in Point p, in Point q, in Plane plane, out Vector hit)
        {
            var s0 = plane.Evaluate(p);
            var s1 = plane.Evaluate(q);
            hit = default;

            bool s0On = Math.Abs(s0) <= eps;
            bool s1On = Math.Abs(s1) <= eps;

            if (s0On && s1On)
            {
                // Coplanar edge; skip in this first pass
                return false;
            }

            if (s0On)
            {
                hit = new Vector(p.X, p.Y, p.Z);
                return true;
            }
            if (s1On)
            {
                hit = new Vector(q.X, q.Y, q.Z);
                return true;
            }

            // Proper crossing
            if (s0 > 0 && s1 > 0) return false;
            if (s0 < 0 && s1 < 0) return false;

            double t = s0 / (s0 - s1);
            if (t < -1e-12 || t > 1.0 + 1e-12) return false;
            var hd = LerpD(p, q, t);
            hit = new Vector(hd.x, hd.y, hd.z);
            return true;
        }

        void Collect(in Triangle tOther, in Plane planeOther, in Point e0, in Point e1, in Point e2)
        {
            if (EdgePlaneHitD(e0, e1, planeOther, out var h01) && PointInTriangleD(h01, tOther, eps)) hitsD.Add(h01);
            if (EdgePlaneHitD(e1, e2, planeOther, out var h12) && PointInTriangleD(h12, tOther, eps)) hitsD.Add(h12);
            if (EdgePlaneHitD(e2, e0, planeOther, out var h20) && PointInTriangleD(h20, tOther, eps)) hitsD.Add(h20);
        }

        Collect(b, planeB, pa0, pa1, pa2);
        Collect(a, planeA, pb0, pb1, pb2);

        // Snap and dedup
        var unique = new List<Point>(4);
        foreach (var h in hitsD)
        {
            var p = Snap(h.X, h.Y, h.Z);
            bool dup = false;
            for (int i = 0; i < unique.Count; i++)
            {
                if (p.Equals(unique[i])) { dup = true; break; }
            }
            if (!dup) unique.Add(p);
        }

        if (unique.Count < 2)
        {
            p0 = default; p1 = default; return false;
        }
        if (unique.Count == 2)
        {
            p0 = unique[0]; p1 = unique[1]; return true;
        }

        // More than two candidates (rare): choose the farthest pair
        double maxD2 = -1; int ia = 0, ib = 1;
        for (int i = 0; i < unique.Count; i++)
        {
            for (int j = i + 1; j < unique.Count; j++)
            {
                double d2 = Dist2(unique[i], unique[j]);
                if (d2 > maxD2) { maxD2 = d2; ia = i; ib = j; }
            }
        }
        p0 = unique[ia]; p1 = unique[ib];
        return true;
    }

    private static bool PointInTriangleD(in Vector p, in Triangle t, double eps)
    {
        var p0 = new Vector(t.P0.X, t.P0.Y, t.P0.Z);
        var v0 = new Vector(t.P2.X - t.P0.X, t.P2.Y - t.P0.Y, t.P2.Z - t.P0.Z);
        var v1 = new Vector(t.P1.X - t.P0.X, t.P1.Y - t.P0.Y, t.P1.Z - t.P0.Z);
        var v2 = new Vector(p.X - t.P0.X, p.Y - t.P0.Y, p.Z - t.P0.Z);

        double dot00 = v0.Dot(v0);
        double dot01 = v0.Dot(v1);
        double dot02 = v0.Dot(v2);
        double dot11 = v1.Dot(v1);
        double dot12 = v1.Dot(v2);

        double denom = dot00 * dot11 - dot01 * dot01;
        if (Math.Abs(denom) <= 1e-18) return false; // degenerate
        double inv = 1.0 / denom;
        double u = (dot11 * dot02 - dot01 * dot12) * inv;
        double v = (dot00 * dot12 - dot01 * dot02) * inv;
        return u >= -eps && v >= -eps && (u + v) <= 1.0 + eps;
    }

    private static double Dist2(in Point a, in Point b)
    {
        double dx = (double)a.X - b.X, dy = (double)a.Y - b.Y, dz = (double)a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}
