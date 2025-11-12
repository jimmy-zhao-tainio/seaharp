using System;
using System.Collections.Generic;
using Seaharp.Geometry;
using Seaharp.Topology;

namespace Seaharp.World;

public static class ShapeExtensions
{
    // Builds a Surface by extracting boundary triangles from a shape.
    public static Surface ExtractSurface(this Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        var triangleOccurrences = new Dictionary<TriangleKey, (int count, Triangle triangle)>(shape.Tetrahedrons.Count * 4);
        void Accumulate(in Triangle triangle)
        {
            var key = TriangleKey.FromTriangle(triangle);
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
        foreach (var item in triangleOccurrences)
        {
            if (item.Value.count == 1) boundaryTriangles.Add(item.Value.triangle);
        }
        return new Surface(boundaryTriangles);
    }
}


