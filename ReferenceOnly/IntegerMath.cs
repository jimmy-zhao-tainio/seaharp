using System;

namespace Seaharp.Geometry;

public static class IntegerMath
{
    public static Int128 SignedTetrahedronVolume6(in Point p, in Point q, in Point r, in Point s)
    {
        var pq = Int128Vector.FromPoints(p, q);
        var pr = Int128Vector.FromPoints(p, r);
        var ps = Int128Vector.FromPoints(p, s);

        var cross = Int128Vector.Cross(pq, pr);
        return Int128Vector.Dot(cross, ps);
    }

    public static Int128 AbsoluteTetrahedronVolume6(in Point a, in Point b, in Point c, in Point d) =>
        Int128.Abs(SignedTetrahedronVolume6(a, b, c, d));
}
