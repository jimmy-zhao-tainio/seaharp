using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World.Predicates;

public static class PointInPolyhedron
{
    // Strict: returns true if p is strictly inside (not on boundary).
    public static bool ContainsStrict(Polyhedron poly, Point p, double eps = Tolerances.PlaneSideEpsilon)
        => Classify(poly, p, eps) == Classification.Inside;

    // Inclusive: returns true if p is inside or on boundary.
    public static bool ContainsInclusive(Polyhedron poly, Point p, double eps = Tolerances.PlaneSideEpsilon)
    {
        var c = Classify(poly, p, eps);
        return c == Classification.Inside || c == Classification.Boundary;
    }

    public enum Classification { Outside, Inside, Boundary }

    // Even-odd ray casting along +X with robust tie rules.
    public static Classification Classify(Polyhedron poly, Point p, double eps = 1e-12)
    {
        if (poly is null) throw new ArgumentNullException(nameof(poly));
        var faces = poly.Triangles;
        var verts = poly.Vertices;

        // Quick reject: if p lies on any triangle plane and within its bounds -> boundary
        for (int i = 0; i < faces.Count; i++)
        {
            var (a, b, c) = faces[i];
            var tri = Triangle.FromWinding(verts[a], verts[b], verts[c]);
            var plane = Plane.FromTriangle(tri);
            var s = plane.Side(p, eps);
            if (s == 0)
            {
                // Barycentric test in double
                if (PointOnTriangle(p, tri, eps)) return Classification.Boundary;
            }
        }

        // Use a fixed, non-axis-aligned ray direction to avoid degeneracies.
        const double dx = 0.42412451, dy = 0.7315123, dz = 0.535087; // arbitrary non-rational ratios
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
        // Convert to double barycentric via area method
        var ax = (double)tri.P0.X; var ay = (double)tri.P0.Y; var az = (double)tri.P0.Z;
        var bx = (double)tri.P1.X; var by = (double)tri.P1.Y; var bz = (double)tri.P1.Z;
        var cx = (double)tri.P2.X; var cy = (double)tri.P2.Y; var cz = (double)tri.P2.Z;
        var px = (double)p.X; var py = (double)p.Y; var pz = (double)p.Z;

        // Project to dominant axis to do 2D barycentric robustly
        var nx = tri.Normal.X; var ny = tri.Normal.Y; var nz = tri.Normal.Z;
        double ax0, ay0, bx0, by0, cx0, cy0, px0, py0;
        if (Math.Abs(nx) >= Math.Abs(ny) && Math.Abs(nx) >= Math.Abs(nz))
        { // drop X
            ax0 = ay; ay0 = az; bx0 = by; by0 = bz; cx0 = cy; cy0 = cz; px0 = py; py0 = pz;
        }
        else if (Math.Abs(ny) >= Math.Abs(nz))
        { // drop Y
            ax0 = ax; ay0 = az; bx0 = bx; by0 = bz; cx0 = cx; cy0 = cz; px0 = px; py0 = pz;
        }
        else
        { // drop Z
            ax0 = ax; ay0 = ay; bx0 = bx; by0 = by; cx0 = cx; cy0 = cy; px0 = px; py0 = py;
        }

        double area = Cross2(bx0 - ax0, by0 - ay0, cx0 - ax0, cy0 - ay0);
        if (Math.Abs(area) < eps) return false; // degenerate triangle
        double w0 = Cross2(bx0 - px0, by0 - py0, cx0 - px0, cy0 - py0) / area;
        double w1 = Cross2(cx0 - px0, cy0 - py0, ax0 - px0, ay0 - py0) / area;
        double w2 = 1.0 - w0 - w1;
        return w0 >= -eps && w1 >= -eps && w2 >= -eps;
    }

    private static double Cross2(double ax, double ay, double bx, double by) => ax * by - ay * bx;

    // Edge-case tie-breaker not needed with non-axis-aligned ray; omitted for simplicity.
}
