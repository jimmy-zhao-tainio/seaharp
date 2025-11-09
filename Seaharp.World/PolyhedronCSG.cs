using System;
using System.Collections.Generic;
using Seaharp.Geometry;
using System.Linq;

namespace Seaharp.World;

// Boolean operations over Polyhedron. Initial implementation: naive union without splitting.
public static class PolyhedronCSG
{
    // Naive union: drop faces whose centroids lie strictly inside the other mesh; keep the rest.
    // Works correctly for disjoint meshes and strict containment. For intersecting meshes without splitting,
    // the result is not guaranteed to be manifold.
    public static Polyhedron UnionNaive(Polyhedron a, Polyhedron b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var keepKeys = new HashSet<TriangleKey>();
        var keptTris = new List<(Point p0, Point p1, Point p2)>();

        void Accumulate(Polyhedron src, Polyhedron other)
        {
            var faces = src.Triangles; var verts = src.Vertices;
            for (int i = 0; i < faces.Count; i++)
            {
                var (ia, ib, ic) = faces[i];
                var p0 = verts[ia]; var p1 = verts[ib]; var p2 = verts[ic];
                var tri = Triangle.FromWinding(p0, p1, p2);
                var c = Centroid(p0, p1, p2);
                var cls = Classify(other, c);
                bool inside = cls == Classification.Inside;
                if (!inside)
                {
                    var key = TriangleKey.FromTriangle(tri);
                    if (keepKeys.Add(key)) keptTris.Add((p0, p1, p2));
                }
            }
        }

        Accumulate(a, b);
        Accumulate(b, a);

        // Weld vertices
        var indexOf = new Dictionary<Point, int>(keptTris.Count * 2);
        var outVerts = new List<Point>(keptTris.Count * 2);
        int IndexOf(in Point p)
        {
            if (indexOf.TryGetValue(p, out var idx)) return idx;
            idx = outVerts.Count; outVerts.Add(p); indexOf[p] = idx; return idx;
        }

        var outFaces = new List<(int a, int b, int c)>(keptTris.Count);
        foreach (var (p0, p1, p2) in keptTris)
        {
            int aIdx = IndexOf(p0), bIdx = IndexOf(p1), cIdx = IndexOf(p2);
            if (aIdx == bIdx || bIdx == cIdx || aIdx == cIdx) continue;
            outFaces.Add((aIdx, bIdx, cIdx));
        }

        return new Polyhedron(outVerts, outFaces);
    }

    private static Point Centroid(in Point a, in Point b, in Point c)
        => new Point((a.X + b.X + c.X) / 3, (a.Y + b.Y + c.Y) / 3, (a.Z + b.Z + c.Z) / 3);

    private enum Classification { Outside, Inside, Boundary }

    // Local point-in-polyhedron classification (non-axis-aligned ray cast) to avoid cross-project deps.
    private static Classification Classify(Polyhedron poly, Point p, double eps = Tolerances.PlaneSideEpsilon)
    {
        var faces = poly.Triangles; var verts = poly.Vertices;

        // Boundary quick-check
        for (int i = 0; i < faces.Count; i++)
        {
            var (a, b, c) = faces[i];
            var tri = Triangle.FromWinding(verts[a], verts[b], verts[c]);
            var plane = Plane.FromTriangle(tri);
            if (plane.Side(p, eps) == 0 && PointOnTriangle(p, tri, eps)) return Classification.Boundary;
        }

        const double dx = 0.42412451, dy = 0.7315123, dz = 0.535087;
        var ray = new RayD(p.X + eps * 3, p.Y + eps * 5, p.Z + eps * 7, dx, dy, dz);
        int hits = 0;
        for (int i = 0; i < faces.Count; i++)
        {
            var (a, b, c) = faces[i];
            var tri = Triangle.FromWinding(verts[a], verts[b], verts[c]);
            if (Intersections.RayTriangle(ray, tri, eps, out var t, out var u, out var v)) hits++;
        }
        return (hits & 1) == 1 ? Classification.Inside : Classification.Outside;
    }

    private static bool PointOnTriangle(in Point p, in Triangle tri, double eps)
    {
        var ax = (double)tri.P0.X; var ay = (double)tri.P0.Y; var az = (double)tri.P0.Z;
        var bx = (double)tri.P1.X; var by = (double)tri.P1.Y; var bz = (double)tri.P1.Z;
        var cx = (double)tri.P2.X; var cy = (double)tri.P2.Y; var cz = (double)tri.P2.Z;
        var px = (double)p.X; var py = (double)p.Y; var pz = (double)p.Z;

        var nx = tri.Normal.X; var ny = tri.Normal.Y; var nz = tri.Normal.Z;
        double ax0, ay0, bx0, by0, cx0, cy0, px0, py0;
        if (Math.Abs(nx) >= Math.Abs(ny) && Math.Abs(nx) >= Math.Abs(nz))
        { ax0 = ay; ay0 = az; bx0 = by; by0 = bz; cx0 = cy; cy0 = cz; px0 = py; py0 = pz; }
        else if (Math.Abs(ny) >= Math.Abs(nz))
        { ax0 = ax; ay0 = az; bx0 = bx; by0 = bz; cx0 = cx; cy0 = cz; px0 = px; py0 = pz; }
        else
        { ax0 = ax; ay0 = ay; bx0 = bx; by0 = by; cx0 = cx; cy0 = cy; px0 = px; py0 = py; }

        double area = Cross2(bx0 - ax0, by0 - ay0, cx0 - ax0, cy0 - ay0);
        if (Math.Abs(area) < eps) return false;
        double w0 = Cross2(bx0 - px0, by0 - py0, cx0 - px0, cy0 - py0) / area;
        double w1 = Cross2(cx0 - px0, cy0 - py0, ax0 - px0, ay0 - py0) / area;
        double w2 = 1.0 - w0 - w1;
        return w0 >= -eps && w1 >= -eps && w2 >= -eps;
    }

    private static double Cross2(double ax, double ay, double bx, double by) => ax * by - ay * bx;
}
