namespace Geometry;

// Barycentric coordinates (U, V, W) for points on a triangle.
// Used only for parametrization; robust predicates and intersection
// classification remain in the integer grid layer.
public readonly struct Barycentric
{
    public readonly double U;
    public readonly double V;
    public readonly double W;

    public Barycentric(double u, double v, double w)
    {
        U = u;
        V = v;
        W = w;
    }

    // Inclusive test for being inside or on the boundary of the
    // reference triangle, with a small tolerance on the barycentric
    // constraints U, V, W >= 0 and U + V + W == 1.
    public bool IsInsideInclusive()
    {
        const double epsilon = Tolerances.BarycentricInsideEpsilon;

        if (U < -epsilon || V < -epsilon || W < -epsilon)
            return false;

        var sum = U + V + W;
        return System.Math.Abs(sum - 1.0) <= epsilon;
    }

    // Component-wise closeness between two barycentric coordinates using
    // the shared feature-layer tolerance from Geometry.Tolerances.
    public bool IsCloseTo(in Barycentric other)
    {
        double epsilon = Tolerances.FeatureBarycentricEpsilon;
        return System.Math.Abs(U - other.U) <= epsilon &&
               System.Math.Abs(V - other.V) <= epsilon &&
               System.Math.Abs(W - other.W) <= epsilon;
    }
}
