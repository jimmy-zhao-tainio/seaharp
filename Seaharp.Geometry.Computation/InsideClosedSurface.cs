namespace Seaharp.Geometry.Computation;

public static class InsideClosedSurface
{
    public enum Classification { Outside, Inside, Boundary }

    public static bool ContainsStrict(IReadOnlyList<Triangle> triangles, Point point)
        => Classify(triangles, point) == Classification.Inside;

    public static bool ContainsInclusive(IReadOnlyList<Triangle> triangles, Point point)
    {
        var c = Classify(triangles, point);
        return c == Classification.Inside || c == Classification.Boundary;
    }

    public static Classification Classify(IReadOnlyList<Triangle> triangles, Point point)
    {
        if (triangles is null) throw new ArgumentNullException(nameof(triangles));

        // Quick boundary check
        for (int i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i];
            var plane = Plane.FromTriangle(triangle);
            var side = plane.Side(point, Tolerances.PlaneSideEpsilon);
            if (side == 0 && PointOnTriangle(point, triangle)) return Classification.Boundary;
        }

        // Non-axis-aligned ray casting to avoid degeneracies
        const double dx = 0.42412451, dy = 0.7315123, dz = 0.535087;
        var ray = new Ray(point.X + Tolerances.PlaneSideEpsilon * 3,
                          point.Y + Tolerances.PlaneSideEpsilon * 5,
                          point.Z + Tolerances.PlaneSideEpsilon * 7,
                          dx, dy, dz);
        int hits = 0;
        for (int i = 0; i < triangles.Count; i++)
        {
            var triangle = triangles[i];
            if (Intersections.RayTriangle(ray, triangle, out var t, out var u, out var v)) hits++;
        }
        return (hits & 1) == 1 ? Classification.Inside : Classification.Outside;
    }

    private static bool PointOnTriangle(in Point point, in Triangle triangle)
    {
        var ax = (double)triangle.P0.X; var ay = (double)triangle.P0.Y; var az = (double)triangle.P0.Z;
        var bx = (double)triangle.P1.X; var by = (double)triangle.P1.Y; var bz = (double)triangle.P1.Z;
        var cx = (double)triangle.P2.X; var cy = (double)triangle.P2.Y; var cz = (double)triangle.P2.Z;
        var px = (double)point.X; var py = (double)point.Y; var pz = (double)point.Z;

        var nx = triangle.Normal.X; var ny = triangle.Normal.Y; var nz = triangle.Normal.Z;
        double ax0, ay0, bx0, by0, cx0, cy0, px0, py0;
        
        if (Math.Abs(nx) >= Math.Abs(ny) && Math.Abs(nx) >= Math.Abs(nz))
        { 
            ax0 = ay; ay0 = az; bx0 = by; by0 = bz; cx0 = cy; cy0 = cz; px0 = py; py0 = pz; 
        }
        else if (Math.Abs(ny) >= Math.Abs(nz))
        { 
            ax0 = ax; ay0 = az; bx0 = bx; by0 = bz; cx0 = cx; cy0 = cz; px0 = px; py0 = pz; 
        }
        else
        { 
            ax0 = ax; ay0 = ay; bx0 = bx; by0 = by; cx0 = cx; cy0 = cy; px0 = px; py0 = py; 
        }

        double area = Cross2(bx0 - ax0, by0 - ay0, cx0 - ax0, cy0 - ay0);
        if (Math.Abs(area) < Tolerances.PlaneSideEpsilon) return false;
        double w0 = Cross2(bx0 - px0, by0 - py0, cx0 - px0, cy0 - py0) / area;
        double w1 = Cross2(cx0 - px0, cy0 - py0, ax0 - px0, ay0 - py0) / area;
        double w2 = 1.0 - w0 - w1;
        double eps = Tolerances.PlaneSideEpsilon;
        return w0 >= -eps && w1 >= -eps && w2 >= -eps;
    }

    private static double Cross2(double ax, double ay, double bx, double by) => ax * by - ay * bx;
}