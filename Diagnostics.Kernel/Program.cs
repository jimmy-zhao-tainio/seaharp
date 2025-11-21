using System;
using System.Collections.Generic;
using Geometry;
using Kernel;
using World;

namespace Diagnostics.Kernel;

internal static class Program
{
    private static void Main()
    {
        RunSphereIntersectionDiagnostics();
    }

    private static void RunSphereIntersectionDiagnostics()
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var sphereA = new Sphere(r, subdivisions: 3, center: aCenter);
        var sphereB = new Sphere(r, subdivisions: 3, center: bCenter);

        var set = new IntersectionSet(
            sphereA.Mesh.Triangles,
            sphereB.Mesh.Triangles);

        var graph = IntersectionGraph.FromIntersectionSet(set);
        var index = TriangleIntersectionIndex.Build(graph);
        var meshATopology = MeshATopology.Build(graph, index);

        Console.WriteLine("=== Sphere-Sphere Intersection Diagnostics (Mesh A) ===");
        Console.WriteLine($"Triangles A: {set.TrianglesA.Count}");
        Console.WriteLine($"Triangles B: {set.TrianglesB.Count}");
        Console.WriteLine($"Intersecting pairs: {set.Intersections.Count}");
        Console.WriteLine($"Global vertices: {graph.Vertices.Count}");
        Console.WriteLine($"Global edges: {graph.Edges.Count}");
        Console.WriteLine($"Mesh-A edges: {meshATopology.Edges.Count}");
        Console.WriteLine($"Mesh-A components (Loops entries): {meshATopology.Loops.Count}");
        Console.WriteLine();

        // Degree histogram for mesh A vertices.
        var degreeHistogram = new Dictionary<int, int>();
        foreach (var kvp in meshATopology.VertexEdges)
        {
            int degree = kvp.Value.Count;
            degreeHistogram.TryGetValue(degree, out var count);
            degreeHistogram[degree] = count + 1;
        }

        Console.WriteLine("Vertex degree histogram on mesh A (using mesh-A edges only):");
        foreach (var kvp in degreeHistogram)
        {
            Console.WriteLine($"  degree {kvp.Key}: {kvp.Value} vertices");
        }
        Console.WriteLine();

        // Map global vertex ids to positions for length estimates.
        var globalPositions = new Dictionary<int, RealPoint>();
        foreach (var (id, position) in graph.Vertices)
        {
            globalPositions[id.Value] = position;
        }

        Console.WriteLine("Components on mesh A (from MeshATopology.Loops):");
        const int maxPrintedComponents = 10;
        for (int i = 0; i < meshATopology.Loops.Count; i++)
        {
            var chain = meshATopology.Loops[i];
            double length = 0.0;
            for (int j = 0; j < chain.Length - 1; j++)
            {
                var v0 = chain[j];
                var v1 = chain[j + 1];
                if (!globalPositions.TryGetValue(v0.Value, out var p0) ||
                    !globalPositions.TryGetValue(v1.Value, out var p1))
                {
                    continue;
                }

                double dx = p1.X - p0.X;
                double dy = p1.Y - p0.Y;
                double dz = p1.Z - p0.Z;
                length += Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }

            if (i < maxPrintedComponents)
            {
                bool closed = chain.Length > 1 && chain[0].Value == chain[^1].Value;
                Console.WriteLine($"  Component {i}: vertices={chain.Length}, approxLength={length:F3}, closed={closed}");
            }
        }

        if (meshATopology.Loops.Count > maxPrintedComponents)
        {
            Console.WriteLine($"  ... {meshATopology.Loops.Count - maxPrintedComponents} more components not shown");
        }

        Console.WriteLine();
        Console.WriteLine("Done. Press any key to exit.");
        Console.ReadKey();
    }
}

