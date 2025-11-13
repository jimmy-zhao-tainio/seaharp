using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Seaharp.Geometry;
using Seaharp.Topology;
using Seaharp.Geometry.Computation;

internal static class Program
{
    private static void Main(string[] args)
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var a = new Seaharp.World.Sphere(r, subdivisions: 3, center: aCenter);
        var b = new Seaharp.World.Sphere(r, subdivisions: 3, center: bCenter);

        var surfaceA = ClosedSurface.FromTetrahedra(a.Tetrahedra);
        var surfaceB = ClosedSurface.FromTetrahedra(b.Tetrahedra);

        // Compute intersection cuts; record all touched triangles and delete them.
        // Additionally, delete any triangle whose vertex lies inside (or on) the other surface.
        var cuts = IntersectionSegments.BuildCuts(surfaceA, surfaceB);

        var keepA = new List<Triangle>(surfaceA.Triangles.Count);
        var keepB = new List<Triangle>(surfaceB.Triangles.Count);

        for (int i = 0; i < surfaceA.Triangles.Count; i++)
        {
            var t = surfaceA.Triangles[i];
            bool intersects = cuts.CutsA[i].Count > 0;
            bool vertexInside =
                InsideClosedSurface.ContainsInclusive(surfaceB.Triangles, t.P0) ||
                InsideClosedSurface.ContainsInclusive(surfaceB.Triangles, t.P1) ||
                InsideClosedSurface.ContainsInclusive(surfaceB.Triangles, t.P2);
            if (!(intersects || vertexInside)) keepA.Add(t);
        }

        for (int j = 0; j < surfaceB.Triangles.Count; j++)
        {
            var t = surfaceB.Triangles[j];
            bool intersects = cuts.CutsB[j].Count > 0;
            bool vertexInside =
                InsideClosedSurface.ContainsInclusive(surfaceA.Triangles, t.P0) ||
                InsideClosedSurface.ContainsInclusive(surfaceA.Triangles, t.P1) ||
                InsideClosedSurface.ContainsInclusive(surfaceA.Triangles, t.P2);
            if (!(intersects || vertexInside)) keepB.Add(t);
        }

        // Build loops for this pair and segment list for post-cull checks and visualization
        var loops = IntersectionSegments.BuildLoops(surfaceA, surfaceB);
        var allSegs = new List<(Point P, Point Q)>();
        for (int i = 0; i < cuts.CutsA.Length; i++) allSegs.AddRange(cuts.CutsA[i]);
        for (int j = 0; j < cuts.CutsB.Length; j++) allSegs.AddRange(cuts.CutsB[j]);
        foreach (var loop in loops)
        {
            int n = loop.Count;
            if (n < 2) continue;
            for (int i = 0; i < n - 1; i++) allSegs.Add((loop[i], loop[i + 1]));
        }

        // Deduplicate segments (undirected)
        var segSet0 = new HashSet<EdgeKey>();
        var segs = new List<(Point P, Point Q)>();
        foreach (var s in allSegs)
        {
            var key = new EdgeKey(s.P, s.Q);
            if (segSet0.Add(key)) segs.Add(s);
        }

        // Additional cull: remove any triangle that has a vertex that coincides with a segment endpoint
        // or lies strictly on a segment (colinear and within bounds). This ensures every remaining
        // kept vertex forms a non-degenerate triangle with the segment endpoints.
        var endpointSet = new HashSet<Point>();
        foreach (var s in segs) { endpointSet.Add(s.P); endpointSet.Add(s.Q); }
        // Include all loop vertices explicitly as well
        foreach (var loop in loops)
            for (int i = 0; i < loop.Count; i++) endpointSet.Add(loop[i]);

        bool TouchesAnySegmentVertexOrInterior(in Triangle t)
        {
            bool OnAny(Point v)
            {
                if (endpointSet.Contains(v)) return true;
                for (int k = 0; k < segs.Count; k++)
                {
                    var sg = segs[k];
                    if (PointOnSegmentInt(sg.P, sg.Q, v)) return true;
                }
                return false;
            }
            return OnAny(t.P0) || OnAny(t.P1) || OnAny(t.P2);
        }

