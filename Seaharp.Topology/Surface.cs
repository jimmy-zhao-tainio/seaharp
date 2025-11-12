using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.Topology;

// A lightweight collection wrapper for a set of triangles assumed to bound a closed volume.
public sealed class Surface
{
    public IReadOnlyList<Triangle> Triangles => _triangles;
    private readonly List<Triangle> _triangles;

    public Surface(IEnumerable<Triangle> triangles)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));
        _triangles = new List<Triangle>(triangles);
    }

    public int Count => _triangles.Count;
}


