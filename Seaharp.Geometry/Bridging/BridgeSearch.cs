using System;
using System.Collections.Generic;
using System.Linq;

namespace Seaharp.Geometry.Bridging;

public static class BridgeSearch
{
    public static bool TryFindBridge(
        Solid firstSolid,
        Solid secondSolid,
        out Triangle firstTriangle,
        out Triangle secondTriangle,
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

        firstTriangle = default;
        secondTriangle = default;
        tetrahedrons = new List<Tetrahedron>();

        var firstBoundary = firstSolid.BoundaryTriangles().ToArray();
        var secondBoundary = secondSolid.BoundaryTriangles().ToArray();

        foreach (var firstCandidate in firstBoundary)
        {
            foreach (var secondCandidate in secondBoundary)
            {
                if (TriangleBridgeBuilder.Classify(firstCandidate, secondCandidate) != BridgeCase.Prism3Tets)
                {
                    continue;
                }

                var connection = TriangleBridgeBuilder.Connect(firstCandidate, secondCandidate);
                if (connection.Count != 3)
                {
                    continue;
                }

                if (!IsBridgeClear(firstCandidate, secondCandidate, connection, firstBoundary, secondBoundary))
                {
                    continue;
                }

                firstTriangle = firstCandidate;
                secondTriangle = secondCandidate;
                tetrahedrons = connection.ToList();
                return true;
            }
        }

        return false;
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
            foreach (var bridgeFace in EnumerateFaces(tetrahedron))
            {
                foreach (var boundaryTriangle in firstBoundary)
                {
                    if (GeometryChecks.AreTrianglesIdentical(boundaryTriangle, firstTriangle) ||
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
                        GeometryChecks.DoTrianglesShareVertex(boundaryTriangle, firstTriangle))
                    {
                        continue;
                    }

                    if (GeometryChecks.IsPointOnTrianglePlane(boundaryTriangle, vertex))
                    {
                        return false;
                    }
                }

                foreach (var boundaryTriangle in secondBoundary)
                {
                    if (GeometryChecks.AreTrianglesIdentical(boundaryTriangle, secondTriangle) ||
                        GeometryChecks.DoTrianglesShareVertex(boundaryTriangle, secondTriangle))
                    {
                        continue;
                    }

                    if (GeometryChecks.IsPointOnTrianglePlane(boundaryTriangle, vertex))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static IEnumerable<Triangle> EnumerateFaces(Tetrahedron tetrahedron)
    {
        var vertices = tetrahedron.Vertices;
        yield return new Triangle(vertices[0], vertices[1], vertices[2]);
        yield return new Triangle(vertices[0], vertices[2], vertices[3]);
        yield return new Triangle(vertices[0], vertices[3], vertices[1]);
        yield return new Triangle(vertices[1], vertices[3], vertices[2]);
    }
}
