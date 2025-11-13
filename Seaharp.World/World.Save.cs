using System;
using System.Collections.Generic;
using System.IO;
using Seaharp.Topology;

namespace Seaharp.World;

public sealed partial class World
{
    // Binary STL export (little-endian). Includes per-triangle normals.
    // TODO: Duplicate STL writing logic exists in Seaharp.Demo.Mesh. Consolidate into a
    //       shared writer (e.g., Topology.StlWriter) that takes a ClosedSurface directly,
    //       and make World.Save delegate to it.
    // TODO: Consider lifting World.Shape from a tetrahedra-only container to a generic
    //       ClosedSurface-producing abstraction so export/booleans share the same mesh type.
    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));

        var tris = new List<Seaharp.Geometry.Triangle>();
        foreach (var s in shapes)
        {
            // Use the unified ClosedSurface pipeline via Shape.Mesh
            var surface = s.Mesh;
            tris.AddRange(surface.Triangles);
        }

        // Delegate to shared STL writer
        StlWriter.Write(tris, path);
    }
}






