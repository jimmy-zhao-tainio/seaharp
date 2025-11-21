namespace Geometry;

// Centralized numerical tolerances for double-based operations.
public static class Tolerances
{
    // Epsilon for plane-side tests and related dot evaluations.
    public const double PlaneSideEpsilon = 1e-12;

    // Epsilon for triangle intersection / projection predicates
    // (2D barycentric checks, segment intersection, collinearity, uniqueness).
    public const double TrianglePredicateEpsilon = 1e-12;

    // Feature-level tolerances for intersection geometry built on top of
    // the predicate layer. These are initially derived from the predicate
    // epsilon but can be tuned independently if needed.

    // Used when merging points in world space (3D and projected 2D) for
    // feature construction.
    public const double FeatureWorldDistanceEpsilonSquared =
        TrianglePredicateEpsilon * TrianglePredicateEpsilon;

    // Used when comparing barycentric coordinates (U, V, W) on triangles
    // when deduplicating feature-layer vertices.
    public const double FeatureBarycentricEpsilon = TrianglePredicateEpsilon;
}
