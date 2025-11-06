using System;
using System.Collections.Generic;
using System.Numerics;

namespace Seaharp.Geometry;

internal static class GeometryChecks
{
    private static readonly Int128 Zero = default;

    // Returns true when all three points are colinear.
    public static bool ArePointsOnSameLine(in Point firstPoint, in Point secondPoint, in Point thirdPoint)
    {
        var firstVector = Int128Vector.FromPoints(firstPoint, secondPoint);
        var secondVector = Int128Vector.FromPoints(firstPoint, thirdPoint);
        return Int128Vector.Cross(firstVector, secondVector).IsZero;
    }

    // Checks whether point lies on the line between the two endpoints.
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

    // Tests whether the line between the endpoints intersects or touches the triangle.
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

    // Detects any intersection between two triangles, including touching.
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

    // Ensures each point sits strictly on the same side of the triangle plane.
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

    // Increments positive/negative counters based on orientation sign.
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

    // Determines whether a point lies somewhere on the triangle boundary.
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

    // True when the point lies on the supporting plane of the triangle.
    public static bool IsPointOnTrianglePlane(in Triangle triangle, in Point point) =>
        TrianglePlaneOffset(triangle, point) == Zero;

    // True when the point is strictly above the triangle plane.
    public static bool IsPointOnPositiveSideOfTrianglePlane(in Triangle triangle, in Point point) =>
        TrianglePlaneOffset(triangle, point) > Zero;

    // True when the point is strictly below the triangle plane.
    public static bool IsPointOnNegativeSideOfTrianglePlane(in Triangle triangle, in Point point) =>
        TrianglePlaneOffset(triangle, point) < Zero;

    // Signed offset from the triangle plane (six-times the tetrahedron volume).
    private static Int128 TrianglePlaneOffset(in Triangle triangle, in Point point) =>
        IntegerMath.SignedTetrahedronVolume6(triangle.A, triangle.B, triangle.C, point);

    // Tests if two triangles contain exactly the same vertices.
    public static bool AreTrianglesIdentical(in Triangle first, in Triangle second) =>
        TriangleKey.From(first) == TriangleKey.From(second);

    // Returns true when triangles share at least one vertex.
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

    // === Interior-aware geometry helpers ===
    // Counts the number of common vertices between two triangles.
    public static int GetSharedVertexCount(in Triangle first, in Triangle second)
    {
        var set = new HashSet<Point>(first.Vertices);
        var count = 0;
        foreach (var v in second.Vertices)
        {
            if (set.Contains(v)) count++;
        }
        return count;
    }

    // True when triangles share a full edge (two common vertices).
    public static bool DoTrianglesShareEdge(in Triangle first, in Triangle second) =>
        GetSharedVertexCount(first, second) == 2;

    // True when triangles lie on the same geometric plane.
    public static bool AreTrianglesCoplanar(in Triangle first, in Triangle second)
    {
        return TrianglePlaneOffset(first, second.A) == Zero &&
               TrianglePlaneOffset(first, second.B) == Zero &&
               TrianglePlaneOffset(first, second.C) == Zero;
    }

    // Determines whether the line between the endpoints intersects strictly inside the triangle.
    public static bool DoesLineIntersectTriangleInterior(in Point firstPoint, in Point secondPoint, in Triangle triangle)
    {
        var orientAtStart = TrianglePlaneOffset(triangle, firstPoint);
        var orientAtEnd = TrianglePlaneOffset(triangle, secondPoint);

        // Must cross the plane strictly (endpoints on the plane are boundary-only)
        if (orientAtStart == Zero || orientAtEnd == Zero)
        {
            return false;
        }

        if ((orientAtStart > Zero && orientAtEnd > Zero) || (orientAtStart < Zero && orientAtEnd < Zero))
        {
            return false;
        }

        var orientOppositeAB = IntegerMath.SignedTetrahedronVolume6(firstPoint, secondPoint, triangle.A, triangle.B);
        var orientOppositeBC = IntegerMath.SignedTetrahedronVolume6(firstPoint, secondPoint, triangle.B, triangle.C);
        var orientOppositeCA = IntegerMath.SignedTetrahedronVolume6(firstPoint, secondPoint, triangle.C, triangle.A);

        // Any zero means the intersection lies on an edge/vertex (boundary-only)
        if (orientOppositeAB == Zero || orientOppositeBC == Zero || orientOppositeCA == Zero)
        {
            return false;
        }

        var positive = 0; var negative = 0;
        CountSign(orientOppositeAB, ref positive, ref negative);
        CountSign(orientOppositeBC, ref positive, ref negative);
        CountSign(orientOppositeCA, ref positive, ref negative);
        return positive == 3 || negative == 3;
    }

    // Returns true when a point lies on the triangle plane and inside or on edges.
    public static bool IsPointInTriangleInclusive(in Point point, in Triangle triangle)
    {
        if (!IsPointOnTrianglePlane(triangle, point))
        {
            return false;
        }

        var p = point.ToVector3();
        return IsPointInTriangleInclusive(p, triangle);
    }

    // Returns true when a point lies strictly inside the triangle interior.
    public static bool IsPointInTriangleInterior(in Point point, in Triangle triangle)
    {
        if (!IsPointOnTrianglePlane(triangle, point))
        {
            return false;
        }

        var p = point.ToVector3();
        return IsPointInTriangleInterior(p, triangle);
    }