        keepA.RemoveAll(t => TouchesAnySegmentVertexOrInterior(t));
        keepB.RemoveAll(t => TouchesAnySegmentVertexOrInterior(t));

        Console.WriteLine($"Removed A:{surfaceA.Triangles.Count - keepA.Count} B:{surfaceB.Triangles.Count - keepB.Count}");

        var combined = new ClosedSurface(keepA.Concat(keepB));
        var outPath = args != null && args.Length > 0 ? args[0] : "spheres_destroy_cuts.stl";
        StlWriter.Write(combined, outPath);
        Console.WriteLine($"Wrote: {System.IO.Path.GetFullPath(outPath)} with {combined.Triangles.Count} triangles");

        // Also write the triangles that were crossed (combined A+B) for visual inspection
        var touched = IntersectionSegments.ExtractTouchedTriangles(surfaceA, surfaceB);
        var crossed = new List<Triangle>(touched.A.Count + touched.B.Count);
        crossed.AddRange(touched.A);
        crossed.AddRange(touched.B);
        if (crossed.Count > 0)
        {
            var pc = "triangles_crossed.stl";
            StlWriter.Write(crossed, pc);
            Console.WriteLine($"Wrote crossed triangles: {System.IO.Path.GetFullPath(pc)} count={crossed.Count} (A={touched.A.Count}, B={touched.B.Count})");
        }

        // Combine cracked-egg shells (kept triangles) with the crossed triangles
        // to get an exterior view that appears unioned without splitting.
        var combinedExterior = new List<Triangle>(keepA.Count + keepB.Count + crossed.Count);
        combinedExterior.AddRange(keepA);
        combinedExterior.AddRange(keepB);
        combinedExterior.AddRange(crossed);
        var unionLikePath = "spheres_outside_union.stl";
        StlWriter.Write(combinedExterior, unionLikePath);
        Console.WriteLine($"Wrote outside-union appearance: {System.IO.Path.GetFullPath(unionLikePath)} with {combinedExterior.Count} triangles");

        // Build a disc from intersection segments to loop center and combine with cracked shells
        var discTris = IntersectionSegments.BuildLoopDiscs(surfaceA, surfaceB);
        if (discTris.Count > 0)
        {
            var crackedPlusDisc = new List<Triangle>(keepA.Count + keepB.Count + discTris.Count);
            crackedPlusDisc.AddRange(keepA);
            crackedPlusDisc.AddRange(keepB);
            crackedPlusDisc.AddRange(discTris);
            var discPath = "spheres_with_disc.stl";
            StlWriter.Write(crackedPlusDisc, discPath);
            Console.WriteLine($"Wrote cracked shells + disc: {System.IO.Path.GetFullPath(discPath)} with {crackedPlusDisc.Count} triangles (disc={discTris.Count})");
        }

        // Additional visualization: small boxes centered on crack-edge vertices (boundary vertices of kept shells)
        var crackVertices = new HashSet<Point>();
        CollectBoundaryVertices(keepA, crackVertices);
        CollectBoundaryVertices(keepB, crackVertices);
        if (crackVertices.Count > 0)
        {
            var boxesTris = new List<Triangle>(crackVertices.Count * 12);
            const long boxSize = 4; // small, even to keep integer centering (half = 2)
            long half = boxSize / 2;
            foreach (var v in crackVertices)
            {
                var bx = new Seaharp.World.Box(boxSize, boxSize, boxSize);
                bx.Position(v.X - half, v.Y - half, v.Z - half);
                boxesTris.AddRange(bx.Mesh.Triangles);
            }

            // Combine shells + boxes only (no fan)
            var shellsPlusBoxes = new List<Triangle>(keepA.Count + keepB.Count + boxesTris.Count);
            shellsPlusBoxes.AddRange(keepA);
            shellsPlusBoxes.AddRange(keepB);
            shellsPlusBoxes.AddRange(boxesTris);
            var boxesPath = "spheres_with_boxes.stl";
            StlWriter.Write(shellsPlusBoxes, boxesPath);
            Console.WriteLine($"Wrote cracked shells + boxes: {System.IO.Path.GetFullPath(boxesPath)} with {shellsPlusBoxes.Count} triangles (boxes={boxesTris.Count})");
        }

