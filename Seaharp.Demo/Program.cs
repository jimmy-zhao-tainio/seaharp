using System;
using System.Globalization;
using System.IO;
using Seaharp.World;

internal class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var outPath = args.Length > 0 ? args[0] : "demo.obj";
        var outDir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrWhiteSpace(outDir)) Directory.CreateDirectory(outDir!);

        var world = new Seaharp.World.World();

        // Replace demo scene with an icosphere built from tetrahedra
        var center = new Seaharp.Geometry.Point(0, 0, 0);
        var sphere = new Sphere(radius: 90, subdivisions: 2, center: center);
        world.Add(sphere);

        // 4) Möbius-like ring of twisted boxes
        int twists = 1; // one half-twist along the ring
        int segs = 48;
        double R = 180.0; // ring radius
        for (int i = 0; i < segs; i++)
        {
            double t = (double)i / segs;
            double ang = t * 360.0;
            long x = (long)Math.Round(Math.Cos(ang * Math.PI / 180.0) * R);
            long y = (long)Math.Round(Math.Sin(ang * Math.PI / 180.0) * R);
            long z = (long)Math.Round(15 * Math.Sin(t * 2 * Math.PI));
            double twist = 180.0 * twists * t; // gradual half-twist
            var strip = new Box(width: 10, depth: 4, height: 26);
            strip.Rotate(10 + 10 * Math.Sin(i * 0.5), ang, twist);
            strip.Position(x, y, z);
            world.Add(strip);
        }

        // STL-only export (OBJ disabled due to non-manifold issues in slicers)
        var stlPath = Path.ChangeExtension(outPath, ".stl");
        world.Save(stlPath);
        Console.WriteLine($"Wrote STL: {stlPath}");
    }
}
