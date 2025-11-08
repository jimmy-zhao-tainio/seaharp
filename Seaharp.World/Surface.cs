using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World;

// A lightweight collection wrapper for a set of boundary triangles.
public sealed class Surface
{
    public IReadOnlyList<Seaharp.Geometry.Tetrahedron.Triangle> Triangles => triangles;
    private readonly List<Seaharp.Geometry.Tetrahedron.Triangle> triangles;

    // Snapshot boundary triangles for the provided shape at construction.
    public Surface(Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        var map = new Dictionary<TriangleKey, (int count, Seaharp.Geometry.Tetrahedron.Triangle tri)>(shape.Tetrahedrons.Count * 4);
        void Acc(in Seaharp.Geometry.Tetrahedron.Triangle t)
        {
            var key = TriangleKey.FromTriangle(t);
            if (map.TryGetValue(key, out var e)) map[key] = (e.count + 1, e.tri); else map[key] = (1, t);
        }
        foreach (var t in shape.Tetrahedrons)
        {
            Acc(t.ABC); Acc(t.ABD); Acc(t.ACD); Acc(t.BCD);
        }
        var boundary = new List<Seaharp.Geometry.Tetrahedron.Triangle>();
        foreach (var kv in map) if (kv.Value.count == 1) boundary.Add(kv.Value.tri);
        triangles = boundary;
    }

    public int Count => triangles.Count;
}
