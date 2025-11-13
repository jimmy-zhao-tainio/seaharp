using System;
using System.Collections.Generic;
using System.Linq;
using Seaharp.Geometry;
using Seaharp.Geometry.Computation;

namespace Seaharp.Topology;

// Utility to split a triangle by chord segments whose endpoints lie on the triangle edges (grid-snapped Points),
// then classify and triangulate the resulting sub-polygons.
//
// TODO: Handle crossing chords within a single triangle (introduce interior vertices when two
//       chords intersect away from edges). Current approach splits sequentially by chords that
//       lie on the current polygon boundary.
// TODO: Deduplicate collinear vertices introduced by snapping to keep triangulation clean.
// TODO: Consider ear-clipping (or constrained triangulation) for complex polygons instead of a fan.
internal static class TriangleSplitter
{
    private struct EdgeParam
    {
        public int EdgeIndex; // 0: P0->P1, 1: P1->P2, 2: P2->P0
        public double T;      // param in [0,1]
        public Point P;       // snapped point
    }

    public static IEnumerable<IReadOnlyList<Point>> SplitIntoPolygons(Triangle t, Point s0, Point s1)
    {
        if (s0.Equals(s1)) yield break;
        
        if (!TryEdgeParam(t.P0, t.P1, s0, 0, out var e0) && !TryEdgeParam(t.P1, t.P2, s0, 1, out e0) && !TryEdgeParam(t.P2, t.P0, s0, 2, out e0)) yield break;
        if (!TryEdgeParam(t.P0, t.P1, s1, 0, out var e1) && !TryEdgeParam(t.P1, t.P2, s1, 1, out e1) && !TryEdgeParam(t.P2, t.P0, s1, 2, out e1)) yield break;
        if (e0.EdgeIndex == e1.EdgeIndex && Math.Abs(e0.T - e1.T) < 1e-12) yield break;

        // Build ring of triangle with inserted points along edges
        var ring = new List<Point>();
        void AddEdge(Point a, Point b, int edgeIndex)
        {
            var list = new List<EdgeParam>();
            void AddIfMatch(EdgeParam ep) { if (ep.EdgeIndex == edgeIndex) list.Add(ep); }
            AddIfMatch(e0); AddIfMatch(e1);
            list.Sort((x, y) => x.T.CompareTo(y.T));
            ring.Add(a);
            foreach (var ep in list) ring.Add(ep.P);
            // b is added by next edge or at the end
        }

        AddEdge(t.P0, t.P1, 0);
        AddEdge(t.P1, t.P2, 1);
        AddEdge(t.P2, t.P0, 2);
        ring.Add(t.P0);

        // Find indices of s0, s1 in ring
        int i0 = ring.FindIndex(p => p.Equals(s0));
        int i1 = ring.FindIndex(p => p.Equals(s1));
        if (i0 < 0 || i1 < 0) yield break;

        IReadOnlyList<Point> Path(int from, int to)
        {
            var res = new List<Point>();
            for (int k = from; k != to; k = (k + 1) % ring.Count)
                res.Add(ring[k]);
            res.Add(ring[to]);
            return res;
        }

        var pathA = Path(i0, i1);
        var pathB = Path(i1, i0);

        // Form two polygons by closing with chord s1->s0 or s0->s1 respectively
        var polyA = new List<Point>(pathA.Count + 1);
        polyA.AddRange(pathA);
        polyA.Add(s0);

        var polyB = new List<Point>(pathB.Count + 1);
        polyB.AddRange(pathB);
        polyB.Add(s1);

        if (polyA.Count >= 3) yield return NormalizePolygon(polyA);
        if (polyB.Count >= 3) yield return NormalizePolygon(polyB);
    }

    // General polygon split by a chord whose endpoints lie on the polygon boundary (possibly in the interior of edges).
    // Returns two polygons if the split is applicable; otherwise returns zero.
    public static IEnumerable<IReadOnlyList<Point>> SplitPolygonByChord(IReadOnlyList<Point> polygon, Point s0, Point s1)
    {
        if (s0.Equals(s1)) yield break;
        if (polygon.Count < 3) yield break;

        // Build ring with s0/s1 inserted along edges in traversal order
        var ring = new List<Point>(polygon.Count + 2);
        bool s0Inserted = false, s1Inserted = false;

        static bool OnSeg(in Point a, in Point b, in Point p)
        {
            long minX = Math.Min(a.X, b.X), maxX = Math.Max(a.X, b.X);
            long minY = Math.Min(a.Y, b.Y), maxY = Math.Max(a.Y, b.Y);
            long minZ = Math.Min(a.Z, b.Z), maxZ = Math.Max(a.Z, b.Z);
            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY || p.Z < minZ || p.Z > maxZ) return false;
            var v0 = new Vector(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var v1 = new Vector(p.X - a.X, p.Y - a.Y, p.Z - a.Z);
            var c = v0.Cross(v1);
            return c.X == 0 && c.Y == 0 && c.Z == 0;
        }

        static double Param(in Point a, in Point b, in Point p)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
            if (Math.Abs(dx) >= Math.Abs(dy) && Math.Abs(dx) >= Math.Abs(dz)) return (p.X - a.X) / dx;
            if (Math.Abs(dy) >= Math.Abs(dz)) return (p.Y - a.Y) / dy; else return (p.Z - a.Z) / dz;
        }

        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            ring.Add(a);

