using System.Collections.Generic;
using Seaharp.Topology;

namespace Seaharp.World;

public sealed class Box : Shape
{
    public Box(long width, long depth, long height)
    {
        Width = width;
        Depth = depth;
        Height = height;

        var origin = new Seaharp.Geometry.Point(0, 0, 0);

        var p000 = origin;
        var p100 = new Seaharp.Geometry.Point(origin.X + Width, origin.Y, origin.Z);
        var p010 = new Seaharp.Geometry.Point(origin.X, origin.Y + Depth, origin.Z);
        var p001 = new Seaharp.Geometry.Point(origin.X, origin.Y, origin.Z + Height);
        var p110 = new Seaharp.Geometry.Point(origin.X + Width, origin.Y + Depth, origin.Z);
        var p101 = new Seaharp.Geometry.Point(origin.X + Width, origin.Y, origin.Z + Height);
        var p011 = new Seaharp.Geometry.Point(origin.X, origin.Y + Depth, origin.Z + Height);
        var p111 = new Seaharp.Geometry.Point(origin.X + Width, origin.Y + Depth, origin.Z + Height);

        var tetrahedra = new List<Seaharp.Geometry.Tetrahedron>(5)
        {
            new Seaharp.Geometry.Tetrahedron(p000, p100, p010, p001),
            new Seaharp.Geometry.Tetrahedron(p100, p110, p010, p111),
            new Seaharp.Geometry.Tetrahedron(p100, p010, p001, p111),
            new Seaharp.Geometry.Tetrahedron(p010, p001, p011, p111),
            new Seaharp.Geometry.Tetrahedron(p100, p001, p101, p111)
        };

        Mesh = ClosedSurface.FromTetrahedra(tetrahedra);
    }

    public long Width { get; }
    public long Depth { get; }
    public long Height { get; }
}
