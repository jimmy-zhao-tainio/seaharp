using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Seaharp.Geometry;
using Seaharp.Topology;
using Seaharp.Geometry.Computation;
using Seaharp.IO;

internal static class Program
{
    private static void Main(string[] args)
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var a = new Seaharp.World.Sphere(r, subdivisions: 3, center: aCenter);
        var b = new Seaharp.World.Sphere(r, subdivisions: 3, center: bCenter);

        var surfaceA = a.Mesh;
        var surfaceB = b.Mesh;

        // Compute intersection cuts; record all touched triangles and delete them.
        // Additionally, delete any triangle whose vertex lies inside (or on) the other surface.
        // Intersection demo temporarily disabled; writing both meshes as-is.
        var combined = new List<Triangle>(surfaceA.Triangles.Count + surfaceB.Triangles.Count);
        combined.AddRange(surfaceA.Triangles);
        combined.AddRange(surfaceB.Triangles);
        var outPath = "spheres_with_disc.stl";
        StlWriter.Write(combined, outPath);
        Console.WriteLine($"Wrote placeholder (no intersection): {System.IO.Path.GetFullPath(outPath)} with {combined.Count} triangles");
    }
}
