using System;

namespace Seaharp.World;

// Solid right circular cylinder aligned to Z axis, polygonized with N segments.
// Constructed as a ring of triangular prisms between base and top discs.
public sealed class SolidCylinder : Shape
{
    public SolidCylinder(long radius, long height, Seaharp.Geometry.Point? center = null,
                         int? segments = null, double xTiltDeg = 0, double yTiltDeg = 0, double zSpinDeg = 0)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        Radius = radius;
        HalfHeight = Math.Max(1, height / 2);
        Segments = Math.Max(3, segments ?? ChooseSegments(radius));

        Build(xTiltDeg, yTiltDeg, zSpinDeg);
    }

    public long Radius { get; }
    public long HalfHeight { get; }
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
        long z0 = -HalfHeight;
        long z1 = +HalfHeight;

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

        var bc = R(0, 0, z0); // base center
        var tc = R(0, 0, z1); // top center
        var BC = new Seaharp.Geometry.Point(bc.X, bc.Y, bc.Z);
        var TC = new Seaharp.Geometry.Point(tc.X, tc.Y, tc.Z);

        for (int i = 0; i < Segments; i++)
        {
            double a0 = (2.0 * Math.PI * i) / Segments;
            double a1 = (2.0 * Math.PI * ((i + 1) % Segments)) / Segments;

            long b0x = (long)Math.Round(Radius * Math.Cos(a0));
            long b0y = (long)Math.Round(Radius * Math.Sin(a0));
            long b1x = (long)Math.Round(Radius * Math.Cos(a1));
            long b1y = (long)Math.Round(Radius * Math.Sin(a1));

            var b0 = R(b0x, b0y, z0);
            var b1 = R(b1x, b1y, z0);
            var t0 = R(b0x, b0y, z1);
            var t1 = R(b1x, b1y, z1);

            var pb0 = new Seaharp.Geometry.Point(b0.X, b0.Y, b0.Z);
            var pb1 = new Seaharp.Geometry.Point(b1.X, b1.Y, b1.Z);
            var pt0 = new Seaharp.Geometry.Point(t0.X, t0.Y, t0.Z);
            var pt1 = new Seaharp.Geometry.Point(t1.X, t1.Y, t1.Z);

            // Triangular prism between (BC, b0, b1) and (TC, t0, t1)
            AddPrismAsTets(BC, pb0, pb1, TC, pt0, pt1);
        }
    }

    private void AddPrismAsTets(in Seaharp.Geometry.Point b0,
                                in Seaharp.Geometry.Point b1,
                                in Seaharp.Geometry.Point b2,
                                in Seaharp.Geometry.Point t0,
                                in Seaharp.Geometry.Point t1,
                                in Seaharp.Geometry.Point t2)
    {
        TryAdd(b0, b1, b2, t2);
        TryAdd(b0, b1, t1, t2);
        TryAdd(b0, t0, t1, t2);
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

