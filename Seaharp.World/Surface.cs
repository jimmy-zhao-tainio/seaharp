using System;
using System.Collections.Generic;
using Seaharp.Geometry.Predicates;

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

        triangles = new List<Seaharp.Geometry.Tetrahedron.Triangle>(shape.GetSurface());
    }

    public int Count => triangles.Count;
}
