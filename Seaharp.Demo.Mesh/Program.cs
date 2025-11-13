using System;
using System.IO;
using Seaharp.Geometry;
using Seaharp.World;
using Seaharp.Topology;

internal class Program
{
    private static void Main(string[] args)
    {
        // Two large spheres with a non-axis-aligned offset (they intersect in a circle)
        long r = 200;
        var centerA = new Point(0, 0, 0);
        var centerB = new Point(150, 50, -30);

        var a = new Sphere(radius: r, subdivisions: 3, center: centerA);
        var b = new Sphere(radius: r, subdivisions: 3, center: centerB);

        var surfaceA = ClosedSurface.FromTetrahedra(a.Tetrahedra);
        var surfaceB = ClosedSurface.FromTetrahedra(b.Tetrahedra);

        var union = MeshBoolean.Union(surfaceA, surfaceB);

        // TODO: The seam in the preview looks like a "cracked egg". Track/fix in boolean pipeline:
        //  - Ensure loop orientation consistency before welding.
        //  - Apply all chords per triangle (done) and introduce interior vertices for crossing chords.
        //  - Handle coplanar overlaps and dedup slivers after snapping.
        //  - Add manifold post-check and optional repair.

        var path = args != null && args.Length > 0 ? args[0] : "union_spheres.stl";
        StlWriter.Write(union, path);
        Console.WriteLine($"Wrote union STL: {Path.GetFullPath(path)} (triangles: {union.Triangles.Count})");
    }
}
