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

        var path = args != null && args.Length > 0 ? args[0] : "union_spheres.stl";
        WriteBinaryStl(union, path);
        Console.WriteLine($"Wrote union STL: {Path.GetFullPath(path)} (triangles: {union.Triangles.Count})");
    }

    private static void WriteBinaryStl(ClosedSurface surface, string path)
    {
        var tris = surface.Triangles;
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        var header = new byte[80];
        var tag = System.Text.Encoding.ASCII.GetBytes("Seaharp.Demo.Mesh Union");
        Array.Copy(tag, header, Math.Min(header.Length, tag.Length));
        bw.Write(header);

        bw.Write((uint)tris.Count);

        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            // normal (3 floats)
            bw.Write((float)t.Normal.X);
            bw.Write((float)t.Normal.Y);
            bw.Write((float)t.Normal.Z);
            // vertices (9 floats)
            bw.Write((float)t.P0.X); bw.Write((float)t.P0.Y); bw.Write((float)t.P0.Z);
            bw.Write((float)t.P1.X); bw.Write((float)t.P1.Y); bw.Write((float)t.P1.Z);
            bw.Write((float)t.P2.X); bw.Write((float)t.P2.Y); bw.Write((float)t.P2.Z);
            // attribute byte count
            bw.Write((ushort)0);
        }
    }
}

