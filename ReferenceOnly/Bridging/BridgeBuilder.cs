using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Seaharp.Geometry.Bridging;

public static class BridgeBuilder
{
    public static Shape BuildBridge(Shape firstShape, Shape secondShape)
    {
        if (firstShape is null) throw new ArgumentNullException(nameof(firstShape));
        if (secondShape is null) throw new ArgumentNullException(nameof(secondShape));
        if (firstShape.Unit != secondShape.Unit) throw new InvalidOperationException("Mismatched units.");

        if (TryFindBridge(firstShape.Solid, secondShape.Solid, out var tetrahedrons))
        {
            var bridgeSolid = new Solid(tetrahedrons);
            return new Shape(bridgeSolid);
        }

        return Shape.Empty;
    }

    public static bool TryFindBridge(
        Solid firstSolid,
        Solid secondSolid,
        out List<Tetrahedron> tetrahedrons)
    {
        if (firstSolid is null)
        {
            throw new ArgumentNullException(nameof(firstSolid));
        }
        if (secondSolid is null)
        {
            throw new ArgumentNullException(nameof(secondSolid));
        }

        tetrahedrons = new List<Tetrahedron>();

        var firstBoundary = firstSolid.BoundaryTriangles().ToArray();
        var secondBoundary = secondSolid.BoundaryTriangles().ToArray();

        var visiblePairs = GetVisibleTrianglePairs(firstSolid, secondSolid, strict: false).ToList();
        var pairs = visiblePairs.Count; var classified = 0; var connected = 0; var cleared = 0;

        foreach (var (firstTriangle, secondTriangle) in visiblePairs)
        {
            if (TriangleBridgeBuilder.Classify(firstTriangle, secondTriangle) != BridgeCase.Prism3Tets)
            {
                continue;
            }

            classified++;

            var connection = TriangleBridgeBuilder.Connect(firstTriangle, secondTriangle);
            if (connection.Count != 3)
            {
                continue;
            }
            connected++;

            if (!IsBridgeClear(firstTriangle, secondTriangle, connection, firstBoundary, secondBoundary))
            {
                continue;
            }
            cleared++;

            tetrahedrons = connection.ToList();
            return true;
        }

        return false;
    }

    internal static IEnumerable<(Triangle First, Triangle Second)> GetVisibleTrianglePairs(
        Solid firstSolid,
        Solid secondSolid,
        bool strict)
    {
        if (firstSolid is null)
        {
            throw new ArgumentNullException(nameof(firstSolid));
        }
        if (secondSolid is null)
        {
            throw new ArgumentNullException(nameof(secondSolid));
        }

        var firstBoundary = firstSolid.BoundaryTriangles();
        var secondBoundary = secondSolid.BoundaryTriangles().ToArray();

        foreach (var firstTriangle in firstBoundary)
        {
            foreach (var secondTriangle in secondBoundary)
            {
                if (!GeometryChecks.IsTriangleFullyOnPositiveSideOfTrianglePlane(firstTriangle, secondTriangle, strict))
                {
                    continue;
                }
                if (!GeometryChecks.IsTriangleFullyOnPositiveSideOfTrianglePlane(secondTriangle, firstTriangle, strict))
                {
                    continue;
                }
                if (GeometryChecks.AreTrianglesCoplanar(firstTriangle, secondTriangle))
                {
                    continue;
                }
                if (!AreNormalsFacingEachOther(firstTriangle, secondTriangle))
                {
                    continue;
                }
                if (PlaneCutsTriangleInterior(firstTriangle, secondTriangle) ||
                    PlaneCutsTriangleInterior(secondTriangle, firstTriangle))
                {
                    continue;
                }
                yield return (firstTriangle, secondTriangle);
            }
        }
    }

