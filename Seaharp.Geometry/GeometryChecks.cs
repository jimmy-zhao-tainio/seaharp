using System;
using System.Collections.Generic;

namespace Seaharp.Geometry;

internal static class GeometryChecks
{
    private static readonly Int128 Zero = default;

    public static bool ArePointsOnSameLine(in Point firstPoint, in Point secondPoint, in Point thirdPoint)
    {
        var firstVector = Int128Vector.FromPoints(firstPoint, secondPoint);
        var secondVector = Int128Vector.FromPoints(firstPoint, thirdPoint);
        return Int128Vector.Cross(firstVector, secondVector).IsZero;
    }

    public static bool IsPointOnLine(in Point lineStart, in Point lineEnd, in Point point)
    {
        if (!ArePointsOnSameLine(lineStart, lineEnd, point))
        {
            return false;
        }

        var fromStart = Int128Vector.FromPoints(lineStart, point);
        var fromEnd = Int128Vector.FromPoints(lineEnd, point);
        return Int128Vector.Dot(fromStart, fromEnd) <= 0;
    }

    public static bool DoesLineIntersectTriangle(
        in Point firstPoint,
        in Point secondPoint,
        in Triangle triangle)
    {
        var orientAtStart = TrianglePlaneOffset(triangle, firstPoint);
        var orientAtEnd = TrianglePlaneOffset(triangle, secondPoint);

        if (orientAtStart == Zero && orientAtEnd == Zero)
        {
            if (IsPointOnTriangle(firstPoint, triangle))
            {
                return true;
            }

            if (IsPointOnTriangle(secondPoint, triangle))
            {
                return true;
            }

            return false;
        }

        if ((orientAtStart > Zero && orientAtEnd > Zero) || (orientAtStart < Zero && orientAtEnd < Zero))
        {
            return false;
        }

        var orientOppositeAB = IntegerMath.SignedTetrahedronVolume6(firstPoint, secondPoint, triangle.A, triangle.B);
        var orientOppositeBC = IntegerMath.SignedTetrahedronVolume6(firstPoint, secondPoint, triangle.B, triangle.C);
        var orientOppositeCA = IntegerMath.SignedTetrahedronVolume6(firstPoint, secondPoint, triangle.C, triangle.A);

        if (orientOppositeAB == Zero || orientOppositeBC == Zero || orientOppositeCA == Zero)
        {
            return true;
        }

        var positive = 0;
        var negative = 0;

        CountSign(orientOppositeAB, ref positive, ref negative);
        CountSign(orientOppositeBC, ref positive, ref negative);
        CountSign(orientOppositeCA, ref positive, ref negative);

        return positive == 3 || negative == 3;
    }

    public static bool DoesTriangleIntersectTriangle(in Triangle first, in Triangle second)
    {
        if (DoesLineIntersectTriangle(first.A, first.B, second)) return true;
        if (DoesLineIntersectTriangle(first.B, first.C, second)) return true;
        if (DoesLineIntersectTriangle(first.C, first.A, second)) return true;

        if (DoesLineIntersectTriangle(second.A, second.B, first)) return true;
        if (DoesLineIntersectTriangle(second.B, second.C, first)) return true;
        if (DoesLineIntersectTriangle(second.C, second.A, first)) return true;

        return false;
    }

    public static bool ArePointsStrictlyOnSameSideOfTrianglePlane(
        in Triangle triangle,
        in Point firstPoint,
        in Point secondPoint,
        in Point thirdPoint)
    {
        var orientationFirst = TrianglePlaneOffset(triangle, firstPoint);
        if (orientationFirst == Zero)
        {
            return false;
        }

        var referenceIsPositive = orientationFirst > Zero;

        var orientationSecond = TrianglePlaneOffset(triangle, secondPoint);
        if (orientationSecond == Zero || (orientationSecond > 0) != referenceIsPositive)
        {
            return false;
        }

        var orientationThird = TrianglePlaneOffset(triangle, thirdPoint);
        if (orientationThird == Zero || (orientationThird > 0) != referenceIsPositive)
        {
            return false;
        }

        return true;
    }

    private static void CountSign(Int128 value, ref int positive, ref int negative)
    {
        if (value > Zero)
        {
            positive++;
        }
        else if (value < Zero)
        {
            negative++;
        }
    }

    private static bool IsPointOnTriangle(in Point point, in Triangle triangle)
    {
        if (!IsPointOnLine(triangle.A, triangle.B, point) &&
            !IsPointOnLine(triangle.B, triangle.C, point) &&
            !IsPointOnLine(triangle.C, triangle.A, point))
        {
            return false;
        }

        return true;
    }

    public static bool IsPointOnTrianglePlane(in Triangle triangle, in Point point) =>
        TrianglePlaneOffset(triangle, point) == Zero;

    public static bool IsPointOnPositiveSideOfTrianglePlane(in Triangle triangle, in Point point) =>
        TrianglePlaneOffset(triangle, point) > Zero;

    public static bool IsPointOnNegativeSideOfTrianglePlane(in Triangle triangle, in Point point) =>
        TrianglePlaneOffset(triangle, point) < Zero;

    private static Int128 TrianglePlaneOffset(in Triangle triangle, in Point point) =>
        IntegerMath.SignedTetrahedronVolume6(triangle.A, triangle.B, triangle.C, point);

    public static bool AreTrianglesIdentical(in Triangle first, in Triangle second) =>
        TriangleKey.From(first) == TriangleKey.From(second);

    public static bool DoTrianglesShareVertex(in Triangle first, in Triangle second)
    {
        var vertices = new HashSet<Point>(first.Vertices);
        foreach (var vertex in second.Vertices)
        {
            if (vertices.Contains(vertex))
            {
                return true;
            }
        }
        return false;
    }
}
