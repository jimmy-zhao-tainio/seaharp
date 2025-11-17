namespace Geometry;

// Centralized numerical tolerances for double-based operations.
public static class Tolerances
{
    // Epsilon for plane-side tests and related dot evaluations.
    public const double PlaneSideEpsilon = 1e-12;

    // Epsilon for triangle intersection / projection predicates
    // (2D barycentric checks, segment intersection, collinearity, uniqueness).
    public const double TrianglePredicateEpsilon = 1e-12;
}
