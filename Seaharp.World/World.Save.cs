using System;
using System.Collections.Generic;
using System.IO;

namespace Seaharp.World;

public sealed partial class World
{
    // Binary STL export (little-endian). Includes per-triangle normals.
    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));

        var tris = new List<Seaharp.Geometry.Triangle>();
        foreach (var s in shapes)
        {
            var surface = Seaharp.World.SurfaceBuilder.FromShape(s);
            tris.AddRange(surface.Triangles);
        }

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        // 80-byte header
        var header = new byte[80];
        var tag = System.Text.Encoding.ASCII.GetBytes("Seaharp.World STL");
        Array.Copy(tag, header, Math.Min(header.Length, tag.Length));
        bw.Write(header);

        // Triangle count (uint32)
        bw.Write((uint)tris.Count);

        foreach (var tri in tris)
        {
            // normal (3 floats)
            bw.Write((float)tri.Normal.X);
            bw.Write((float)tri.Normal.Y);
            bw.Write((float)tri.Normal.Z);

            // vertices (9 floats)
            bw.Write((float)tri.P0.X); bw.Write((float)tri.P0.Y); bw.Write((float)tri.P0.Z);
            bw.Write((float)tri.P1.X); bw.Write((float)tri.P1.Y); bw.Write((float)tri.P1.Z);
            bw.Write((float)tri.P2.X); bw.Write((float)tri.P2.Y); bw.Write((float)tri.P2.Z);

            // attribute byte count
            bw.Write((ushort)0);
        }
    }
}

