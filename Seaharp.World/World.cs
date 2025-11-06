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

        // Collect boundary triangles from each shape independently
        var boundary = new List<Seaharp.Geometry.Tetrahedron.Triangle>();
        foreach (var s in shapes)
        {
            boundary.AddRange(s.GetBoundaryTriangles());
        }

        // Write OBJ
        using var sw = new StreamWriter(path);
        sw.NewLine = "\n";
        sw.WriteLine("# Seaharp.World OBJ export");

        var vertexIndex = new Dictionary<Seaharp.Geometry.Point, int>();
        int nextIndex = 1;
        foreach (var tri in boundary)
        {
            IndexVertex(tri.P0); IndexVertex(tri.P1); IndexVertex(tri.P2);
        }

        // Write vertices
        foreach (var kv in vertexIndex.OrderBy(k => k.Value))
        {
            sw.WriteLine(string.Create(CultureInfo.InvariantCulture, $"v {kv.Key.X} {kv.Key.Y} {kv.Key.Z}"));
        }

        // Write triangles (OBJ faces) using stored outward ordering
        foreach (var tri in boundary)
        {
            var i0 = vertexIndex[tri.P0];
            var i1 = vertexIndex[tri.P1];
            var i2 = vertexIndex[tri.P2];
            sw.WriteLine($"f {i0} {i1} {i2}");
        }

        void IndexVertex(Seaharp.Geometry.Point p)
        {
            if (!vertexIndex.ContainsKey(p))
            {
                vertexIndex[p] = nextIndex++;
            }
        }
    }
}
