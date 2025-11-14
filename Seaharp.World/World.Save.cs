using System;
using System.Collections.Generic;
using Seaharp.Topology;
using Seaharp.IO;

namespace Seaharp.World;

public sealed partial class World
{
    // Binary STL export (little-endian).\r\n    public void Save(string path)
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








