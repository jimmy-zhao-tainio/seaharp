using Seaharp.IO;
using Seaharp.Geometry;

namespace Seaharp.World;

public sealed partial class World
{
    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));

        var triangles = new List<Triangle>();
        foreach (var shape in Shapes)
        {
            triangles.AddRange(shape.Mesh.Triangles);
        }

        StlWriter.Write(triangles, path);
    }
}