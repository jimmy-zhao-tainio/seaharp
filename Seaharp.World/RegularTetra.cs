using System;

namespace Seaharp.World;

// Regular tetrahedron centered at a point using symmetric cube-corner coordinates.
// Vertices are permutations of (±1, ±1, ±1) with an even number of negatives.
public sealed class RegularTetra : Shape
{
    public RegularTetra(long scale, Seaharp.Geometry.Point? center = null)
    {
        if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        Scale = scale;

        // Define the four vertices in double space then map to grid.
        var v = new (double x, double y, double z)[]
        {
            ( 1,  1,  1),
            ( 1, -1, -1),
            (-1,  1, -1),
            (-1, -1,  1)
        };

        var pts = new Seaharp.Geometry.Point[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            long x = Center.X + (long)Math.Round(v[i].x * scale);
            long y = Center.Y + (long)Math.Round(v[i].y * scale);
            long z = Center.Z + (long)Math.Round(v[i].z * scale);
            pts[i] = new Seaharp.Geometry.Point(x, y, z);
        }

        // One tetrahedron fills the solid; use index order to keep orientation consistent
        try { tetrahedrons.Add(new Seaharp.Geometry.Tetrahedron(pts[0], pts[1], pts[2], pts[3])); }
        catch (InvalidOperationException) { }
    }

    public long Scale { get; }
    public Seaharp.Geometry.Point Center { get; }
}