    internal static bool IsBridgeClear(
        Triangle firstTriangle,
        Triangle secondTriangle,
        IReadOnlyList<Tetrahedron> connectionTetrahedrons,
        IReadOnlyList<Triangle> firstBoundary,
        IReadOnlyList<Triangle> secondBoundary)
    {
        foreach (var tetrahedron in connectionTetrahedrons)
        {
            foreach (var bridgeFace in tetrahedron.Faces)
            {
                foreach (var boundaryTriangle in firstBoundary)
                {
                    if (GeometryChecks.AreTrianglesIdentical(boundaryTriangle, firstTriangle) ||
                        GeometryChecks.DoTrianglesShareEdge(boundaryTriangle, firstTriangle) ||
                        GeometryChecks.DoTrianglesShareVertex(boundaryTriangle, firstTriangle))
                    {
                        continue;
                    }

                    if (GeometryChecks.DoesTriangleIntersectTriangle(bridgeFace, boundaryTriangle))
                    {
                        return false;
                    }
                }

                foreach (var boundaryTriangle in secondBoundary)
                {
                    if (GeometryChecks.AreTrianglesIdentical(boundaryTriangle, secondTriangle) ||
                        GeometryChecks.DoTrianglesShareEdge(boundaryTriangle, secondTriangle) ||
                        GeometryChecks.DoTrianglesShareVertex(boundaryTriangle, secondTriangle))
                    {
                        continue;
                    }

                    if (GeometryChecks.DoesTriangleIntersectTriangle(bridgeFace, boundaryTriangle))
                    {
                        return false;
                    }
                }
            }
        }

        foreach (var tetrahedron in connectionTetrahedrons)
        {
            foreach (var vertex in tetrahedron.Vertices)
            {
                foreach (var boundaryTriangle in firstBoundary)
                {
                    if (GeometryChecks.AreTrianglesIdentical(boundaryTriangle, firstTriangle) ||
                        GeometryChecks.DoTrianglesShareEdge(boundaryTriangle, firstTriangle) ||
                        GeometryChecks.DoTrianglesShareVertex(boundaryTriangle, firstTriangle))
                    {
                        continue;
                    }

                    if (GeometryChecks.IsPointOnTrianglePlane(boundaryTriangle, vertex) &&
                        GeometryChecks.IsPointInTriangleInterior(vertex, boundaryTriangle))
                    {
                        return false;
                    }
                }

                foreach (var boundaryTriangle in secondBoundary)
                {
                    if (GeometryChecks.AreTrianglesIdentical(boundaryTriangle, secondTriangle) ||
                        GeometryChecks.DoTrianglesShareEdge(boundaryTriangle, secondTriangle) ||
                        GeometryChecks.DoTrianglesShareVertex(boundaryTriangle, secondTriangle))
                    {
                        continue;
                    }

                    if (GeometryChecks.IsPointOnTrianglePlane(boundaryTriangle, vertex) &&
                        GeometryChecks.IsPointInTriangleInterior(vertex, boundaryTriangle))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool AreNormalsFacingEachOther(in Triangle first, in Triangle second)
    {
        var firstNormal = GeometryChecks.GetTriangleUnitNormal(first);
        var secondNormal = GeometryChecks.GetTriangleUnitNormal(second);

        if (firstNormal == Vector3.Zero || secondNormal == Vector3.Zero)
        {
            return false;
        }

        const float OpposingDotThreshold = -0.05f;
        if (Vector3.Dot(firstNormal, secondNormal) > OpposingDotThreshold)
        {
            return false;
        }

        var firstCentroid = GeometryChecks.GetTriangleCentroidVector(first);
        var secondCentroid = GeometryChecks.GetTriangleCentroidVector(second);
        var firstToSecond = secondCentroid - firstCentroid;
        var distanceSq = firstToSecond.LengthSquared();
        if (distanceSq <= float.Epsilon)
        {
            return false;
        }

        firstToSecond /= MathF.Sqrt(distanceSq);
        var secondToFirst = -firstToSecond;

        const float FacingEpsilon = 1e-3f;

        return Vector3.Dot(firstNormal, firstToSecond) > FacingEpsilon &&
               Vector3.Dot(secondNormal, secondToFirst) > FacingEpsilon;
    }

    private static bool PlaneCutsTriangleInterior(in Triangle planeTriangle, in Triangle other)
    {
        var o0 = IntegerMath.SignedTetrahedronVolume6(planeTriangle.A, planeTriangle.B, planeTriangle.C, other.A);
        var o1 = IntegerMath.SignedTetrahedronVolume6(planeTriangle.A, planeTriangle.B, planeTriangle.C, other.B);
        var o2 = IntegerMath.SignedTetrahedronVolume6(planeTriangle.A, planeTriangle.B, planeTriangle.C, other.C);

        var min = o0;
        if (o1 < min) min = o1;
        if (o2 < min) min = o2;

        var max = o0;
        if (o1 > max) max = o1;
        if (o2 > max) max = o2;

        return min < default(Int128) && max > default(Int128);
    }
}
