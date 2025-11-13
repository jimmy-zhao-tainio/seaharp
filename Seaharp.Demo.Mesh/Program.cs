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
}
