using System;

namespace Seaharp.Geometry;

public static class Intersections
{
    // Möller–Trumbore ray-triangle intersection in double with epsilon.
    // Returns true if ray intersects front or back face; t = distance along ray (>=0).
    // Excludes very grazing hits using epsilon to stabilize even-odd counts.
    public static bool RayTriangle(in RayD ray, in Triangle tri, double eps, out double t, out double u, out double v)
    {
        var p0x = (double)tri.P0.X; var p0y = (double)tri.P0.Y; var p0z = (double)tri.P0.Z;
        var e1x = (double)tri.P1.X - p0x; var e1y = (double)tri.P1.Y - p0y; var e1z = (double)tri.P1.Z - p0z;
        var e2x = (double)tri.P2.X - p0x; var e2y = (double)tri.P2.Y - p0y; var e2z = (double)tri.P2.Z - p0z;

        // pvec = D x e2
        var pvx = ray.Dy * e2z - ray.Dz * e2y;
        var pvy = ray.Dz * e2x - ray.Dx * e2z;
        var pvz = ray.Dx * e2y - ray.Dy * e2x;

        // det = e1 . pvec
        var det = e1x * pvx + e1y * pvy + e1z * pvz;
        if (det > -eps && det < eps) { t = u = v = 0; return false; } // parallel / nearly parallel

        var invDet = 1.0 / det;
        // tvec = O - P0
        var tvx = ray.Ox - p0x; var tvy = ray.Oy - p0y; var tvz = ray.Oz - p0z;
        u = (tvx * pvx + tvy * pvy + tvz * pvz) * invDet;
        if (u < -eps || u > 1.0 + eps) { t = v = 0; return false; }

        // qvec = tvec x e1
        var qvx = tvy * e1z - tvz * e1y;
        var qvy = tvz * e1x - tvx * e1z;
        var qvz = tvx * e1y - tvy * e1x;
        v = (ray.Dx * qvx + ray.Dy * qvy + ray.Dz * qvz) * invDet;
        if (v < -eps || u + v > 1.0 + eps) { t = 0; return false; }

        t = (e2x * qvx + e2y * qvy + e2z * qvz) * invDet;
        if (t < 0) return false; // opposite direction

        return true;
    }
}

