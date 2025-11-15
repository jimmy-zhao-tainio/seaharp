using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.Topology;

// A lightweight collection wrapper for a set of triangles assumed to bound a closed volume.
public sealed class ClosedSurface
{
    public IReadOnlyList<Triangle> Triangles => triangles;
    private readonly List<Triangle> triangles;

    public ClosedSurface(IEnumerable<Triangle> triangles)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        this.triangles = new List<Triangle>(triangles);
    }

    public int Count => triangles.Count;

    // Factory: builds a ClosedSurface from a collection of tetrahedra by
    // selecting only boundary triangles (those that appear exactly once).
    public static ClosedSurface FromTetrahedra(IEnumerable<Tetrahedron> tetrahedra)
    {
        if (tetrahedra is null) throw new ArgumentNullException(nameof(tetrahedra));
        var triangleOccurrences = new Dictionary<TriangleKey, (int count, Triangle triangle)>();

        static void Accumulate(ref Dictionary<TriangleKey, (int count, Triangle triangle)> map, in Triangle triangle)
        {
            var key = TriangleKey.FromTriangle(triangle);
            if (map.TryGetValue(key, out var entry)) map[key] = (entry.count + 1, entry.triangle);
            else map[key] = (1, triangle);
        }

        foreach (var tetrahedron in tetrahedra)
        {
            Accumulate(ref triangleOccurrences, tetrahedron.ABC);
            Accumulate(ref triangleOccurrences, tetrahedron.ABD);
            Accumulate(ref triangleOccurrences, tetrahedron.ACD);
            Accumulate(ref triangleOccurrences, tetrahedron.BCD);
        }

        var boundary = new List<Triangle>();
        foreach (var pair in triangleOccurrences)
            if (pair.Value.count == 1) boundary.Add(pair.Value.triangle);

        return new ClosedSurface(boundary);
    }
}


