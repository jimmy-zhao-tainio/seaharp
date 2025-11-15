namespace Seaharp.Geometry.Computation;

public static class TrianglePredicates
{
    public static bool IsSame(
        in Triangle a,
        in Triangle b)
    {
        int found = 0;
        var a0 = a.P0; var a1 = a.P1; var a2 = a.P2;

        var b0 = b.P0;
        if (a0.Equals(b0) || a1.Equals(b0) || a2.Equals(b0)) found++;

        var b1 = b.P1;
        if (a0.Equals(b1) || a1.Equals(b1) || a2.Equals(b1)) found++;

        var b2 = b.P2;
        if (a0.Equals(b2) || a1.Equals(b2) || a2.Equals(b2)) found++;

        return found == 3;
    }
}