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
        // Solar system demo made of icospheres
        string outName = Path.GetFileNameWithoutExtension(outPath);

        // Sun
        var sun = new Sphere(radius: 180, subdivisions: 4, center: new Seaharp.Geometry.Point(0, 0, 0));
        world.Add(sun);

        // Planets
        var p1 = new Sphere(radius: 70, subdivisions: 3, center: new Seaharp.Geometry.Point(480, 0, 0));
        var p2 = new Sphere(radius: 50, subdivisions: 3, center: new Seaharp.Geometry.Point(860, 0, 0));
        var p3 = new Sphere(radius: 42, subdivisions: 3, center: new Seaharp.Geometry.Point(1200, 0, 0));
        world.Add(p1);
        world.Add(p2);
        world.Add(p3);

        // Ring of moons around planet 2
        int moons = 18;
        long moonRing = 140;
        for (int i = 0; i < moons; i++)
        {
            double a = i * (2 * Math.PI / moons);
            long dx = (long)Math.Round(Math.Cos(a) * moonRing);
            long dy = (long)Math.Round(Math.Sin(a) * moonRing);
            long dz = (long)Math.Round(12 * Math.Sin(i * 0.4));
            var mCenter = new Seaharp.Geometry.Point(p2.Center.X + dx, p2.Center.Y + dy, p2.Center.Z + dz);
            var moon = new Sphere(radius: 14, subdivisions: 2, center: mCenter);
            world.Add(moon);
        }

        // Asteroid belt around the sun (tiny spheres)
        int ast = 48;
        long belt = 1000;
        for (int i = 0; i < ast; i++)
        {
            double t = i * (2 * Math.PI / ast);
            long x = (long)Math.Round(Math.Cos(t) * belt);
            long y = (long)Math.Round(Math.Sin(t) * belt);
            long z = (long)Math.Round(25 * Math.Sin(i * 0.3));
            var aCenter = new Seaharp.Geometry.Point(x, y, z);
            var rock = new Sphere(radius: 10, subdivisions: 1, center: aCenter);
            world.Add(rock);
        }

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
