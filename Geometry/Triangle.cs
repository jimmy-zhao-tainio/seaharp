namespace Geometry;

// Standalone triangle with outward unit normal in grid space.
// Mirrors the former nested Tetrahedron.Triangle semantics.
public readonly struct Triangle
{
    public readonly Point P0;
    public readonly Point P1;
    public readonly Point P2;
    public readonly Normal Normal; // outward, unit length

    // Construct a triangle from three points and a "missing" point.
    // The missing point is NOT part of the triangle. It is a fourth vertex
    // (typically from a tetrahedron) used only to orient the triangle's normal
    // so that it points away from that missing point.
    public Triangle(Point p0, Point p1, Point p2, Point missing)
    {
        P0 = p0;
        P1 = p1;
        P2 = p2;

        var e1 = Vector128.FromPoints(p0, p1);
        var e2 = Vector128.FromPoints(p0, p2);
        var nc = Vector128.Cross(e1, e2);
        var n = new Vector((double)nc.X, (double)nc.Y, (double)nc.Z);
        var normal = Normal.FromVector(n);

        // Ensure outward: decide using exact Int128 dot to avoid floating ambiguity.
        var missDelta = Vector128.FromPoints(p0, missing);
        var sign = Vector128.Dot(nc, missDelta);
        if (sign >= 0)
        {
            normal = Normal.FromVector(n * -1.0);
        }

        Normal = normal;
    }

    // Construct from three points assuming given winding already encodes outward orientation.
    // Normal is computed directly from P0->P1 and P0->P2.
    public static Triangle FromWinding(Point p0, Point p1, Point p2)
    {
        var e1 = new Vector((double)p1.X - p0.X, (double)p1.Y - p0.Y, (double)p1.Z - p0.Z);
        var e2 = new Vector((double)p2.X - p0.X, (double)p2.Y - p0.Y, (double)p2.Z - p0.Z);
        var n = e1.Cross(e2).Normalized();
        return new Triangle(p0, p1, p2, n);
    }

    // Private ctor to set exact normal when already computed as unit vector.
    private Triangle(Point p0, Point p1, Point p2, Vector unitNormal)
    {
        P0 = p0; P1 = p1; P2 = p2; Normal = Normal.FromVector(unitNormal);
    }

    // Convert a point in the plane of this triangle to barycentric
    // coordinates (U,V,W) such that P = U*P0 + V*P1 + W*P2 and
    // U + V + W = 1. Uses double precision; intended for parametrization
    // only, not for robust geometric classification.
    public Barycentric ToBarycentric(Point point)
    {
        // Work in double to avoid repeated casting in arithmetic.
        var p0 = new Vector(P0.X, P0.Y, P0.Z);
        var p1 = new Vector(P1.X, P1.Y, P1.Z);
        var p2 = new Vector(P2.X, P2.Y, P2.Z);
        var p = new Vector(point.X, point.Y, point.Z);

        var v0 = p1 - p0;
        var v1 = p2 - p0;
        var v2 = p - p0;

        var d00 = v0.Dot(v0);
        var d01 = v0.Dot(v1);
        var d11 = v1.Dot(v1);
        var d20 = v2.Dot(v0);
        var d21 = v2.Dot(v1);

        var denom = d00 * d11 - d01 * d01;
        if (denom == 0.0)
        {
            // Degenerate triangle in this metric; return a neutral value.
            return new Barycentric(0.0, 0.0, 0.0);
        }

        var invDenom = 1.0 / denom;
        var v = (d11 * d20 - d01 * d21) * invDenom;
        var w = (d00 * d21 - d01 * d20) * invDenom;
        var u = 1.0 - v - w;
        return new Barycentric(u, v, w);
    }

    // Reconstruct a real-valued point from barycentric coordinates
    // with respect to this triangle: P = U*P0 + V*P1 + W*P2.
    // Returns RealPoint; any snapping back to integer grid should be
    // done via GridRounding.
    public RealPoint FromBarycentric(in Barycentric barycentric)
    {
        var u = barycentric.U;
        var v = barycentric.V;
        var w = barycentric.W;

        var x = u * P0.X + v * P1.X + w * P2.X;
        var y = u * P0.Y + v * P1.Y + w * P2.Y;
        var z = u * P0.Z + v * P1.Z + w * P2.Z;

        return new RealPoint(x, y, z);
    }
}