    // Vector3 helper that allows inclusion checks on projected triangles.
    private static bool IsPointInTriangleInclusive(in System.Numerics.Vector3 p, in Triangle tri)
    {
        var a = tri.A.ToVector3();
        var b = tri.B.ToVector3();
        var c = tri.C.ToVector3();
        var n = System.Numerics.Vector3.Cross(b - a, c - a);
        if (n.LengthSquared() <= 0)
        {
            return false;
        }
        var c0 = System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(b - a, p - a), n);
        var c1 = System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(c - b, p - b), n);
        var c2 = System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(a - c, p - c), n);
        return c0 >= 0 && c1 >= 0 && c2 >= 0;
    }

    // Vector3 helper that enforces strict interior membership.
    private static bool IsPointInTriangleInterior(in System.Numerics.Vector3 p, in Triangle tri)
    {
        var a = tri.A.ToVector3();
        var b = tri.B.ToVector3();
        var c = tri.C.ToVector3();
        var n = System.Numerics.Vector3.Cross(b - a, c - a);
        if (n.LengthSquared() <= 0)
        {
            return false;
        }
        var c0 = System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(b - a, p - a), n);
        var c1 = System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(c - b, p - b), n);
        var c2 = System.Numerics.Vector3.Dot(System.Numerics.Vector3.Cross(a - c, p - c), n);
        return c0 > 0 && c1 > 0 && c2 > 0;
    }

    // Detects whether the interiors of two triangles overlap.
    public static bool DoTrianglesIntersectInterior(in Triangle first, in Triangle second)
    {
        // edge lines of first vs interior of second
        if (DoesLineIntersectTriangleInterior(first.A, first.B, second)) return true;
        if (DoesLineIntersectTriangleInterior(first.B, first.C, second)) return true;
        if (DoesLineIntersectTriangleInterior(first.C, first.A, second)) return true;

        // edge lines of second vs interior of first
        if (DoesLineIntersectTriangleInterior(second.A, second.B, first)) return true;
        if (DoesLineIntersectTriangleInterior(second.B, second.C, first)) return true;
        if (DoesLineIntersectTriangleInterior(second.C, second.A, first)) return true;

        // containment
        if (IsPointInTriangleInterior(first.A, second)) return true;
        if (IsPointInTriangleInterior(first.B, second)) return true;
        if (IsPointInTriangleInterior(first.C, second)) return true;
        if (IsPointInTriangleInterior(second.A, first)) return true;
        if (IsPointInTriangleInterior(second.B, first)) return true;
        if (IsPointInTriangleInterior(second.C, first)) return true;

        return false;
    }

    // Indicates that triangles meet only along their perimeters.
    public static bool DoTrianglesTouchBoundaryOnly(in Triangle first, in Triangle second)
        => DoesTriangleIntersectTriangle(first, second) && !DoTrianglesIntersectInterior(first, second);

    // Triangle utility: centroid and unit normal (for facing metrics)
    // Computes the centroid of a triangle in 3D space.
    public static Vector3 GetTriangleCentroidVector(in Triangle triangle)
    {
        var a = triangle.A.ToVector3();
        var b = triangle.B.ToVector3();
        var c = triangle.C.ToVector3();
        return (a + b + c) / 3f;
    }

    // Returns a unit-length normal vector for the triangle or zero if degenerate.
    public static Vector3 GetTriangleUnitNormal(in Triangle triangle)
    {
        var a = triangle.A.ToVector3();
        var b = triangle.B.ToVector3();
        var c = triangle.C.ToVector3();
        var normal = Vector3.Cross(b - a, c - a);
        var len2 = normal.LengthSquared();
        return len2 <= float.Epsilon ? Vector3.Zero : normal / MathF.Sqrt(len2);
    }

    // Calculates how strongly a triangle faces towards a target point.
    public static float ComputeFacingScore(Vector3 unitNormal, Vector3 centroid, Vector3 targetCenter)
    {
        var dir = targetCenter - centroid;
        var len2 = dir.LengthSquared();
        if (unitNormal == Vector3.Zero || len2 <= float.Epsilon)
        {
            return -1f;
        }
        dir /= MathF.Sqrt(len2);
        return Vector3.Dot(unitNormal, dir);
    }


    // Verifies every vertex of other lies on the positive side of planeTriangle.
    public static bool IsTriangleFullyOnPositiveSideOfTrianglePlane(
        in Triangle planeTriangle,
        in Triangle other,
        bool strict = true)
    {
        foreach (var v in other.Vertices)
        {
            var offset = IntegerMath.SignedTetrahedronVolume6(planeTriangle.A, planeTriangle.B, planeTriangle.C, v);
            if (strict)
            {
                if (offset <= Zero) return false;
            }
            else
            {
                if (offset < Zero) return false;
            }
        }
        return true;
    }

    // Solid utility: center of bounds as a Vector3
    // Computes the center of a solid's axis-aligned bounding box.
    public static Vector3 GetSolidCenterVector(Solid solid)
    {
        var bounds = solid.GetBounds();
        return new Vector3(
            (bounds.Min.X + bounds.Max.X) * 0.5f,
            (bounds.Min.Y + bounds.Max.Y) * 0.5f,
            (bounds.Min.Z + bounds.Max.Z) * 0.5f);
    }
}


