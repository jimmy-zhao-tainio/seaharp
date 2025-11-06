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

        // 1) Hero: Box on a corner (body diagonal up)
        var cornerBox = new Box(width: 28, depth: 28, height: 28);
        cornerBox.Rotate(35.26438968, 45.0, 0.0);
        cornerBox.Position(0, 0, 0);
        world.Add(cornerBox);

        // 2) Helix of small boxes spiraling upward
        int helixCount = 24;
        double radius = 80;
        double step = 6;
        for (int i = 0; i < helixCount; i++)
        {
            double t = i * (Math.PI * 2 / 8.0);
            long x = (long)Math.Round(Math.Cos(t) * radius);
            long y = (long)Math.Round(Math.Sin(t) * radius);
            long z = (long)Math.Round(i * step);
            int w = 8 + (int)(4 * Math.Sin(i * 0.7));
            int d = 8 + (int)(4 * Math.Cos(i * 0.6));
            int h = 10 + (int)(3 * Math.Sin(i * 0.9));

            var b = new Box(width: w, depth: d, height: h);
            b.Rotate(i * 10.0, i * 7.0, i * 13.0);
            b.Position(x, y, z);
            world.Add(b);
        }

        // 3) Flower ring of tilted boxes
        int petals = 10;
        double ringR = 120;
        for (int k = 0; k < petals; k++)
        {
            double a = k * (360.0 / petals);
            long x = (long)Math.Round(Math.Cos(a * Math.PI / 180.0) * ringR);
            long y = (long)Math.Round(Math.Sin(a * Math.PI / 180.0) * ringR);
            var p = new Box(width: 14, depth: 10, height: 22);
            p.Rotate(20 + 10 * Math.Sin(k), a, 15);
            p.Position(x, y, -10);
            world.Add(p);
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

        // Save OBJ + MTL + vertex normals (if .obj), and also STL
        if (Path.GetExtension(outPath).Equals(".obj", StringComparison.OrdinalIgnoreCase))
        {
            world.Save(outPath);
            Console.WriteLine($"Wrote OBJ: {outPath}");
            var stl = Path.ChangeExtension(outPath, ".stl");
            world.SaveStl(stl);
            Console.WriteLine($"Wrote STL: {stl}");
        }
        else
        {
            world.SaveStl(outPath);
            Console.WriteLine($"Wrote STL: {outPath}");
        }
    }
}
