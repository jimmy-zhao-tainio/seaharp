using System;
using System.Collections.Generic;

namespace Seaharp.World;

// Regular icosahedron centered at a point. Built as star tetrahedra from center to each triangular face.
public sealed class Icosahedron : Shape
{
    public Icosahedron(long radius, Seaharp.Geometry.Point? center = null)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        Radius = radius;

        BuildIcosahedron(out var verts, out var faces);

        // Map to grid at given radius and dedup identical integer points
        var pointMap = new Dictionary<int, Seaharp.Geometry.Point>(verts.Count);
        var unique = new Dictionary<Seaharp.Geometry.Point, Seaharp.Geometry.Point>();
        for (int i = 0; i < verts.Count; i++)
        {
            var p = ProjectToGrid(verts[i], Center, radius);
            if (!unique.TryGetValue(p, out var stored))
            {
                unique[p] = p;
                stored = p;
            }
            pointMap[i] = stored;
        }

        var c = Center;
        foreach (var (aIdx, bIdx, dIdx) in faces)
        {
            var a = pointMap[aIdx];
            var b = pointMap[bIdx];
            var d = pointMap[dIdx];
            if (a.Equals(b) || a.Equals(d) || b.Equals(d)) continue;
            try { tetrahedrons.Add(new Seaharp.Geometry.Tetrahedron(c, a, b, d)); }
            catch (InvalidOperationException) { }
        }
    }

    public long Radius { get; }
    public Seaharp.Geometry.Point Center { get; }

    private readonly struct DVec
    {
        public readonly double X, Y, Z;
        public DVec(double x, double y, double z) { X = x; Y = y; Z = z; }
        public DVec Normalize()
        {
            var len = Math.Sqrt(X * X + Y * Y + Z * Z);
            if (len == 0) return this;
            var inv = 1.0 / len;
            return new DVec(X * inv, Y * inv, Z * inv);
        }
        public static DVec operator +(DVec a, DVec b) => new DVec(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static DVec operator /(DVec a, double s) => new DVec(a.X / s, a.Y / s, a.Z / s);
    }

    private static Seaharp.Geometry.Point ProjectToGrid(DVec v, Seaharp.Geometry.Point center, long radius)
    {
        var n = v.Normalize();
        long x = center.X + (long)Math.Round(n.X * radius);
        long y = center.Y + (long)Math.Round(n.Y * radius);
        long z = center.Z + (long)Math.Round(n.Z * radius);
        return new Seaharp.Geometry.Point(x, y, z);
    }

    private static void BuildIcosahedron(out List<DVec> verts, out List<(int a, int b, int c)> faces)
    {
        double phi = (1.0 + Math.Sqrt(5.0)) * 0.5;
        verts = new List<DVec>
        {
            new DVec(-1,  phi, 0), new DVec(1,  phi, 0), new DVec(-1, -phi, 0), new DVec(1, -phi, 0),
            new DVec(0, -1,  phi), new DVec(0, 1,  phi), new DVec(0, -1, -phi), new DVec(0, 1, -phi),
            new DVec( phi, 0, -1), new DVec( phi, 0, 1), new DVec(-phi, 0, -1), new DVec(-phi, 0, 1)
        };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].Normalize();

        int[][] f = new int[][]
        {
            new[]{0,11,5}, new[]{0,5,1}, new[]{0,1,7}, new[]{0,7,10}, new[]{0,10,11},
            new[]{1,5,9}, new[]{5,11,4}, new[]{11,10,2}, new[]{10,7,6}, new[]{7,1,8},
            new[]{3,9,4}, new[]{3,4,2}, new[]{3,2,6}, new[]{3,6,8}, new[]{3,8,9},
            new[]{4,9,5}, new[]{2,4,11}, new[]{6,2,10}, new[]{8,6,7}, new[]{9,8,1}
        };
        faces = new List<(int a, int b, int c)>(f.Length);
        foreach (var t in f) faces.Add((t[0], t[1], t[2]));
    }
}

