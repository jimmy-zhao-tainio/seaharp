using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.Topology;

// Computes intersection polylines (closed loops) between two closed surfaces by
// intersecting their constituent triangles pairwise and stitching resulting segments.
// This is a first stepping stone toward mesh booleans: it only extracts the loops.
// IMPORTANT: All intersection vertices are snapped to the integer grid (Point).
//
// TODO: Performance — current implementation checks all triangle pairs (O(n*m)).
//       Integrate BoundingBoxTree BVH to prune non-overlapping triangle pairs.
// TODO: Coplanar triangles are currently skipped; add explicit coplanar handling
//       (merge or classify) to avoid missing boundary segments on overlapping sheets.
// TODO: Robustness — review epsilon use for plane/inside checks vs integer snapping
//       to ensure consistent behavior on grazing intersections and shared vertices.
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

    // Computes integer center for a closed loop (last equals first) by averaging
    // unique vertices and rounding away from zero per component using Int128.
    private static Point LoopCenter(IReadOnlyList<Point> loop)
    {
        int count = loop.Count;
        if (count == 0) return default;
        int n = loop[^1].Equals(loop[0]) ? count - 1 : count;
        if (n <= 0) return loop[0];

        Int128 sx = 0, sy = 0, sz = 0;
        for (int i = 0; i < n; i++)
        {
            sx += loop[i].X; sy += loop[i].Y; sz += loop[i].Z;
        }
        static long RoundAway(Int128 num, Int128 den)
        {
            if (num >= 0) return (long)((num + (den / 2)) / den);
            else return (long)(-((-num + (den / 2)) / den));
        }
        var d = (Int128)n;
        long cx = RoundAway(sx, d);
        long cy = RoundAway(sy, d);
        long cz = RoundAway(sz, d);
        return new Point(cx, cy, cz);
    }

    // Builds triangle fans (discs) from each loop to its integer center.
    // Triangles are formed as (pi, pi+1, center) for each edge of the loop.
    public static List<Triangle> BuildLoopDiscs(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        var loops = BuildLoops(a, b, epsilon);
        var tris = new List<Triangle>();
        foreach (var loop in loops)
        {
            if (loop.Count < 3) continue;
            var center = LoopCenter(loop);
            int n = loop[^1].Equals(loop[0]) ? loop.Count - 1 : loop.Count;
            for (int i = 0; i < n; i++)
            {
                var p = loop[i];
                var q = loop[(i + 1) % n];
                if (p.Equals(q) || p.Equals(center) || q.Equals(center)) continue;
                tris.Add(Triangle.FromWinding(p, q, center));
            }
        }
        return tris;
    }

    public sealed class BridgeTriangles
    {
        public required List<Triangle> A { get; init; }
        public required List<Triangle> B { get; init; }
    }

    // Builds zipper-like bridge strips between the intersection seam (loops) and anchor
    // vertices from intersected triangles on each side. Does not split shells: it only
    // emits new triangles that connect seam edges to anchors and across adjacent anchors.
    public static BridgeTriangles BuildBridgeTriangles(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var loops = BuildLoops(a, b, epsilon);
        var prov = BuildCutsProvenance(a, b, epsilon);

        var anchorA = new Dictionary<EdgeKey, Point>();
        var anchorB = new Dictionary<EdgeKey, Point>();

        // Map each snapped segment to its anchor vertex on A
        for (int i = 0; i < prov.CutsA.Length; i++)
        {
            var list = prov.CutsA[i];
            var tri = a.Triangles[i];
            foreach (var cut in list)
            {
                var key = new EdgeKey(cut.A.P, cut.B.P);
                if (!anchorA.ContainsKey(key))
                {
                    int sv = SharedVertexIndex(cut.A.Edge, cut.B.Edge);
                    var v = sv switch { 0 => tri.P0, 1 => tri.P1, 2 => tri.P2, _ => ClosestVertex(tri, cut.A.P, cut.B.P) };
                    anchorA[key] = v;
                }
            }
        }
        // And for B
        for (int j = 0; j < prov.CutsB.Length; j++)
        {
            var list = prov.CutsB[j];
            var tri = b.Triangles[j];
            foreach (var cut in list)
            {
                var key = new EdgeKey(cut.A.P, cut.B.P);
                if (!anchorB.ContainsKey(key))
                {
                    int sv = SharedVertexIndex(cut.A.Edge, cut.B.Edge);
                    var v = sv switch { 0 => tri.P0, 1 => tri.P1, 2 => tri.P2, _ => ClosestVertex(tri, cut.A.P, cut.B.P) };
                    anchorB[key] = v;
                }
            }
        }

        var outA = new List<Triangle>();
        var outB = new List<Triangle>();

        foreach (var loop in loops)
        {
            if (loop.Count < 3) continue;
            int n = loop[^1].Equals(loop[0]) ? loop.Count - 1 : loop.Count;
            for (int i = 0; i < n; i++)
            {
                var p = loop[i];
                var q = loop[(i + 1) % n];
                var k = new EdgeKey(p, q);

                if (anchorA.TryGetValue(k, out var a0))
                {
                    // Triangle from seam edge to anchor
                    TryAdd(outA, p, q, a0);
                    // Connect to next anchor if available
                    var kNext = new EdgeKey(q, loop[(i + 2) % n]);
                    if (anchorA.TryGetValue(kNext, out var a1))
                    {
                        TryAdd(outA, q, a0, a1);
                    }
                }
                if (anchorB.TryGetValue(k, out var b0))
                {
                    TryAdd(outB, p, q, b0);
                    var kNext = new EdgeKey(q, loop[(i + 2) % n]);
                    if (anchorB.TryGetValue(kNext, out var b1))
                    {
                        TryAdd(outB, q, b0, b1);
                    }
                }
            }
        }

        return new BridgeTriangles { A = outA, B = outB };

        static void TryAdd(List<Triangle> dst, Point p0, Point p1, Point p2)
        {
            if (p0.Equals(p1) || p1.Equals(p2) || p2.Equals(p0)) return;
            // Skip degenerate by checking area via cross product length
            double ux = (double)p1.X - p0.X, uy = (double)p1.Y - p0.Y, uz = (double)p1.Z - p0.Z;
            double vx = (double)p2.X - p0.X, vy = (double)p2.Y - p0.Y, vz = (double)p2.Z - p0.Z;
            double cx = uy * vz - uz * vy;
            double cy = uz * vx - ux * vz;
            double cz = ux * vy - uy * vx;
            double l2 = cx * cx + cy * cy + cz * cz;
            if (l2 <= 0.0) return;
            dst.Add(Triangle.FromWinding(p0, p1, p2));
        }
    }

    public sealed class CutTriangles
    {
        public required List<Triangle> A { get; init; }
        public required List<Triangle> B { get; init; }
    }

    // Builds small visualization triangles per segment using the two segment endpoints
    // plus one vertex from the intersected triangle (the shared vertex of the two
    // intersected edges when determinable; otherwise the closest triangle vertex).
    public static CutTriangles BuildCutTriangles(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var cuts = BuildCuts(a, b, epsilon);
        var trisA = new List<Triangle>();
        var trisB = new List<Triangle>();

        for (int i = 0; i < a.Triangles.Count; i++)
        {
            var t = a.Triangles[i];
            foreach (var seg in cuts.CutsA[i])
            {
                if (TryBuildTriangle(t, seg.P, seg.Q, out var tri)) trisA.Add(tri);
            }
        }
        for (int j = 0; j < b.Triangles.Count; j++)
        {
            var t = b.Triangles[j];
            foreach (var seg in cuts.CutsB[j])
            {
                if (TryBuildTriangle(t, seg.P, seg.Q, out var tri)) trisB.Add(tri);
            }
        }

        return new CutTriangles { A = trisA, B = trisB };
    }

    // Returns the exact triangles from A and B that produced an intersection cut
    // (i.e., triangles that were crossed). Uses the provenance variant to avoid
    // recomputing which triangles were touched.
    public sealed class TouchedTriangles
    {
        public required List<Triangle> A { get; init; }
        public required List<Triangle> B { get; init; }
    }

    public static TouchedTriangles ExtractTouchedTriangles(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        // Union of two detections for coverage:
        // 1) BuildCuts-based (same as deletion pipeline)
        // 2) Direct tri-pair intersection test (TryTriangleTriangleSegment)
        var cuts = BuildCuts(a, b, epsilon);
        var hitA = new bool[a.Triangles.Count];
        var hitB = new bool[b.Triangles.Count];
        for (int i = 0; i < cuts.CutsA.Length; i++) if (cuts.CutsA[i].Count > 0) hitA[i] = true;
        for (int j = 0; j < cuts.CutsB.Length; j++) if (cuts.CutsB[j].Count > 0) hitB[j] = true;

        for (int i = 0; i < a.Triangles.Count; i++)
        {
            var ta = a.Triangles[i];
            for (int j = 0; j < b.Triangles.Count; j++)
            {
                var tb = b.Triangles[j];
                if (TryTriangleTriangleSegment(ta, tb, epsilon, out _, out _))
                {
                    hitA[i] = true; hitB[j] = true;
                }
            }
        }

        var listA = new List<Triangle>();
        var listB = new List<Triangle>();
        for (int i = 0; i < hitA.Length; i++) if (hitA[i]) listA.Add(a.Triangles[i]);
        for (int j = 0; j < hitB.Length; j++) if (hitB[j]) listB.Add(b.Triangles[j]);
        return new TouchedTriangles { A = listA, B = listB };
    }

    private static bool TryBuildTriangle(in Triangle t, in Point p, in Point q, out Triangle tri)
    {
        tri = default;
        if (p.Equals(q)) return false;
        int e1 = EdgeIndex(t, p);
        int e2 = EdgeIndex(t, q);

        Point vShared;
        if (e1 != -1 && e2 != -1 && e1 != e2)
        {
            int sv = SharedVertexIndex(e1, e2);
            vShared = sv switch { 0 => t.P0, 1 => t.P1, 2 => t.P2, _ => ClosestVertex(t, p, q) };
        }
        else
        {
            vShared = ClosestVertex(t, p, q);
        }

        if (vShared.Equals(p) || vShared.Equals(q)) return false;
        tri = Triangle.FromWinding(p, q, vShared);
        return true;
    }

    private static int EdgeIndex(in Triangle t, in Point p)
    {
        if (OnSeg(t.P0, t.P1, p)) return 0;
        if (OnSeg(t.P1, t.P2, p)) return 1;
        if (OnSeg(t.P2, t.P0, p)) return 2;
        return -1;
    }
    private static bool OnSeg(in Point a, in Point b, in Point p)
    {
        long minX = Math.Min(a.X, b.X), maxX = Math.Max(a.X, b.X);
        long minY = Math.Min(a.Y, b.Y), maxY = Math.Max(a.Y, b.Y);
        long minZ = Math.Min(a.Z, b.Z), maxZ = Math.Max(a.Z, b.Z);
        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY || p.Z < minZ || p.Z > maxZ) return false;
        var v0 = new Vector(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var v1 = new Vector(p.X - a.X, p.Y - a.Y, p.Z - a.Z);
        var c = v0.Cross(v1);
        return Math.Abs(c.X) < 1e-9 && Math.Abs(c.Y) < 1e-9 && Math.Abs(c.Z) < 1e-9;
    }
    private static int SharedVertexIndex(int e1, int e2)
    {
        int[] cnt = new int[3];
        void Add(int e)
        {
            if (e == 0) { cnt[0]++; cnt[1]++; }
            else if (e == 1) { cnt[1]++; cnt[2]++; }
            else if (e == 2) { cnt[2]++; cnt[0]++; }
        }
        Add(e1); Add(e2);
        for (int i = 0; i < 3; i++) if (cnt[i] == 2) return i;
        return -1;
    }
    private static Point ClosestVertex(in Triangle t, in Point p, in Point q)
    {
        var v0 = t.P0; var v1 = t.P1; var v2 = t.P2;
        long d0 = Math.Min(Dist2L(v0, p), Dist2L(v0, q));
        long d1 = Math.Min(Dist2L(v1, p), Dist2L(v1, q));
        long d2 = Math.Min(Dist2L(v2, p), Dist2L(v2, q));
        if (d0 <= d1 && d0 <= d2) return v0;
        if (d1 <= d2) return v1;
        return v2;
    }

    private static long Dist2L(in Point a, in Point b)
    {
        long dx = a.X - b.X; long dy = a.Y - b.Y; long dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    // Detailed cut data carrying per-endpoint edge provenance and pre-snap param.
    public readonly struct CutEndpoint
    {
        // Snapped grid point
        public readonly Point P;
        // Edge index on the triangle: 0=P0->P1, 1=P1->P2, 2=P2->P0
        public readonly int Edge;
        // Parametric position along the edge in [0,1] before snapping
        public readonly double T;
        // Rational representation of T for exact ordering
        public readonly Int128 TNum;
        public readonly Int128 TDen; // always > 0
        public CutEndpoint(Point p, int edge, double t)
        {
            P = p; Edge = edge; T = t; TNum = 0; TDen = 1;
        }
        public CutEndpoint(Point p, int edge, Int128 tNum, Int128 tDen)
        {
            P = p; Edge = edge; TNum = tNum; TDen = tDen; T = (double)tNum / (double)tDen;
        }
    }

    public readonly struct TriangleCut
    {
        public readonly CutEndpoint A;
        public readonly CutEndpoint B;
        // Pair identity to synchronize keep/drop across surfaces
        public readonly int AIndex;
        public readonly int BIndex;
        public TriangleCut(CutEndpoint a, CutEndpoint b, int aIndex, int bIndex)
        { A = a; B = b; AIndex = aIndex; BIndex = bIndex; }
    }

    public sealed class IntersectionCutsWithProvenance
    {
        public required List<TriangleCut>[] CutsA { get; init; }
        public required List<TriangleCut>[] CutsB { get; init; }
    }

    // Builds per-triangle cuts with edge provenance for both surfaces.
    // Keeps old BuildCuts API unchanged; this richer variant will let
    // downstream splitters avoid re-detecting which edge was hit.
    public static IntersectionCutsWithProvenance BuildCutsProvenance(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var trisA = a.Triangles; var trisB = b.Triangles;
        var cutsA = new List<TriangleCut>[trisA.Count];
        var cutsB = new List<TriangleCut>[trisB.Count];
        for (int i = 0; i < trisA.Count; i++) cutsA[i] = new List<TriangleCut>();
        for (int j = 0; j < trisB.Count; j++) cutsB[j] = new List<TriangleCut>();

        static Point SnapInt(Point p, Point q, Int128 num, Int128 den)
        {
            // Computes round_away(p + (q-p)*num/den) per component using Int128
            long RoundAway(Int128 n, Int128 d)
            {
                if (n >= 0) return (long)((n + (d / 2)) / d);
                else return (long)(-((-n + (d / 2)) / d));
            }
            var dx = (Int128)q.X - (Int128)p.X;
            var dy = (Int128)q.Y - (Int128)p.Y;
            var dz = (Int128)q.Z - (Int128)p.Z;
            var nx = (Int128)p.X * den + dx * num;
            var ny = (Int128)p.Y * den + dy * num;
            var nz = (Int128)p.Z * den + dz * num;
            return new Point(RoundAway(nx, den), RoundAway(ny, den), RoundAway(nz, den));
        }

        // Exact 3D orientation using Int128
        static Int128 Orient(in Point a, in Point b, in Point c, in Point p)
        {
            Int128 abx = (Int128)b.X - a.X;
            Int128 aby = (Int128)b.Y - a.Y;
            Int128 abz = (Int128)b.Z - a.Z;
            Int128 acx = (Int128)c.X - a.X;
            Int128 acy = (Int128)c.Y - a.Y;
            Int128 acz = (Int128)c.Z - a.Z;
            // n = ab x ac
            Int128 nx = aby * acz - abz * acy;
            Int128 ny = abz * acx - abx * acz;
            Int128 nz = abx * acy - aby * acx;
            Int128 apx = (Int128)p.X - a.X;
            Int128 apy = (Int128)p.Y - a.Y;
            Int128 apz = (Int128)p.Z - a.Z;
            return nx * apx + ny * apy + nz * apz;
        }

        // Returns optional rational t and snapped point for edge p->q against triangle other's plane
        static bool EdgePlaneHitExact(in Point p, in Point q, in Triangle other, out Int128 tNum, out Int128 tDen, out Point snapped)
        {
            var s0 = Orient(other.P0, other.P1, other.P2, p);
            var s1 = Orient(other.P0, other.P1, other.P2, q);
            tNum = 0; tDen = 1; snapped = default;
            if (s0 == 0 && s1 == 0) return false; // coplanar
            // same sign -> no crossing
            if ((s0 > 0 && s1 > 0) || (s0 < 0 && s1 < 0)) return false;
            var den = s0 - s1;
            if (den == 0) return false;
            if (den < 0) { den = -den; s0 = -s0; }
            // 0 <= s0/den <= 1
            if (s0 < 0 || s0 > den) return false;
            tNum = s0; tDen = den;
            snapped = SnapInt(p, q, tNum, tDen);
            return true;
        }

        for (int i = 0; i < trisA.Count; i++)
        {
            var ta = trisA[i]; var planeB = Plane.FromTriangle(ta); // unused; symmetry placeholder
        }

        for (int i = 0; i < trisA.Count; i++)
        {
            var ta = trisA[i];
            var planeB = Plane.FromTriangle(ta); // dummy
            for (int j = 0; j < trisB.Count; j++)
            {
                var tb = trisB[j];
                var planeA = Plane.FromTriangle(tb);

                // Collect endpoints on A
                var aPts = new List<CutEndpoint>(2);
                if (EdgePlaneHitExact(ta.P0, ta.P1, tb, out var n01, out var d01, out var p01))
                { aPts.Add(new CutEndpoint(p01, 0, n01, d01)); }
                if (EdgePlaneHitExact(ta.P1, ta.P2, tb, out var n12, out var d12, out var p12))
                { aPts.Add(new CutEndpoint(p12, 1, n12, d12)); }
                if (EdgePlaneHitExact(ta.P2, ta.P0, tb, out var n20, out var d20, out var p20))
                { aPts.Add(new CutEndpoint(p20, 2, n20, d20)); }

                // Filter: only keep endpoints that fall inside the other triangle
                var aIn = new List<CutEndpoint>(2);
                foreach (var ep in aPts)
                {
                    if (PointInTriangleInt(tb, ep.P)) aIn.Add(ep);
                }
                // Dedup by snapped point
                if (aIn.Count > 1 && aIn[0].P.Equals(aIn[1].P)) aIn.RemoveAt(1);
                if (aIn.Count >= 2)
                {
                    var (ia, ib) = FarthestPair(aIn);
                    cutsA[i].Add(new TriangleCut(aIn[ia], aIn[ib], aIndex: i, bIndex: j));
                }

                // Collect endpoints on B
                var bPts = new List<CutEndpoint>(2);
                if (EdgePlaneHitExact(tb.P0, tb.P1, ta, out var bn01, out var bd01, out var bp01))
                { bPts.Add(new CutEndpoint(bp01, 0, bn01, bd01)); }
                if (EdgePlaneHitExact(tb.P1, tb.P2, ta, out var bn12, out var bd12, out var bp12))
                { bPts.Add(new CutEndpoint(bp12, 1, bn12, bd12)); }
                if (EdgePlaneHitExact(tb.P2, tb.P0, ta, out var bn20, out var bd20, out var bp20))
                { bPts.Add(new CutEndpoint(bp20, 2, bn20, bd20)); }

                var bIn = new List<CutEndpoint>(2);
                foreach (var ep in bPts)
                {
                    if (PointInTriangleInt(ta, ep.P)) bIn.Add(ep);
                }
                if (bIn.Count > 1 && bIn[0].P.Equals(bIn[1].P)) bIn.RemoveAt(1);
                if (bIn.Count >= 2)
                {
                    var (ja, jb) = FarthestPair(bIn);
                    cutsB[j].Add(new TriangleCut(bIn[ja], bIn[jb], aIndex: i, bIndex: j));
                }
            }
        }

        return new IntersectionCutsWithProvenance { CutsA = cutsA, CutsB = cutsB };

        static (int ia, int ib) FarthestPair(List<CutEndpoint> pts)
        {
            if (pts.Count == 2) return (0, 1);
            double best = -1; int ia = 0, ib = 1;
            for (int i = 0; i < pts.Count; i++)
                for (int j = i + 1; j < pts.Count; j++)
                {
                    double d2 = Dist2(pts[i].P, pts[j].P);
                    if (d2 > best) { best = d2; ia = i; ib = j; }
                }
            return (ia, ib);
        }

        static bool PointInTriangleInt(in Triangle t, in Point p)
        {
            // Choose dominant axis of normal to project to 2D
            Int128 abx = (Int128)t.P1.X - t.P0.X;
            Int128 aby = (Int128)t.P1.Y - t.P0.Y;
            Int128 abz = (Int128)t.P1.Z - t.P0.Z;
            Int128 acx = (Int128)t.P2.X - t.P0.X;
            Int128 acy = (Int128)t.P2.Y - t.P0.Y;
            Int128 acz = (Int128)t.P2.Z - t.P0.Z;
            Int128 nx = aby * acz - abz * acy;
            Int128 ny = abz * acx - abx * acz;
            Int128 nz = abx * acy - aby * acx;
            Int128 ax = nx >= 0 ? nx : -nx;
            Int128 ay = ny >= 0 ? ny : -ny;
            Int128 az = nz >= 0 ? nz : -nz;
            int axis = (ax >= ay && ax >= az) ? 0 : (ay >= az ? 1 : 2);

            static void Proj(in Point s, int axis, out Int128 u, out Int128 v)
            {
                if (axis == 0) { u = s.Y; v = s.Z; }
                else if (axis == 1) { u = s.X; v = s.Z; }
                else { u = s.X; v = s.Y; }
            }
            static Int128 Cross2(Int128 ux, Int128 uy, Int128 vx, Int128 vy) => ux * vy - uy * vx;

            Proj(t.P0, axis, out var a0x, out var a0y);
            Proj(t.P1, axis, out var a1x, out var a1y);
            Proj(t.P2, axis, out var a2x, out var a2y);
            Proj(p,    axis, out var px,  out var py);

            var v0x = a1x - a0x; var v0y = a1y - a0y;
            var v1x = a2x - a1x; var v1y = a2y - a1y;
            var v2x = a0x - a2x; var v2y = a0y - a2y;

            var w0x = px - a0x; var w0y = py - a0y;
            var w1x = px - a1x; var w1y = py - a1y;
            var w2x = px - a2x; var w2y = py - a2y;

            var c0 = Cross2(v0x, v0y, w0x, w0y);
            var c1 = Cross2(v1x, v1y, w1x, w1y);
            var c2 = Cross2(v2x, v2y, w2x, w2y);

            bool pos = c0 >= 0 && c1 >= 0 && c2 >= 0;
            bool neg = c0 <= 0 && c1 <= 0 && c2 <= 0;
            return pos || neg;
        }
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

        // TODO: Pre-partition triangles into spatial bins or BVH before pairwise checks.

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
        // TODO: Extend to return multiple segments per triangle pair (rare but possible)
        //       when triangles intersect along two disjoint chords after snapping.

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

        // Decouple A/B collection: record available endpoints independently to avoid
        // dropping segments when one side barely fails an inside test.
        a0 = a1 = b0 = b1 = default;
        bool any = false;
        if (okA)
        {
            (a0, a1) = PickPair(ptsA);
            any = true;
        }
        if (okB)
        {
            (b0, b1) = PickPair(ptsB);
            any = true;
        }
        return any;
    }

    // --- Segment stitching helpers ---
    private static List<List<Point>> BuildClosedLoopsFromSegments(List<(Point P, Point Q)> segments)
    {
        var adj = new Dictionary<Point, List<Point>>();
        var edgeMulti = new Dictionary<EdgeKey, int>();
        // TODO: Ensure consistent loop orientation (CW/CCW per surface normal) if needed
        //       for subsequent boolean operations that depend on winding.

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
                // TODO: If coplanar, we should consider overlap classification and possibly
                //       emit boundary-aligned segments to avoid missing loops.
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
            // TODO: Edge-on/vertex-on cases may duplicate points after snapping; ensure robust dedup.
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
