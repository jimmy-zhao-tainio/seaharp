using System;
using System.Numerics;

#if NET8_0_OR_GREATER
using ExactScalar = System.Int128;
#else
using ExactScalar = System.Numerics.BigInteger;
#endif

namespace Seaharp.Geometry;

/// <summary>
/// Provides exact integer-based geometric predicates for grid-aligned geometry.
/// </summary>
public static class Exact
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// Computes the signed six times volume of the tetrahedron defined by four points.
    /// </summary>
    /// <param name="p">The origin point.</param>
    /// <param name="q">The first adjacent point.</param>
    /// <param name="r">The second adjacent point.</param>
    /// <param name="s">The third adjacent point.</param>
    /// <returns>The signed six times volume as an <see cref="Int128"/>.</returns>
    public static Int128 Orient3D(in GridPoint p, in GridPoint q, in GridPoint r, in GridPoint s) =>
        ComputeOrient3D(p, q, r, s);
#else
    /// <summary>
    /// Computes the signed six times volume of the tetrahedron defined by four points.
    /// </summary>
    /// <param name="p">The origin point.</param>
    /// <param name="q">The first adjacent point.</param>
    /// <param name="r">The second adjacent point.</param>
    /// <param name="s">The third adjacent point.</param>
    /// <returns>The signed six times volume as a <see cref="BigInteger"/>.</returns>
    public static BigInteger Orient3D(in GridPoint p, in GridPoint q, in GridPoint r, in GridPoint s) =>
        ComputeOrient3D(p, q, r, s);
#endif

    /// <summary>
    /// Determines whether three points are collinear.
    /// </summary>
    /// <param name="p">The first point.</param>
    /// <param name="q">The second point.</param>
    /// <param name="r">The third point.</param>
    /// <returns><see langword="true"/> if the points lie on the same line; otherwise, <see langword="false"/>.</returns>
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

            return cx == Zero && cy == Zero && cz == Zero;
        }
    }

    /// <summary>
    /// Determines whether a point lies on the line segment formed by two other points.
    /// </summary>
    /// <param name="a">The first endpoint of the segment.</param>
    /// <param name="b">The second endpoint of the segment.</param>
    /// <param name="x">The point to test.</param>
    /// <returns><see langword="true"/> if <paramref name="x"/> lies on the segment; otherwise, <see langword="false"/>.</returns>
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

            return dot <= Zero;
        }
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Computes the absolute value of six times the volume of the tetrahedron defined by four points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <param name="c">The third point.</param>
    /// <param name="d">The fourth point.</param>
    /// <returns>The non-negative six times volume as an <see cref="Int128"/>.</returns>
    public static Int128 AbsVol6(in GridPoint a, in GridPoint b, in GridPoint c, in GridPoint d) =>
        Int128.Abs(ComputeOrient3D(a, b, c, d));
#else
    /// <summary>
    /// Computes the absolute value of six times the volume of the tetrahedron defined by four points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <param name="c">The third point.</param>
    /// <param name="d">The fourth point.</param>
    /// <returns>The non-negative six times volume as a <see cref="BigInteger"/>.</returns>
    public static BigInteger AbsVol6(in GridPoint a, in GridPoint b, in GridPoint c, in GridPoint d) =>
        BigInteger.Abs(ComputeOrient3D(a, b, c, d));
#endif

    private static ExactScalar ComputeOrient3D(in GridPoint p, in GridPoint q, in GridPoint r, in GridPoint s)
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

    private static ExactScalar Diff(long a, long b) => (ExactScalar)a - (ExactScalar)b;

    private static ExactScalar Zero => default;
}