            bool s0On = !s0Inserted && (s0.Equals(a) || s0.Equals(b) || OnSeg(a, b, s0));
            bool s1On = !s1Inserted && (s1.Equals(a) || s1.Equals(b) || OnSeg(a, b, s1));
            if (!s0On && !s1On) continue;

            if (s0On && s1On)
            {
                // If both fall on the same edge, insert them by param order between a and b
                var t0 = Param(a, b, s0);
                var t1 = Param(a, b, s1);
                if (t0 < t1)
                {
                    if (!s0.Equals(a)) ring.Add(s0);
                    if (!s1.Equals(b)) ring.Add(s1);
                }
                else
                {
                    if (!s1.Equals(a)) ring.Add(s1);
                    if (!s0.Equals(b)) ring.Add(s0);
                }
                s0Inserted = s1Inserted = true;
            }
            else if (s0On)
            {
                if (!(s0.Equals(a) || s0.Equals(b))) ring.Add(s0);
                s0Inserted = true;
            }
            else if (s1On)
            {
                if (!(s1.Equals(a) || s1.Equals(b))) ring.Add(s1);
                s1Inserted = true;
            }
        }

        if (!(s0Inserted && s1Inserted)) yield break;

        // Normalize ring (remove consecutive dups)
        for (int i = ring.Count - 2; i >= 0; i--)
            if (ring[i].Equals(ring[i + 1])) ring.RemoveAt(i + 1);

        int i0 = ring.FindIndex(p => p.Equals(s0));
        int i1 = ring.FindIndex(p => p.Equals(s1));
        if (i0 < 0 || i1 < 0) yield break;

        IReadOnlyList<Point> Path(int from, int to)
        {
            var res = new List<Point>();
            for (int k = from; k != to; k = (k + 1) % ring.Count)
                res.Add(ring[k]);
            res.Add(ring[to]);
            return res;
        }

        var pathA = Path(i0, i1);
        var pathB = Path(i1, i0);

        var polyA = new List<Point>(pathA.Count + 1);
        polyA.AddRange(pathA);
        polyA.Add(s0);

        var polyB = new List<Point>(pathB.Count + 1);
        polyB.AddRange(pathB);
        polyB.Add(s1);

        if (polyA.Count >= 3) yield return NormalizePolygon(polyA);
        if (polyB.Count >= 3) yield return NormalizePolygon(polyB);
    }

    private static IReadOnlyList<Point> NormalizePolygon(List<Point> poly)
    {
        // Remove consecutive duplicates
        for (int i = poly.Count - 2; i >= 0; i--)
            if (poly[i].Equals(poly[i + 1])) poly.RemoveAt(i + 1);
        if (poly[0].Equals(poly[^1])) poly.RemoveAt(poly.Count - 1);
        return poly;
    }

    private static bool TryEdgeParam(in Point a, in Point b, in Point p, int edgeIndex, out EdgeParam ep)
    {
        ep = default;
        if (!IsOnSegment(a, b, p)) return false;
        double dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
        double t = 0.0;
        if (Math.Abs(dx) >= Math.Abs(dy) && Math.Abs(dx) >= Math.Abs(dz)) t = (p.X - a.X) / dx;
        else if (Math.Abs(dy) >= Math.Abs(dz)) t = (p.Y - a.Y) / dy; else t = (p.Z - a.Z) / dz;
        ep.EdgeIndex = edgeIndex;
        ep.T = t; ep.P = p;
        return true;
    }

    private static bool IsOnSegment(in Point a, in Point b, in Point p)
    {
        long minX = Math.Min(a.X, b.X), maxX = Math.Max(a.X, b.X);
        long minY = Math.Min(a.Y, b.Y), maxY = Math.Max(a.Y, b.Y);
        long minZ = Math.Min(a.Z, b.Z), maxZ = Math.Max(a.Z, b.Z);
        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY || p.Z < minZ || p.Z > maxZ) return false;
        var v0 = new Vector(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var v1 = new Vector(p.X - a.X, p.Y - a.Y, p.Z - a.Z);
        var c = v0.Cross(v1);
        double area2 = Math.Sqrt(c.X * c.X + c.Y * c.Y + c.Z * c.Z);
        return area2 == 0.0; // exact colinearity on integer grid
    }

    public static IEnumerable<Triangle> TriangulatePolygon(Triangle basis, IReadOnlyList<Point> polygon)
    {
        if (polygon.Count < 3) yield break;
        // Fan triangulation around polygon[0]; attempt to match basis normal orientation
        // TODO: Use constrained triangulation if polygons become non-convex or contain many collinear points.
        var n = basis.Normal;
        var p0 = polygon[0];
        for (int i = 1; i + 1 < polygon.Count; i++)
        {
            var p1 = polygon[i]; var p2 = polygon[i + 1];
            var t = Triangle.FromWinding(p0, p1, p2);
            // Ensure normal aligns with basis
            var dot = t.Normal.Dot(new Vector(n.X, n.Y, n.Z));
            if (dot < 0)
            {
                t = Triangle.FromWinding(p0, p2, p1);
            }
            yield return t;
        }
    }
}