        // Compute minimum signed distance from any kept vertex to any intersection segment
        // Sign: negative if the vertex lies inside/on the other surface (would have been destroyed)
        // segs already computed above

        var keptVerts = new List<(Point V, bool isA)>();
        CollectVerts(keepA, keptVerts, true);
        CollectVerts(keepB, keptVerts, false);

        double minSigned = double.PositiveInfinity;
        double minArea = double.PositiveInfinity; // 0.5 * d * |PQ|
        double minDistance = double.PositiveInfinity;
        double minSegLen = double.PositiveInfinity;
        foreach (var kv in keptVerts)
        {
            int segIdx;
            double d = MinDistanceToSegments(kv.V, segs, out segIdx);
            if (segIdx >= 0)
            {
                var s = segs[segIdx];
                double L = Math.Sqrt(Dist2D(s.P, s.Q));
                if (L < minSegLen) minSegLen = L;
                double area = 0.5 * d * L;
                if (area < minArea) minArea = area;
            }
            if (d < minDistance) minDistance = d;
            bool insideOther = kv.isA
                ? InsideClosedSurface.ContainsInclusive(surfaceB.Triangles, kv.V)
                : InsideClosedSurface.ContainsInclusive(surfaceA.Triangles, kv.V);
            double signed = insideOther ? -d : d;
            if (signed < minSigned) minSigned = signed;
        }
        if (double.IsPositiveInfinity(minSigned)) minSigned = double.NaN;
        if (double.IsPositiveInfinity(minArea)) minArea = double.NaN;
        if (double.IsPositiveInfinity(minDistance)) minDistance = double.NaN;
        if (double.IsPositiveInfinity(minSegLen)) minSegLen = double.NaN;
        Console.WriteLine($"Min signed distance from kept vertex to any segment: {minSigned:F6}");
        Console.WriteLine($"Min unsigned distance: {minDistance:F6}, min segment length: {minSegLen:F6}, min area(v,seg): {minArea:F6}");

        // Build zipper-like bridges from seam to anchors on A and B; combine with shells
        var bridges = IntersectionSegments.BuildBridgeTriangles(surfaceA, surfaceB);
        if (bridges.A.Count + bridges.B.Count > 0)
        {
            var withBridges = new List<Triangle>(keepA.Count + keepB.Count + bridges.A.Count + bridges.B.Count);
            withBridges.AddRange(keepA);
            withBridges.AddRange(keepB);
            withBridges.AddRange(bridges.A);
            withBridges.AddRange(bridges.B);
            var bridgesPath = "spheres_with_bridges.stl";
            StlWriter.Write(withBridges, bridgesPath);
            Console.WriteLine($"Wrote cracked shells + bridges: {System.IO.Path.GetFullPath(bridgesPath)} with {withBridges.Count} triangles (bridges={bridges.A.Count + bridges.B.Count})");
        }

        // Local, per-destroyed-triangle bridges using two anchors per segment
        var localBridges = IntersectionSegments.BuildLocalBridgesFromDestroyed(surfaceA, surfaceB);
        if (localBridges.A.Count + localBridges.B.Count > 0)
        {
            // Filter out any local-bridge triangle that lies inside the opposite surface
            var filteredA = new List<Triangle>(localBridges.A.Count);
            foreach (var t in localBridges.A)
            {
                var c = CentroidPoint(t);
                if (!InsideClosedSurface.ContainsInclusive(surfaceB.Triangles, c))
                    filteredA.Add(t);
            }
            var filteredB = new List<Triangle>(localBridges.B.Count);
            foreach (var t in localBridges.B)
            {
                var c = CentroidPoint(t);
                if (!InsideClosedSurface.ContainsInclusive(surfaceA.Triangles, c))
                    filteredB.Add(t);
            }

            var withLocal = new List<Triangle>(keepA.Count + keepB.Count + filteredA.Count + filteredB.Count);
            withLocal.AddRange(keepA);
            withLocal.AddRange(keepB);
            withLocal.AddRange(filteredA);
            withLocal.AddRange(filteredB);
            var localPath = "spheres_with_local_bridges.stl";
            StlWriter.Write(withLocal, localPath);
            Console.WriteLine($"Wrote cracked shells + local bridges: {System.IO.Path.GetFullPath(localPath)} with {withLocal.Count} triangles (bridges_kept={filteredA.Count + filteredB.Count}, filtered_out={localBridges.A.Count + localBridges.B.Count - filteredA.Count - filteredB.Count})");
        }

