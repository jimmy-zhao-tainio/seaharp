using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Seaharp.World;

public sealed class World
{
    private readonly List<Shape> shapes = new();

    public void Add(Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        shapes.Add(shape);
    }

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));

        // Collect surface triangles from each shape independently, preserving grouping
        var boundary = new List<(int shapeIndex, Seaharp.Geometry.Tetrahedron.Triangle tri)>();
        for (int si = 0; si < shapes.Count; si++)
        {
            foreach (var tri in shapes[si].GetSurface())
            {
                boundary.Add((si, tri));
            }
        }

        // Prepare paths and writers
        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        var objName = Path.GetFileName(path);
        var mtlName = Path.ChangeExtension(objName, ".mtl");

        using var sw = new StreamWriter(path);
        sw.NewLine = "\n";
        sw.WriteLine("# Seaharp.World OBJ export");
        if (!string.IsNullOrEmpty(mtlName))
        {
            sw.WriteLine($"mtllib {mtlName}");
        }

        var vertexIndex = new Dictionary<Seaharp.Geometry.Point, int>();
        var normalSums = new Dictionary<Seaharp.Geometry.Point, Seaharp.Geometry.Vector>();
        int nextIndex = 1;
        foreach (var entry in boundary)
        {
            var tri = entry.tri;
            IndexVertex(tri.P0); IndexVertex(tri.P1); IndexVertex(tri.P2);
            // accumulate per-vertex normal sums (using unit face normals)
            Accumulate(tri.P0, tri);
            Accumulate(tri.P1, tri);
            Accumulate(tri.P2, tri);
        }

        // Write vertices
        foreach (var kv in vertexIndex.OrderBy(k => k.Value))
        {
            sw.WriteLine(string.Create(CultureInfo.InvariantCulture, $"v {kv.Key.X} {kv.Key.Y} {kv.Key.Z}"));
        }

        // Write vertex normals in the same index order as vertices
        var normalIndex = new Dictionary<Seaharp.Geometry.Point, int>();
        int nextNormal = 1;
        foreach (var kv in vertexIndex.OrderBy(k => k.Value))
        {
            var p = kv.Key;
            if (!normalSums.TryGetValue(p, out var sum)) sum = new Seaharp.Geometry.Vector(0, 0, 0);
            var n = sum.Normalized();
            sw.WriteLine(string.Create(CultureInfo.InvariantCulture, $"vn {n.X} {n.Y} {n.Z}"));
            normalIndex[p] = nextNormal++;
        }

        // Simple material palette per shape
        var palette = BuildPalette(shapes.Count);
        int currentShape = -1;

        // Write triangles (OBJ faces) using stored outward ordering
        foreach (var entry in boundary)
        {
            if (entry.shapeIndex != currentShape)
            {
                currentShape = entry.shapeIndex;
                sw.WriteLine($"g shape_{currentShape}");
                sw.WriteLine($"usemtl m_{currentShape}");
            }

            var tri = entry.tri;
            var i0 = vertexIndex[tri.P0];
            var i1 = vertexIndex[tri.P1];
            var i2 = vertexIndex[tri.P2];
            var n0 = normalIndex[tri.P0];
            var n1 = normalIndex[tri.P1];
            var n2 = normalIndex[tri.P2];
            sw.WriteLine($"f {i0}//{n0} {i1}//{n1} {i2}//{n2}");
        }

        void IndexVertex(Seaharp.Geometry.Point p)
        {
            if (!vertexIndex.ContainsKey(p))
            {
                vertexIndex[p] = nextIndex++;
            }
        }

        void Accumulate(Seaharp.Geometry.Point p, in Seaharp.Geometry.Tetrahedron.Triangle tri)
        {
            var add = new Seaharp.Geometry.Vector(tri.Normal.X, tri.Normal.Y, tri.Normal.Z);
            if (normalSums.TryGetValue(p, out var cur)) normalSums[p] = cur + add; else normalSums[p] = add;
        }

        // Write MTL file with per-shape diffuse colors
        try
        {
            var mtlPath = Path.Combine(dir, mtlName);
            using var mw = new StreamWriter(mtlPath);
            mw.NewLine = "\n";
            mw.WriteLine("# Seaharp.World materials");
            for (int i = 0; i < shapes.Count; i++)
            {
                var (r, g, b) = palette[i];
                mw.WriteLine($"newmtl m_{i}");
                mw.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Kd {r} {g} {b}"));
                mw.WriteLine("d 1.0");
                mw.WriteLine("illum 1");
                mw.WriteLine();
            }
        }
        catch
        {
            // ignore material write errors
        }
    }

    private static (double r, double g, double b)[] BuildPalette(int count)
    {
        var list = new (double, double, double)[count];
        for (int i = 0; i < count; i++)
        {
            // golden ratio hue steps for visually distinct colors
            double h = (i * 0.61803398875) % 1.0;
            (double r, double g, double b) = HsvToRgb(h, 0.5, 0.9);
            list[i] = (r, g, b);
        }
        return list;
    }

    private static (double r, double g, double b) HsvToRgb(double h, double s, double v)
    {
        double i = Math.Floor(h * 6);
        double f = h * 6 - i;
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);
        switch ((int)i % 6)
        {
            case 0: return (v, t, p);
            case 1: return (q, v, p);
            case 2: return (p, v, t);
            case 3: return (p, q, v);
            case 4: return (t, p, v);
            default: return (v, p, q);
        }
    }

    // Binary STL export (little-endian). Includes per-triangle normals.
    public void SaveStl(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));

        // Collect all surface triangles across all shapes
        var tris = new List<Seaharp.Geometry.Tetrahedron.Triangle>();
        foreach (var s in shapes)
        {
            tris.AddRange(s.GetSurface());
        }

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        // 80-byte header
        var header = new byte[80];
        var tag = System.Text.Encoding.ASCII.GetBytes("Seaharp.World STL");
        Array.Copy(tag, header, Math.Min(header.Length, tag.Length));
        bw.Write(header);

        // Triangle count (uint32)
        bw.Write((uint)tris.Count);

        foreach (var tri in tris)
        {
            // normal (3 floats)
            bw.Write((float)tri.Normal.X);
            bw.Write((float)tri.Normal.Y);
            bw.Write((float)tri.Normal.Z);

            // vertices (9 floats)
            bw.Write((float)tri.P0.X); bw.Write((float)tri.P0.Y); bw.Write((float)tri.P0.Z);
            bw.Write((float)tri.P1.X); bw.Write((float)tri.P1.Y); bw.Write((float)tri.P1.Z);
            bw.Write((float)tri.P2.X); bw.Write((float)tri.P2.Y); bw.Write((float)tri.P2.Z);

            // attribute byte count
            bw.Write((ushort)0);
        }
    }
}
