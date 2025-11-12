using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World;

// A lightweight collection wrapper for a set of boundary triangles.
public sealed class Surface
{
    public IReadOnlyList<Triangle> Triangles => triangles;
    private readonly List<Triangle> triangles;

    // Snapshot boundary triangles for the provided shape at construction.
    public Surface(Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        var triangleOccurrences = new Dictionary<Seaharp.Surface.TriangleKey, (int count, Triangle triangle)>(shape.Tetrahedrons.Count * 4);
        void Accumulate(in Triangle triangle)
        {
            var key = Seaharp.Surface.TriangleKey.FromTriangle(triangle);
            if (triangleOccurrences.TryGetValue(key, out var entry))
                triangleOccurrences[key] = (entry.count + 1, entry.triangle);
            else
                triangleOccurrences[key] = (1, triangle);
        }
        foreach (var tetrahedron in shape.Tetrahedrons)
        {
            Accumulate(tetrahedron.ABC);
            Accumulate(tetrahedron.ABD);
            Accumulate(tetrahedron.ACD);
            Accumulate(tetrahedron.BCD);
        }
        var boundaryTriangles = new List<Triangle>();
        foreach (var keyValuePair in triangleOccurrences)
        {
            if (keyValuePair.Value.count == 1)
                boundaryTriangles.Add(keyValuePair.Value.triangle);
        }
        triangles = boundaryTriangles;
    }

    public int Count => triangles.Count;
}

