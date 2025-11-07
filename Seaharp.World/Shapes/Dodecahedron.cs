using System;
using System.Collections.Generic;

namespace Seaharp.World;

// Regular dodecahedron centered at a point.
// Constructed as the dual of an icosahedron: dodeca vertices are face centers of the icosahedron.
// Faces (12 pentagons) are obtained by ordering adjacent icosa faces around each icosa vertex.
public sealed class Dodecahedron : Shape
{
    public Dodecahedron(long radius, Seaharp.Geometry.Point? center = null)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        Radius = radius;

        BuildIcosahedron(out var icoVerts, out var icoFaces);

        // Compute centers (normalized) of icosahedron faces -> these are the 20 dodeca vertices on unit sphere
        var faceCenters = new List<DVec>(icoFaces.Count);
        foreach (var (a, b, c) in icoFaces)
        {
            var cen = (icoVerts[a] + icoVerts[b] + icoVerts[c]) / 3.0;
            faceCenters.Add(cen);
        }

        // For each icosahedron vertex, collect adjacent face indices (5 each), order them cyclically
        var facesByVertex = new List<int>[icoVerts.Count];
        for (int i = 0; i < icoVerts.Count; i++) facesByVertex[i] = new List<int>(5);
        for (int f = 0; f < icoFaces.Count; f++)
        {
            var (a, b, c) = icoFaces[f];
            facesByVertex[a].Add(f);
            facesByVertex[b].Add(f);
            facesByVertex[c].Add(f);
        }

        var dodecaFaces = new List<List<int>>(icoVerts.Count); // 12 pentagons, each as list of indices into faceCenters
        for (int vi = 0; vi < icoVerts.Count; vi++)
        {
            var fc = facesByVertex[vi];
            if (fc.Count != 5) continue; // safety

            // Build a local orthonormal basis around the icosa vertex direction to sort the centers
            var vdir = icoVerts[vi].Normalize();
            var up = Math.Abs(vdir.Z) < 0.9 ? new DVec(0, 0, 1) : new DVec(0, 1, 0);
            var u = Cross(up, vdir).Normalize();
            var w = Cross(vdir, u).Normalize();

            var idxAngles = new List<(int idx, double ang)>(5);
            foreach (var fidx in fc)
            {
                var p = faceCenters[fidx].Normalize();
                double x = Dot(p, u);
                double y = Dot(p, w);
                double ang = Math.Atan2(y, x);
                idxAngles.Add((fidx, ang));
            }
            idxAngles.Sort((l, r) => l.ang.CompareTo(r.ang));

            var poly = new List<int>(5);
            foreach (var (idx, _) in idxAngles) poly.Add(idx);
            dodecaFaces.Add(poly);
        }

        // Map unique dodeca vertices to integer grid
        var unique = new Dictionary<Seaharp.Geometry.Point, Seaharp.Geometry.Point>();
        var centerMap = new Dictionary<int, Seaharp.Geometry.Point>(faceCenters.Count);
        for (int i = 0; i < faceCenters.Count; i++)
        {
            var p = ProjectToGrid(faceCenters[i], Center, radius);
            if (!unique.TryGetValue(p, out var stored))
            {
                unique[p] = p;
                stored = p;
            }
            centerMap[i] = stored;
        }

        // Star tetrahedra: center + triangulated pentagon faces
        var cpt = Center;
        foreach (var poly in dodecaFaces)
        {
            if (poly.Count < 3) continue;
            var v0 = centerMap[poly[0]];
            for (int i = 1; i + 1 < poly.Count; i++)
            {
                var v1 = centerMap[poly[i]];
                var v2 = centerMap[poly[i + 1]];
                if (v0.Equals(v1) || v0.Equals(v2) || v1.Equals(v2)) continue;
                try { tetrahedrons.Add(new Seaharp.Geometry.Tetrahedron(cpt, v0, v1, v2)); }
                catch (InvalidOperationException) { }
            }
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

    private static double Dot(in DVec a, in DVec b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    private static DVec Cross(in DVec a, in DVec b)
        => new DVec(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

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

