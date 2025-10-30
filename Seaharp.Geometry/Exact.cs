using System;

namespace Seaharp.Geometry;

/// <summary>
/// Provides exact integer-based geometric predicates for grid-aligned geometry.
/// </summary>
public static class Exact
{
    /// <summary>
    /// Computes the signed six times volume of the tetrahedron defined by four points.
    /// </summary>
    public static Int128 Orient3D(in GridPoint p, in GridPoint q, in GridPoint r, in GridPoint s) =>
        ComputeOrient3D(p, q, r, s);

    /// <summary>
    /// Determines whether three points are collinear.
    /// </summary>
    public static bool Collinear(in GridPoint p, in GridPoint q, in GridPoint r)
    {
        checked
        {
            var ux = Diff(q.X, p.X);
            var uy = Diff(q.Y, p.Y);
            var uz = Diff(q.Z, p.Z);

            var vx = Diff(r.X, p.X);
            var vy = Diff(r.Y, p.Y);
            var vz = Diff(r.Z, p.Z);

            var cx = uy * vz - uz * vy;
            var cy = uz * vx - ux * vz;
            var cz = ux * vy - uy * vx;

            return cx == 0 && cy == 0 && cz == 0;
        }
    }

    /// <summary>
    /// Determines whether a point lies on the line segment formed by two other points.
    /// </summary>
    public static bool OnSegment(in GridPoint a, in GridPoint b, in GridPoint x)
    {
        if (!Collinear(a, b, x))
        {
            return false;
        }

        checked
        {
            var ax = Diff(x.X, a.X);
            var ay = Diff(x.Y, a.Y);
            var az = Diff(x.Z, a.Z);

            var bx = Diff(x.X, b.X);
            var by = Diff(x.Y, b.Y);
            var bz = Diff(x.Z, b.Z);

            var dot = ax * bx + ay * by + az * bz;

            return dot <= 0;
        }
    }

    /// <summary>
    /// Computes the absolute value of six times the volume of the tetrahedron defined by four points.
    /// </summary>
    public static Int128 AbsVol6(in GridPoint a, in GridPoint b, in GridPoint c, in GridPoint d) =>
        Int128.Abs(ComputeOrient3D(a, b, c, d));

    private static Int128 ComputeOrient3D(in GridPoint p, in GridPoint q, in GridPoint r, in GridPoint s)
    {
        checked
        {
            var qx = Diff(q.X, p.X);
            var qy = Diff(q.Y, p.Y);
            var qz = Diff(q.Z, p.Z);

            var rx = Diff(r.X, p.X);
            var ry = Diff(r.Y, p.Y);
            var rz = Diff(r.Z, p.Z);

            var sx = Diff(s.X, p.X);
            var sy = Diff(s.Y, p.Y);
            var sz = Diff(s.Z, p.Z);

            var cx = qy * rz - qz * ry;
            var cy = qz * rx - qx * rz;
            var cz = qx * ry - qy * rx;

            return cx * sx + cy * sy + cz * sz;
        }
    }

    private static Int128 Diff(long a, long b) => (Int128)a - (Int128)b;
}