        // Experimental zipper between seam loops and cracked-shell boundaries (greedy)
        if (loops.Count > 0)
        {
            var boundaryA = BuildBoundaryLoops(keepA);
            var boundaryB = BuildBoundaryLoops(keepB);

            var zipTris = new List<Triangle>();
            foreach (var seam in loops)
            {
                var ba = ChooseClosestBoundary(seam, boundaryA);
                var bb = ChooseClosestBoundary(seam, boundaryB);
                if (ba != null)
                {
                    ZipClosed(seam, ba, zipTris, preferA: true);
                }
                if (bb != null)
                {
                    ZipClosed(seam, bb, zipTris, preferA: false);
                }
            }

            if (zipTris.Count > 0)
            {
                var withZipper = new List<Triangle>(keepA.Count + keepB.Count + zipTris.Count);
                withZipper.AddRange(keepA);
                withZipper.AddRange(keepB);
                withZipper.AddRange(zipTris);
                var zipperPath = "spheres_with_zipper.stl";
                StlWriter.Write(withZipper, zipperPath);
                Console.WriteLine($"Wrote cracked shells + zipper: {System.IO.Path.GetFullPath(zipperPath)} with {withZipper.Count} triangles (zipper={zipTris.Count})");
            }
        }

        // DTW-based monotone zipper (recommended)
        if (loops.Count > 0)
        {
            var boundaryA = BuildBoundaryLoops(keepA);
            var boundaryB = BuildBoundaryLoops(keepB);
            var zipDTW = new List<Triangle>();
            foreach (var seam in loops)
            {
                var ba = ChooseClosestBoundary(seam, boundaryA);
                var bb = ChooseClosestBoundary(seam, boundaryB);
                if (ba != null) ZipClosedDTW(seam, ba, zipDTW);
                if (bb != null) ZipClosedDTW(seam, bb, zipDTW);
            }
            if (zipDTW.Count > 0)
            {
                var withZipperDTW = new List<Triangle>(keepA.Count + keepB.Count + zipDTW.Count);
                withZipperDTW.AddRange(keepA);
                withZipperDTW.AddRange(keepB);
                withZipperDTW.AddRange(zipDTW);
                var zipperDTWPath = "spheres_with_zipper_dtw.stl";
                StlWriter.Write(withZipperDTW, zipperDTWPath);
                Console.WriteLine($"Wrote cracked shells + zipper (DTW): {System.IO.Path.GetFullPath(zipperDTWPath)} with {withZipperDTW.Count} triangles (zipper={zipDTW.Count})");
            }
        }

