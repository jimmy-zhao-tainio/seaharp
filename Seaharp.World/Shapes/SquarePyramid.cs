using System;

namespace Seaharp.World;

// Right square pyramid aligned to Z axis, base in Z=0 plane, apex at +height.
public sealed class SquarePyramid : Shape
{
    public SquarePyramid(long baseEdge, long height, Seaharp.Geometry.Point? center = null,
                         double xTiltDeg = 0, double yTiltDeg = 0, double zSpinDeg = 0)
    {
        if (baseEdge <= 0) throw new ArgumentOutOfRangeException(nameof(baseEdge));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        BaseHalf = baseEdge / 2;
        Height = height;

        Build(xTiltDeg, yTiltDeg, zSpinDeg);
    }

    public long BaseHalf { get; }
    public long Height { get; }
    public Seaharp.Geometry.Point Center { get; }

    private void Build(double xTiltDeg, double yTiltDeg, double zSpinDeg)
    {
        // Rotation X->Y->Z
        double rx = xTiltDeg * Math.PI / 180.0;
        double ry = yTiltDeg * Math.PI / 180.0;
        double rz = zSpinDeg * Math.PI / 180.0;
        double cxr = Math.Cos(rx), sxr = Math.Sin(rx);
        double cyr = Math.Cos(ry), syr = Math.Sin(ry);
        double czr = Math.Cos(rz), szr = Math.Sin(rz);

        (long X, long Y, long Z) R(long x, long y, long z)
        {
            double xd = x, yd = y, zd = z;
            double y1 = yd * cxr - zd * sxr; double z1 = yd * sxr + zd * cxr; double x1 = xd;
            double x2 = x1 * cyr + z1 * syr; double z2 = -x1 * syr + z1 * cyr; double y2 = y1;
            double x3 = x2 * czr - y2 * szr; double y3 = x2 * szr + y2 * czr; double z3 = z2;
            return ((long)Math.Round(Center.X + x3), (long)Math.Round(Center.Y + y3), (long)Math.Round(Center.Z + z3));
        }

        long h = Height;
        long a = BaseHalf;

        var p0 = R(-a, -a, 0);
        var p1 = R( a, -a, 0);
        var p2 = R( a,  a, 0);
        var p3 = R(-a,  a, 0);
        var apex = R(0, 0, h);

        var P0 = new Seaharp.Geometry.Point(p0.X, p0.Y, p0.Z);
        var P1 = new Seaharp.Geometry.Point(p1.X, p1.Y, p1.Z);
        var P2 = new Seaharp.Geometry.Point(p2.X, p2.Y, p2.Z);
        var P3 = new Seaharp.Geometry.Point(p3.X, p3.Y, p3.Z);
        var A  = new Seaharp.Geometry.Point(apex.X, apex.Y, apex.Z);

        // Decompose via two base triangles
        TryAdd(A, P0, P1, P2);
        TryAdd(A, P0, P2, P3);
    }

    private void TryAdd(in Seaharp.Geometry.Point a,
                        in Seaharp.Geometry.Point b,
                        in Seaharp.Geometry.Point c,
                        in Seaharp.Geometry.Point d)
    {
        try { tetrahedrons.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d)); }
        catch (InvalidOperationException) { }
    }
}

