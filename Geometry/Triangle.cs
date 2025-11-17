namespace Geometry;

// Standalone triangle with outward unit normal in grid space.
// Mirrors the former nested Tetrahedron.Triangle semantics.
public readonly struct Triangle
{
    public readonly Point P0;
    public readonly Point P1;
    public readonly Point P2;
    public readonly Normal Normal; // outward, unit length

    // Construct a triangle from three points and a 'missing' point used to orient the normal outward
    // (away from the missing point), identical to prior Tetrahedron.Triangle behavior.
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
}