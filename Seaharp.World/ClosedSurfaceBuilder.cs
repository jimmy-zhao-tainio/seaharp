using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World;

public static class ClosedSurfaceBuilder
{
    // Build a Surface by extracting boundary triangles from a shape.
    public static Seaharp.Surface.ClosedSurface FromShape(Shape shape)
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
        foreach (var kv in triangleOccurrences)
        {
            if (kv.Value.count == 1) boundaryTriangles.Add(kv.Value.triangle);
        }
        return new Seaharp.Surface.ClosedSurface(boundaryTriangles);
    }
}




