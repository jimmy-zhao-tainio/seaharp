using System;

namespace Seaharp.World;

// Solid right circular cone aligned to Z axis. Base at Z = 0, apex at Z = +height.
// Constructed as a fan of tetrahedra: (apex, baseCenter, p_i, p_{i+1}).
public sealed class Cone : Shape
{
    public Cone(long radius, long height, Seaharp.Geometry.Point? center = null,
                int? segments = null, double xTiltDeg = 0, double yTiltDeg = 0, double zSpinDeg = 0)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        Radius = radius;
        Height = height;
        Segments = Math.Max(3, segments ?? ChooseSegments(radius));

        Build(xTiltDeg, yTiltDeg, zSpinDeg);
    }

    public long Radius { get; }
    public long Height { get; }
    public int Segments { get; }
    public Seaharp.Geometry.Point Center { get; }

    private static int ChooseSegments(long radius)
    {
        double circumference = 2.0 * Math.PI * Math.Max(1, radius);
        int n = (int)Math.Round(circumference / 30.0);
        if (n < 24) n = 24;
        if (n > 720) n = 720;
        return n;
    }

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

        var apexR = R(0, 0, Height);
        var baseCR = R(0, 0, 0);
        var A = new Seaharp.Geometry.Point(apexR.X, apexR.Y, apexR.Z);
        var BC = new Seaharp.Geometry.Point(baseCR.X, baseCR.Y, baseCR.Z);

        for (int i = 0; i < Segments; i++)
        {
            double a0 = (2.0 * Math.PI * i) / Segments;
            double a1 = (2.0 * Math.PI * ((i + 1) % Segments)) / Segments;
            long x0 = (long)Math.Round(Radius * Math.Cos(a0));
            long y0 = (long)Math.Round(Radius * Math.Sin(a0));
            long x1 = (long)Math.Round(Radius * Math.Cos(a1));
            long y1 = (long)Math.Round(Radius * Math.Sin(a1));
            var p0 = R(x0, y0, 0);
            var p1 = R(x1, y1, 0);
            var P0 = new Seaharp.Geometry.Point(p0.X, p0.Y, p0.Z);
            var P1 = new Seaharp.Geometry.Point(p1.X, p1.Y, p1.Z);

            TryAdd(A, BC, P0, P1);
        }
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

