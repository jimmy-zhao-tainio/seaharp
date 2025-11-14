using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.Geometry.Computation;

public static class InsideClosedSurface
{
    public enum Classification { Outside, Inside, Boundary }

    public static bool ContainsStrict(IReadOnlyList<Triangle> triangles, Point p, double epsilon = Tolerances.PlaneSideEpsilon)
        => Classify(triangles, p, epsilon) == Classification.Inside;

    public static bool ContainsInclusive(IReadOnlyList<Triangle> triangles, Point p, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        var c = Classify(triangles, p, epsilon);
        return c == Classification.Inside || c == Classification.Boundary;
    }

    public static Classification Classify(IReadOnlyList<Triangle> triangles, Point p, double epsilon = 1e-12)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));

        // Quick boundary check
        for (int i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i];
            var plane = Plane.FromTriangle(triangle);
            var s = plane.Side(p, epsilon);
            if (s == 0 && PointOnTriangle(p, triangle, epsilon)) return Classification.Boundary;
        }

        // Non-axis-aligned ray casting to avoid degeneracies
        const double dx = 0.42412451, dy = 0.7315123, dz = 0.535087;
        var ray = new Ray(p.X + epsilon * 3, p.Y + epsilon * 5, p.Z + epsilon * 7, dx, dy, dz);
        int hits = 0;
        for (int i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i];
            if (Intersections.RayTriangle(ray, triangle, epsilon, out var t, out var u, out var v)) hits++;
        }
        return (hits & 1) == 1 ? Classification.Inside : Classification.Outside;
    }

    private static bool PointOnTriangle(in Point p, in Triangle triangle, double epsilon)
    {
        var ax = (double)triangle.P0.X; var ay = (double)triangle.P0.Y; var az = (double)triangle.P0.Z;
        var bx = (double)triangle.P1.X; var by = (double)triangle.P1.Y; var bz = (double)triangle.P1.Z;
        var cx = (double)triangle.P2.X; var cy = (double)triangle.P2.Y; var cz = (double)triangle.P2.Z;
        var px = (double)p.X; var py = (double)p.Y; var pz = (double)p.Z;

        var nx = triangle.Normal.X; var ny = triangle.Normal.Y; var nz = triangle.Normal.Z;
        double ax0, ay0, bx0, by0, cx0, cy0, px0, py0;
        if (Math.Abs(nx) >= Math.Abs(ny) && Math.Abs(nx) >= Math.Abs(nz))
        { ax0 = ay; ay0 = az; bx0 = by; by0 = bz; cx0 = cy; cy0 = cz; px0 = py; py0 = pz; }
        else if (Math.Abs(ny) >= Math.Abs(nz))
        { ax0 = ax; ay0 = az; bx0 = bx; by0 = bz; cx0 = cx; cy0 = cz; px0 = px; py0 = pz; }
        else
        { ax0 = ax; ay0 = ay; bx0 = bx; by0 = by; cx0 = cx; cy0 = cy; px0 = px; py0 = py; }

        double area = Cross2(bx0 - ax0, by0 - ay0, cx0 - ax0, cy0 - ay0);
        if (Math.Abs(area) < epsilon) return false;
        double w0 = Cross2(bx0 - px0, by0 - py0, cx0 - px0, cy0 - py0) / area;
        double w1 = Cross2(cx0 - px0, cy0 - py0, ax0 - px0, ay0 - py0) / area;
        double w2 = 1.0 - w0 - w1;
        return w0 >= -epsilon && w1 >= -epsilon && w2 >= -epsilon;
    }

    private static double Cross2(double ax, double ay, double bx, double by) => ax * by - ay * bx;
}