        // Print loop stats for reference (debug only)
        Console.WriteLine($"Intersection loops: {loops.Count}");
        if (loops.Count > 0)
        {
            var lengths = loops.Select(l => l.Count - 1);
            Console.WriteLine("Loop vertex counts (closed): " + string.Join(", ", lengths));
        }
    }

    private static void CollectVerts(List<Triangle> tris, List<(Point V, bool isA)> outVerts, bool isA)
    {
        var seen = new HashSet<Point>();
        foreach (var t in tris)
        {
            if (seen.Add(t.P0)) outVerts.Add((t.P0, isA));
            if (seen.Add(t.P1)) outVerts.Add((t.P1, isA));
            if (seen.Add(t.P2)) outVerts.Add((t.P2, isA));
        }
    }

    private static void CollectBoundaryVertices(List<Triangle> tris, HashSet<Point> outVerts)
    {
        var edgeCount = new Dictionary<EdgeKey, int>();
        foreach (var t in tris)
        {
            AddEdge(new EdgeKey(t.P0, t.P1));
            AddEdge(new EdgeKey(t.P1, t.P2));
            AddEdge(new EdgeKey(t.P2, t.P0));
        }
        foreach (var kv in edgeCount)
        {
            if (kv.Value == 1)
            {
                outVerts.Add(kv.Key.A);
                outVerts.Add(kv.Key.B);
            }
        }

        void AddEdge(EdgeKey e)
        {
            if (edgeCount.TryGetValue(e, out int c)) edgeCount[e] = c + 1; else edgeCount[e] = 1;
        }
    }

    private static Point CentroidPoint(in Triangle t)
    {
        double cx = (t.P0.X + t.P1.X + t.P2.X) / 3.0;
        double cy = (t.P0.Y + t.P1.Y + t.P2.Y) / 3.0;
        double cz = (t.P0.Z + t.P1.Z + t.P2.Z) / 3.0;
        return new Point(
            (long)Math.Round(cx, MidpointRounding.AwayFromZero),
            (long)Math.Round(cy, MidpointRounding.AwayFromZero),
            (long)Math.Round(cz, MidpointRounding.AwayFromZero));
    }

    private static double MinDistanceToSegments(Point v, List<(Point P, Point Q)> segs)
    {
        double best = double.PositiveInfinity;
        for (int i = 0; i < segs.Count; i++)
        {
            var s = segs[i];
            double d = DistPointSegment(v, s.P, s.Q);
            if (d < best) best = d;
        }
        return best;
    }

    private static double MinDistanceToSegments(Point v, List<(Point P, Point Q)> segs, out int index)
    {
        double best = double.PositiveInfinity; index = -1;
        for (int i = 0; i < segs.Count; i++)
        {
            var s = segs[i];
            double d = DistPointSegment(v, s.P, s.Q);
            if (d < best)
            {
                best = d; index = i;
            }
        }
        return best;
    }

    private static double DistPointSegment(Point v, Point a, Point b)
    {
        double ax = a.X, ay = a.Y, az = a.Z;
        double bx = b.X, by = b.Y, bz = b.Z;
        double vx = v.X, vy = v.Y, vz = v.Z;
        double abx = bx - ax, aby = by - ay, abz = bz - az;
        double avx = vx - ax, avy = vy - ay, avz = vz - az;
        double ab2 = abx * abx + aby * aby + abz * abz;
        if (ab2 == 0)
        {
            double dx = vx - ax, dy = vy - ay, dz = vz - az;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        double t = (avx * abx + avy * aby + avz * abz) / ab2;
        if (t < 0) t = 0; else if (t > 1) t = 1;
        double cx = ax + abx * t, cy = ay + aby * t, cz = az + abz * t;
        double dx2 = vx - cx, dy2 = vy - cy, dz2 = vz - cz;
        return Math.Sqrt(dx2 * dx2 + dy2 * dy2 + dz2 * dz2);
    }

    private static bool PointOnSegmentInt(in Point a, in Point b, in Point p)
    {
        // Bounding box check first
        long minX = Math.Min(a.X, b.X), maxX = Math.Max(a.X, b.X);
        long minY = Math.Min(a.Y, b.Y), maxY = Math.Max(a.Y, b.Y);
        long minZ = Math.Min(a.Z, b.Z), maxZ = Math.Max(a.Z, b.Z);
        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY || p.Z < minZ || p.Z > maxZ)
            return false;

        // Colinearity via exact Int128 cross product of (b-a) x (p-a)
        Int128 v1x = (Int128)b.X - a.X;
        Int128 v1y = (Int128)b.Y - a.Y;
        Int128 v1z = (Int128)b.Z - a.Z;
        Int128 v2x = (Int128)p.X - a.X;
        Int128 v2y = (Int128)p.Y - a.Y;
        Int128 v2z = (Int128)p.Z - a.Z;
        Int128 cx = v1y * v2z - v1z * v2y;
        Int128 cy = v1z * v2x - v1x * v2z;
        Int128 cz = v1x * v2y - v1y * v2x;
        return cx == 0 && cy == 0 && cz == 0;
    }

    private static double Dist2D(Point a, Point b)
    {
        double dx = (double)a.X - b.X;
        double dy = (double)a.Y - b.Y;
        double dz = (double)a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    // --- Zipper helpers ---
    private static List<List<Point>> BuildBoundaryLoops(List<Triangle> tris)
    {
        var edgeCount = new Dictionary<EdgeKey, int>();
        void AddEdge(EdgeKey e) { if (edgeCount.TryGetValue(e, out int c)) edgeCount[e] = c + 1; else edgeCount[e] = 1; }
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            AddEdge(new EdgeKey(t.P0, t.P1));
            AddEdge(new EdgeKey(t.P1, t.P2));
            AddEdge(new EdgeKey(t.P2, t.P0));
        }
        var adj = new Dictionary<Point, List<Point>>();
        foreach (var kv in edgeCount)
        {
            if (kv.Value != 1) continue; // only boundary edges
            var a = kv.Key.A; var b = kv.Key.B;
            if (!adj.TryGetValue(a, out var la)) { la = new List<Point>(2); adj[a] = la; }
            if (!adj.TryGetValue(b, out var lb)) { lb = new List<Point>(2); adj[b] = lb; }
            la.Add(b); lb.Add(a);
        }

        var used = new HashSet<EdgeKey>();
        var loops = new List<List<Point>>();
        foreach (var kv in adj)
        {
            var start = kv.Key;
            foreach (var nb in kv.Value)
            {
                var e = new EdgeKey(start, nb);
                if (used.Contains(e)) continue;
                var loop = WalkLoop(start, nb);
                if (loop != null && loop.Count > 2) loops.Add(loop);
            }
        }
        return loops;

        List<Point>? WalkLoop(Point s, Point next)
        {
            var loop = new List<Point>(32) { s };
            Point prev = s; Point curr = next;
            Mark(prev, curr);
            while (true)
            {
                loop.Add(curr);
                if (curr.Equals(s))
                {
                    if (!loop[^1].Equals(loop[0])) loop.Add(loop[0]);
                    return loop;
                }
                if (!adj.TryGetValue(curr, out var nbrs) || nbrs.Count == 0) return null;
                Point? chosen = null;
                for (int i = 0; i < nbrs.Count; i++)
                {
                    var nb = nbrs[i];
                    if (nb.Equals(prev)) continue;
                    var ek = new EdgeKey(curr, nb);
                    if (!used.Contains(ek)) { chosen = nb; break; }
                }
                if (chosen is null)
                {
                    for (int i = 0; i < nbrs.Count; i++)
                    {
                        var nb = nbrs[i];
                        var ek = new EdgeKey(curr, nb);
                        if (!used.Contains(ek)) { chosen = nb; break; }
                    }
                    if (chosen is null) return null;
                }
                prev = curr; curr = chosen.Value; Mark(prev, curr);
            }
        }
        void Mark(Point a, Point b) { used.Add(new EdgeKey(a, b)); }
    }

    private static List<Point>? ChooseClosestBoundary(List<Point> seam, List<List<Point>> boundaries)
    {
        if (boundaries.Count == 0) return null;
        double best = double.PositiveInfinity; int bestIdx = -1;
        for (int i = 0; i < boundaries.Count; i++)
        {
            double d = MeanDistance(seam, boundaries[i]);
            if (d < best) { best = d; bestIdx = i; }
        }
        return bestIdx >= 0 ? boundaries[bestIdx] : null;
    }
    private static double MeanDistance(List<Point> a, List<Point> b)
    {
        int na = a.Count - 1; if (na <= 0) return double.PositiveInfinity;
        int nb = b.Count - 1; if (nb <= 0) return double.PositiveInfinity;
        double sum = 0;
        for (int i = 0; i < na; i++)
        {
            var p = a[i];
            double best = double.PositiveInfinity;
            for (int j = 0; j < nb; j++)
            {
                double d2 = Dist2D(p, b[j]);
                if (d2 < best) best = d2;
            }
            sum += Math.Sqrt(best);
        }
        return sum / na;
    }

    private static void ZipClosedDTW(List<Point> seamIn, List<Point> boundaryIn, List<Triangle> outTris)
    {
        var seam = NormalizeClosed(seamIn);
        var boundary = NormalizeClosed(boundaryIn);
        if (seam.Count < 3 || boundary.Count < 3) return;

        int n = seam.Count - 1;
        int m = boundary.Count - 1;

        // Align boundary start and direction to seam[0]->seam[1]
        int j0 = 0; double best0 = double.PositiveInfinity;
        for (int j = 0; j < m; j++)
        {
            double d2 = Dist2D(seam[0], boundary[j]);
            if (d2 < best0) { best0 = d2; j0 = j; }
        }
        int jPlus = (j0 + 1) % m, jMinus = (j0 - 1 + m) % m;
        double dPlus = Dist2D(seam[1 % n], boundary[jPlus]);
        double dMinus = Dist2D(seam[1 % n], boundary[jMinus]);
        bool forward = dPlus <= dMinus;
        // Build rotated boundary array b[0..m-1]
        var b = new Point[m];
        if (forward)
        {
            for (int k = 0; k < m; k++) b[k] = boundary[(j0 + k) % m];
        }
        else
        {
            for (int k = 0; k < m; k++) b[k] = boundary[(j0 - k + m) % m];
        }

        // DTW with band constraint
        double inf = double.PositiveInfinity;
        var dtw = new double[n + 1, m + 1];
        var step = new byte[n + 1, m + 1]; // 1=(i-1,j),2=(i,j-1),3=(i-1,j-1)
        for (int i = 0; i <= n; i++) for (int j = 0; j <= m; j++) { dtw[i, j] = inf; step[i, j] = 0; }
        dtw[0, 0] = 0;

        int W = Math.Max(1, (int)Math.Floor(0.25 * Math.Max(n, m)));
        double insPenalty = 0.1; // discourage long runs of inserts/deletes

        for (int i = 1; i <= n; i++)
        {
            int jc = (int)Math.Round((double)i * m / n);
            int jMin = Math.Max(1, jc - W);
            int jMax = Math.Min(m, jc + W);
            for (int j = jMin; j <= jMax; j++)
            {
                double d = Dist2D(seam[i - 1], b[j - 1]);
                double best = dtw[i - 1, j - 1]; byte bs = 3; // match
                double v = dtw[i - 1, j] + insPenalty; if (v < best) { best = v; bs = 1; }
                double h = dtw[i, j - 1] + insPenalty; if (h < best) { best = h; bs = 2; }
                dtw[i, j] = d + best; step[i, j] = bs;
            }
        }

        // Backtrack
        int ii = n, jj = m;
        var rev = new List<(int i, int j, byte s)>(n + m);
        while (ii > 0 || jj > 0)
        {
            byte s = step[ii, jj];
            if (s == 0)
            {
                // fall back to diagonal if band clipped
                if (ii > 0 && jj > 0) { s = 3; }
                else if (ii > 0) { s = 1; }
                else if (jj > 0) { s = 2; }
            }
            rev.Add((ii, jj, s));
            if (s == 3) { ii--; jj--; }
            else if (s == 1) { ii--; }
            else { jj--; }
        }
        rev.Reverse();

        // Emit triangles from path
        int iPrev = 0, jPrev = 0;
        foreach (var (i, j, s) in rev)
        {
            if (s == 1)
            {
                // (i-1,j) -> (i,j): seam advanced
                TryAdd(outTris, seam[i - 1], seam[i % n], b[jPrev % m]);
            }
            else if (s == 2)
            {
                // (i,j-1) -> (i,j): boundary advanced
                TryAdd(outTris, seam[iPrev % n], b[jPrev % m], b[j % m]);
            }
            else // 3
            {
                // diagonal: two triangles forming a quad
                TryAdd(outTris, seam[i - 1], seam[i % n], b[j - 1]);
                TryAdd(outTris, seam[i % n], b[j - 1], b[j % m]);
            }
            iPrev = i; jPrev = j;
        }

        static List<Point> NormalizeClosed(List<Point> loop)
        {
            var list = new List<Point>(loop);
            if (list.Count > 1 && !list[^1].Equals(list[0])) list.Add(list[0]);
            var outL = new List<Point>(list.Count);
            for (int t = 0; t < list.Count; t++)
            {
                if (t == 0 || !list[t].Equals(list[t - 1])) outL.Add(list[t]);
            }
            return outL;
        }
        static void TryAdd(List<Triangle> dst, Point a, Point b, Point c)
        {
            if (a.Equals(b) || b.Equals(c) || c.Equals(a)) return;
            double ux = (double)b.X - a.X, uy = (double)b.Y - a.Y, uz = (double)b.Z - a.Z;
            double vx = (double)c.X - a.X, vy = (double)c.Y - a.Y, vz = (double)c.Z - a.Z;
            double cx = uy * vz - uz * vy;
            double cy = uz * vx - ux * vz;
            double cz = ux * vy - uy * vx;
            double l2 = cx * cx + cy * cy + cz * cz;
            if (l2 <= 0) return;
            dst.Add(Triangle.FromWinding(a, b, c));
        }
    }
    private static void ZipClosed(List<Point> seamIn, List<Point> boundaryIn, List<Triangle> outTris, bool preferA)
    {
        var seam = NormalizeClosed(seamIn);
        var boundary = NormalizeClosed(boundaryIn);
        if (seam.Count < 3 || boundary.Count < 3) return;

        int n = seam.Count - 1;
        int m = boundary.Count - 1;

        // Find closest boundary start to seam[0]
        int j = 0; double best0 = double.PositiveInfinity;
        for (int k = 0; k < m; k++)
        {
            double d2 = Dist2D(seam[0], boundary[k]);
            if (d2 < best0) { best0 = d2; j = k; }
        }
        // Choose walking direction by looking at seam[1]
        int jPlus = (j + 1) % m, jMinus = (j - 1 + m) % m;
        double dPlus = Dist2D(seam[1 % n], boundary[jPlus]);
        double dMinus = Dist2D(seam[1 % n], boundary[jMinus]);
        int dir = dPlus <= dMinus ? +1 : -1;

        for (int i = 0; i < n; i++)
        {
            int iNext = (i + 1) % n;

            // Decide whether to advance boundary index for next seam vertex
            int jCandidate = (j + dir + m) % m;
            double stay = Dist2D(seam[iNext], boundary[j]);
            double step = Dist2D(seam[iNext], boundary[jCandidate]);
            int jNext = step < stay ? jCandidate : j;

            // Triangle using current seam edge and current boundary vertex
            TryAdd(outTris, seam[i], seam[iNext], boundary[j]);

            // If boundary index advanced, close the quad with a second triangle
            if (jNext != j)
            {
                TryAdd(outTris, seam[iNext], boundary[j], boundary[jNext]);
            }

            j = jNext;
        }

        static List<Point> NormalizeClosed(List<Point> loop)
        {
            var list = new List<Point>(loop);
            if (list.Count > 1 && !list[^1].Equals(list[0])) list.Add(list[0]);
            var outL = new List<Point>(list.Count);
            for (int t = 0; t < list.Count; t++)
            {
                if (t == 0 || !list[t].Equals(list[t - 1])) outL.Add(list[t]);
            }
            return outL;
        }
        static void TryAdd(List<Triangle> dst, Point a, Point b, Point c)
        {
            if (a.Equals(b) || b.Equals(c) || c.Equals(a)) return;
            double ux = (double)b.X - a.X, uy = (double)b.Y - a.Y, uz = (double)b.Z - a.Z;
            double vx = (double)c.X - a.X, vy = (double)c.Y - a.Y, vz = (double)c.Z - a.Z;
            double cx = uy * vz - uz * vy;
            double cy = uz * vx - ux * vz;
            double cz = ux * vy - uy * vx;
            double l2 = cx * cx + cy * cy + cz * cz;
            if (l2 <= 0) return;
            dst.Add(Triangle.FromWinding(a, b, c));
        }
    }
}
