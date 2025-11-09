using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World;

// Triangle-indexed polyhedron in grid space.
// Vertices are grid Points; triangles reference vertex indices.
public sealed class Polyhedron
{
    public IReadOnlyList<Point> Vertices => _vertices;
    public IReadOnlyList<(int a, int b, int c)> Triangles => _triangles;

    private readonly List<Point> _vertices;
    private readonly List<(int a, int b, int c)> _triangles;

    public Polyhedron(List<Point> vertices, List<(int a, int b, int c)> triangles)
    {
        _vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        _triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
    }

    // Build a polyhedron by snapshotting a shape's boundary (Surface) and welding vertices.
    public static Polyhedron FromShape(Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        var surface = new Surface(shape);
        var tris = surface.Triangles;

        var indexOf = new Dictionary<Point, int>(tris.Count * 3);
        var verts = new List<Point>(tris.Count * 3);
        int IndexOf(in Point p)
        {
            if (indexOf.TryGetValue(p, out var idx)) return idx;
            idx = verts.Count;
            verts.Add(p);
            indexOf[p] = idx;
            return idx;
        }

        var faces = new List<(int a, int b, int c)>(tris.Count);
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            int a = IndexOf(t.P0);
            int b = IndexOf(t.P1);
            int c = IndexOf(t.P2);
            if (a == b || b == c || a == c) continue; // guard degenerates
            faces.Add((a, b, c));
        }

        return new Polyhedron(verts, faces);
    }

    // Convenience: materialize a Geometry.Triangle from a face index using current winding.
    public Triangle GetTriangle(int faceIndex)
    {
        var (a, b, c) = _triangles[faceIndex];
        var p0 = _vertices[a];
        var p1 = _vertices[b];
        var p2 = _vertices[c];
        return Triangle.FromWinding(p0, p1, p2);
    }
}
