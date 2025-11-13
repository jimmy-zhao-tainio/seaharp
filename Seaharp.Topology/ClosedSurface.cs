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
        var triangleOccurrences = new Dictionary<TriangleKey, (int count, Triangle tri)>();

        static void Accumulate(ref Dictionary<TriangleKey, (int count, Triangle tri)> map, in Triangle t)
        {
            var key = TriangleKey.FromTriangle(t);
            if (map.TryGetValue(key, out var entry)) map[key] = (entry.count + 1, entry.tri);
            else map[key] = (1, t);
        }

        foreach (var tet in tetrahedra)
        {
            Accumulate(ref triangleOccurrences, tet.ABC);
            Accumulate(ref triangleOccurrences, tet.ABD);
            Accumulate(ref triangleOccurrences, tet.ACD);
            Accumulate(ref triangleOccurrences, tet.BCD);
        }

        var boundary = new List<Triangle>();
        foreach (var kv in triangleOccurrences)
            if (kv.Value.count == 1) boundary.Add(kv.Value.tri);

        return new ClosedSurface(boundary);
    }
}


