using System;
using System.Collections.Generic;

namespace Seaharp.Geometry;

public static class TriangleOperations
{
    // Returns the two vertices of the triangle that are not the excluded vertex.
    public static (Point First, Point Second) GetRemainingVertices(in Triangle triangle, in Point excluded)
    {
        Point? first = null;
        Point? second = null;

        foreach (var vertex in triangle.Vertices)
        {
            if (vertex.Equals(excluded))
            {
                continue;
            }

            if (first is null)
            {
                first = vertex;
            }
            else if (second is null)
            {
                second = vertex;
            }
            else
            {
                break;
            }
        }

        if (first is null || second is null)
        {
            throw new InvalidOperationException("Expected exactly two remaining vertices.");
        }

        return (first.Value, second.Value);
    }

    // Returns the vertex of the triangle that does not lie on the given edge.
    public static Point GetOppositeVertex(in Triangle triangle, in Point edgeStart, in Point edgeEnd)
    {
        foreach (var vertex in triangle.Vertices)
        {
            if (!vertex.Equals(edgeStart) && !vertex.Equals(edgeEnd))
            {
                return vertex;
            }
        }

        throw new InvalidOperationException("Edge must be formed by two vertices of the triangle.");
    }

    // Attempts to find the edge shared by both triangles and returns endpoints.
    public static bool TryGetSharedEdge(in Triangle first, in Triangle second, out Edge sharedEdge)
    {
        sharedEdge = default;

        Point? start = null;
        Point? end = null;

        foreach (var vertex in first.Vertices)
        {
            if (vertex.Equals(second.A) || vertex.Equals(second.B) || vertex.Equals(second.C))
            {
                if (start is null)
                {
                    start = vertex;
                }
                else if (end is null)
                {
                    end = vertex;
                }
                else
                {
                    sharedEdge = default;
                    return false;
                }
            }
        }

        if (start is not null && end is not null)
        {
            sharedEdge = new Edge(start.Value, end.Value).Canonicalize();
            return true;
        }

        return false;
    }

    // Finds a vertex of other lying strictly on one of host's edges.
    public static bool TryGetVertexOnEdge(in Triangle host, in Triangle other, out VertexOnEdgeMatch match)
    {
        foreach (var (edgeStart, edgeEnd) in EnumerateEdges(host))
        {
            foreach (var vertex in other.Vertices)
            {
                if (vertex.Equals(edgeStart) || vertex.Equals(edgeEnd))
                {
                    continue;
                }

                if (!GeometryChecks.ArePointsOnSameLine(edgeStart, edgeEnd, vertex))
                {
                    continue;
                }

                if (!GeometryChecks.IsPointOnLine(edgeStart, edgeEnd, vertex))
                {
                    continue;
                }

                var (other0, other1) = GetRemainingVertices(other, vertex);
                match = new VertexOnEdgeMatch(vertex, new Edge(edgeStart, edgeEnd), other0, other1);
                return true;
            }
        }

        match = default;
        return false;
    }

    private static IEnumerable<(Point Start, Point End)> EnumerateEdges(Triangle triangle)
    {
        yield return (triangle.A, triangle.B);
        yield return (triangle.B, triangle.C);
        yield return (triangle.C, triangle.A);
    }
}
