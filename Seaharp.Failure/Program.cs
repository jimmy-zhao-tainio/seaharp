using System;
using System.Collections.Generic;
using System.IO;
using Seaharp.World;
using Seaharp.World.Predicates;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Quick checks for shapes
        var box = new Box(4, 3, 2);
        var sphere = new Sphere(radius: 5, subdivisions: 1);
        var cyl = new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16);
        var surface = new Surface(cyl);

        Console.WriteLine("=== Cylinder Surface Diagnostics ===");
        Console.WriteLine($"Triangles: {surface.Triangles.Count}");

        // Edge counts
        var edgeCounts = new Dictionary<EdgeKey, int>(surface.Triangles.Count * 3);
        foreach (var t in surface.Triangles)
        {
            Count(ref edgeCounts, new EdgeKey(t.P0, t.P1));
            Count(ref edgeCounts, new EdgeKey(t.P1, t.P2));
            Count(ref edgeCounts, new EdgeKey(t.P2, t.P0));
        }
        Console.WriteLine($"Unique edges: {edgeCounts.Count}");

        // Distribution of edge uses
        var dist = new Dictionary<int, int>();
        foreach (var kv in edgeCounts)
        {
            if (!dist.TryGetValue(kv.Value, out var c)) c = 0;
            dist[kv.Value] = c + 1;
        }
        Console.WriteLine("Edge usage distribution (count -> edges):");
        foreach (var kv in Sorted(dist))
        {
            Console.WriteLine($"  {kv.Key} -> {kv.Value}");
        }

        // First N bad edges (count != 2)
        int badShown = 0;
        Console.WriteLine("Bad edges (count != 2), first 50:");
        foreach (var kv in edgeCounts)
        {
            if (kv.Value == 2) continue;
            Console.WriteLine($"  {kv.Key} -> {kv.Value}");
            if (++badShown >= 50) break;
        }

        // Manifold check
        Console.WriteLine($"Box.IsManifold: {SurfacePredicates.IsManifold(new Surface(box))}");
        Console.WriteLine($"Sphere.IsManifold: {SurfacePredicates.IsManifold(new Surface(sphere))}");
        Console.WriteLine($"Cylinder.IsManifold: {SurfacePredicates.IsManifold(surface)}");

        // Export STL of surface for visual inspection
        var w = new Seaharp.World.World();
        w.Add(cyl);
        var outPath = Path.GetFullPath(args.Length > 0 ? args[0] : "failed_cylinder.stl");
        w.Save(outPath);
        Console.WriteLine($"Wrote STL to: {outPath}");
    }

    private static void Count(ref Dictionary<EdgeKey, int> map, EdgeKey k)
    {
        if (map.TryGetValue(k, out var c)) map[k] = c + 1; else map[k] = 1;
    }

    private static IEnumerable<KeyValuePair<int, int>> Sorted(Dictionary<int, int> d)
    {
        var keys = new List<int>(d.Keys);
        keys.Sort();
        foreach (var k in keys) yield return new KeyValuePair<int, int>(k, d[k]);
    }

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        private readonly Seaharp.Geometry.Point A;
        private readonly Seaharp.Geometry.Point B;

        public EdgeKey(in Seaharp.Geometry.Point p, in Seaharp.Geometry.Point q)
        {
            if (LessEq(p, q)) { A = p; B = q; }
            else { A = q; B = p; }
        }

        public bool Equals(EdgeKey other) => A.Equals(other.A) && B.Equals(other.B);
        public override bool Equals(object? obj) => obj is EdgeKey e && Equals(e);
        public override int GetHashCode() => HashCode.Combine(A.X, A.Y, A.Z, B.X, B.Y, B.Z);

        public override string ToString() => $"[{Fmt(A)}]-[{Fmt(B)}]";

        private static string Fmt(in Seaharp.Geometry.Point p) => $"{p.X},{p.Y},{p.Z}";

        private static bool LessEq(in Seaharp.Geometry.Point p, in Seaharp.Geometry.Point q)
        {
            if (p.X != q.X) return p.X < q.X;
            if (p.Y != q.Y) return p.Y < q.Y;
            return p.Z <= q.Z;
        }
    }
}
