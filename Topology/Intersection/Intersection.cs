using Geometry;
using Geometry.Predicates;

public static class Intersection
{
    public static IntersectionType Classify(in Triangle a, in Triangle b)
    {
        // First: coplanar vs non-coplanar.
        if (TrianglePredicates.IsCoplanar(in a, in b))
        {
            // Coplanar: area → segment → point → none.
            if (TrianglePredicates.HasAreaIntersectionCoplanar(in a, in b))
                return IntersectionType.Area;
            if (TrianglePredicates.HasSegmentIntersectionCoplanar(in a, in b))
                return IntersectionType.Segment;
            if (TrianglePredicates.HasPointIntersectionCoplanar(in a, in b))
                return IntersectionType.Point;
            return IntersectionType.None;
        }
        else
        {
            // Non-coplanar: only segment or point (no area possible).
            if (TrianglePredicates.HasSegmentIntersectionNonCoplanar(in a, in b))
                return IntersectionType.Segment;
            if (TrianglePredicates.HasPointIntersectionNonCoplanar(in a, in b))
                return IntersectionType.Point;
            return IntersectionType.None;
        }
    }

    public static bool Any(in Triangle a, in Triangle b)
    {
        return Classify(in a, in b) != IntersectionType.None;
    }

    public static bool HasAreaIntersection(in Triangle a, in Triangle b)
        => Classify(in a, in b) == IntersectionType.Area;

    public static bool HasSegmentIntersection(in Triangle a, in Triangle b)
    {
        var type = Classify(in a, in b);
        return type == IntersectionType.Segment
            || type == IntersectionType.Area;
    }

    public static bool HasPointIntersection(in Triangle a, in Triangle b)
    {
        var type = Classify(in a, in b);
        return type == IntersectionType.Point
            || type == IntersectionType.Segment
            || type == IntersectionType.Area;
    }

    public static bool HasAreaIntersectionCoplanar(in Triangle a, in Triangle b)
    {
        if (!TrianglePredicates.IsCoplanar(in a, in b))
            return false;

        return Classify(in a, in b) == IntersectionType.Area;
    }

    public static bool HasSegmentIntersectionCoplanar(in Triangle a, in Triangle b)
    {
        if (!TrianglePredicates.IsCoplanar(in a, in b))
            return false;

        var type = Classify(in a, in b);
        return type == IntersectionType.Segment
            || type == IntersectionType.Area;
    }

    public static bool HasPointIntersectionCoplanar(in Triangle a, in Triangle b)
    {
        if (!TrianglePredicates.IsCoplanar(in a, in b))
            return false;

        var type = Classify(in a, in b);
        return type == IntersectionType.Point
            || type == IntersectionType.Segment
            || type == IntersectionType.Area;
    }

    public static bool HasSegmentIntersectionNonCoplanar(in Triangle a, in Triangle b)
    {
        if (TrianglePredicates.IsCoplanar(in a, in b))
            return false;

        return Classify(in a, in b) == IntersectionType.Segment;
    }

    public static bool HasPointIntersectionNonCoplanar(in Triangle a, in Triangle b)
    {
        if (TrianglePredicates.IsCoplanar(in a, in b))
            return false;

        var type = Classify(in a, in b);
        return type == IntersectionType.Point
            || type == IntersectionType.Segment;
    }
}
