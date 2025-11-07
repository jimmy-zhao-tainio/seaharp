using System;
using System.Collections.Generic;

namespace Seaharp.World;

// Regular octahedron centered at a point. Built as star tetrahedra from center to each face.
public sealed class Octahedron : Shape
{
    public Octahedron(long radius, Seaharp.Geometry.Point? center = null)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        Radius = radius;

        var c = Center;
        // Exact integer vertices on axes
        var vx = new Seaharp.Geometry.Point(c.X + radius, c.Y, c.Z);
        var vnx = new Seaharp.Geometry.Point(c.X - radius, c.Y, c.Z);
        var vy = new Seaharp.Geometry.Point(c.X, c.Y + radius, c.Z);
        var vny = new Seaharp.Geometry.Point(c.X, c.Y - radius, c.Z);
        var vz = new Seaharp.Geometry.Point(c.X, c.Y, c.Z + radius);
        var vnz = new Seaharp.Geometry.Point(c.X, c.Y, c.Z - radius);

        // Faces (8): upper pyramid (vz) and lower pyramid (vnz)
        AddFace(vz, vx, vy);
        AddFace(vz, vy, vnx);
        AddFace(vz, vnx, vny);
        AddFace(vz, vny, vx);

        AddFace(vnz, vy, vx);
        AddFace(vnz, vnx, vy);
        AddFace(vnz, vny, vnx);
        AddFace(vnz, vx, vny);
    }

    public long Radius { get; }
    public Seaharp.Geometry.Point Center { get; }

    private void AddFace(in Seaharp.Geometry.Point a, in Seaharp.Geometry.Point b, in Seaharp.Geometry.Point d)
    {
        try { tetrahedrons.Add(new Seaharp.Geometry.Tetrahedron(Center, a, b, d)); }
        catch (InvalidOperationException) { }
    }
}

