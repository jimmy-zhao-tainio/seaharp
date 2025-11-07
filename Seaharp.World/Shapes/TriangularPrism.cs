using System;

namespace Seaharp.World;

// Regular triangular prism aligned to Z axis. Base is an equilateral triangle in Z = -height/2 plane,
// extruded to Z = +height/2.
public sealed class TriangularPrism : Shape
{
    public TriangularPrism(long edge, long height, Seaharp.Geometry.Point? center = null,
                           double xTiltDeg = 0, double yTiltDeg = 0, double zSpinDeg = 0)
    {
        if (edge <= 0) throw new ArgumentOutOfRangeException(nameof(edge));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Center = center ?? new Seaharp.Geometry.Point(0, 0, 0);
        Edge = edge;
        HalfHeight = Math.Max(1, height / 2);

        Build(xTiltDeg, yTiltDeg, zSpinDeg);
    }

    public long Edge { get; }
    public long HalfHeight { get; }
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

        long z0 = -HalfHeight;
        long z1 = +HalfHeight;

        // Equilateral triangle circumradius R = edge / sqrt(3)
        double Rcirc = Edge / Math.Sqrt(3.0);
        double a0 = Math.PI / 2.0; // oriented upright
        double[] ang = new double[] { a0, a0 + 2.0 * Math.PI / 3.0, a0 + 4.0 * Math.PI / 3.0 };

        var b = new (long x, long y) [3];
        for (int i = 0; i < 3; i++)
        {
            b[i].x = (long)Math.Round(Rcirc * Math.Cos(ang[i]));
            b[i].y = (long)Math.Round(Rcirc * Math.Sin(ang[i]));
        }

        var B0 = R(b[0].x, b[0].y, z0); var B1 = R(b[1].x, b[1].y, z0); var B2 = R(b[2].x, b[2].y, z0);
        var T0 = R(b[0].x, b[0].y, z1); var T1 = R(b[1].x, b[1].y, z1); var T2 = R(b[2].x, b[2].y, z1);

        var pB0 = new Seaharp.Geometry.Point(B0.X, B0.Y, B0.Z);
        var pB1 = new Seaharp.Geometry.Point(B1.X, B1.Y, B1.Z);
        var pB2 = new Seaharp.Geometry.Point(B2.X, B2.Y, B2.Z);
        var pT0 = new Seaharp.Geometry.Point(T0.X, T0.Y, T0.Z);
        var pT1 = new Seaharp.Geometry.Point(T1.X, T1.Y, T1.Z);
        var pT2 = new Seaharp.Geometry.Point(T2.X, T2.Y, T2.Z);

        // Decompose triangular prism into 3 tetrahedra
        AddPrismAsTets(pB0, pB1, pB2, pT0, pT1, pT2);
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

