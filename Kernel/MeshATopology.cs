using System;
using System.Collections.Generic;
using Geometry;

namespace Kernel;

// Mesh-local intersection topology for mesh A.
//
// This describes which global intersection edges lie on which triangles
// of mesh A, how those edges are incident to global vertices, and the
// resulting closed intersection loops traced on mesh A.
public sealed class MeshATopology
{
    // For each triangle in IntersectionSet.TrianglesA, the set of
    // global intersection edges that lie on it (both endpoints lie on
    // that triangle).
    public IReadOnlyList<IntersectionEdgeId[]> TriangleEdges { get; }

    // All global edges that touch at least one triangle in mesh A.
    public IReadOnlyList<IntersectionEdgeId> Edges { get; }

    // Vertex-edge adjacency restricted to mesh A: for each global
    // intersection vertex, the list of incident edges that lie on
    // triangles of mesh A.
    public IReadOnlyDictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> VertexEdges { get; }

    // Per-component vertex chains on mesh A, expressed as sequences of global
    // IntersectionVertexId. Some components form closed cycles, others may
    // be open chains when local degeneracies are present.
    public IReadOnlyList<IntersectionVertexId[]> Loops { get; }

    private MeshATopology(
        IntersectionEdgeId[][] triangleEdges,
        IntersectionEdgeId[] edges,
        Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>> vertexEdges,
        IntersectionVertexId[][] loops)
    {
        TriangleEdges = triangleEdges ?? throw new ArgumentNullException(nameof(triangleEdges));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        VertexEdges = vertexEdges ?? throw new ArgumentNullException(nameof(vertexEdges));
        Loops = loops ?? throw new ArgumentNullException(nameof(loops));
    }

    public static MeshATopology Build(IntersectionGraph graph, TriangleIntersectionIndex index)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (index is null) throw new ArgumentNullException(nameof(index));

        var trianglesA = graph.IntersectionSet.TrianglesA
            ?? throw new ArgumentNullException(nameof(graph.IntersectionSet.TrianglesA));

        int triangleCountA = trianglesA.Count;

        // Per-triangle sets of global vertex ids for mesh A.
        var perTriangleVertexIds = new HashSet<int>[triangleCountA];
        for (int i = 0; i < triangleCountA; i++)
        {
            var verts = index.TrianglesA[i];
            var set = new HashSet<int>();
            for (int j = 0; j < verts.Length; j++)
            {
                set.Add(verts[j].VertexId.Value);
            }
            perTriangleVertexIds[i] = set;
        }

        // Map each edge id to its endpoints.
        var edgeById = new Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)>();
        foreach (var (id, start, end) in graph.Edges)
        {
            edgeById[id.Value] = (start, end);
        }

        // For each triangle in A, collect the edges whose endpoints both lie
        // on that triangle.
        var triangleEdgeLists = new List<IntersectionEdgeId>[triangleCountA];
        for (int i = 0; i < triangleCountA; i++)
        {
            triangleEdgeLists[i] = new List<IntersectionEdgeId>();
        }

        foreach (var (id, start, end) in graph.Edges)
        {
            int sId = start.Value;
            int eId = end.Value;

            for (int triIndex = 0; triIndex < triangleCountA; triIndex++)
            {
                var vertexSet = perTriangleVertexIds[triIndex];
                if (vertexSet.Contains(sId) && vertexSet.Contains(eId))
                {
                    triangleEdgeLists[triIndex].Add(id);
                }
            }
        }

        // Build vertex-edge adjacency restricted to edges that lie on mesh A.
        var vertexAdjacency = new Dictionary<IntersectionVertexId, List<IntersectionEdgeId>>();
        var meshEdgesSet = new HashSet<int>();

        for (int triIndex = 0; triIndex < triangleCountA; triIndex++)
        {
            var edgesOnTri = triangleEdgeLists[triIndex];
            for (int i = 0; i < edgesOnTri.Count; i++)
            {
                var edgeId = edgesOnTri[i];
                if (!meshEdgesSet.Add(edgeId.Value))
                {
                    continue; // Already accounted for this edge in adjacency.
                }

                var endpoints = edgeById[edgeId.Value];
                AddEdgeToAdjacency(vertexAdjacency, endpoints.Start, edgeId);
                AddEdgeToAdjacency(vertexAdjacency, endpoints.End, edgeId);
            }
        }

        // Convert adjacency lists to read-only views.
        var vertexEdges = new Dictionary<IntersectionVertexId, IReadOnlyList<IntersectionEdgeId>>(vertexAdjacency.Count);
        foreach (var kvp in vertexAdjacency)
        {
            vertexEdges.Add(kvp.Key, kvp.Value.AsReadOnly());
        }

        // Convert per-triangle edges to arrays.
        var triangleEdges = new IntersectionEdgeId[triangleCountA][];
        for (int i = 0; i < triangleCountA; i++)
        {
            var list = triangleEdgeLists[i];
            triangleEdges[i] = list.Count == 0 ? Array.Empty<IntersectionEdgeId>() : list.ToArray();
        }

        // Flatten mesh-A edge ids into a list.
        var meshEdges = new List<IntersectionEdgeId>(meshEdgesSet.Count);
        foreach (var edgeValue in meshEdgesSet)
        {
            meshEdges.Add(new IntersectionEdgeId(edgeValue));
        }

        // Extract closed loops over mesh A.
        var loops = ExtractLoops(vertexAdjacency, edgeById, meshEdgesSet);

        return new MeshATopology(
            triangleEdges,
            meshEdges.ToArray(),
            vertexEdges,
            loops.ToArray());
    }

    private static void AddEdgeToAdjacency(
        Dictionary<IntersectionVertexId, List<IntersectionEdgeId>> adjacency,
        IntersectionVertexId vertex,
        IntersectionEdgeId edge)
    {
        if (!adjacency.TryGetValue(vertex, out var list))
        {
            list = new List<IntersectionEdgeId>();
            adjacency[vertex] = list;
        }

        list.Add(edge);
    }

    private static List<IntersectionVertexId[]> ExtractLoops(
        Dictionary<IntersectionVertexId, List<IntersectionEdgeId>> adjacency,
        Dictionary<int, (IntersectionVertexId Start, IntersectionVertexId End)> edgeById,
        HashSet<int> meshEdges)
    {
        var remainingEdges = new HashSet<int>(meshEdges);
        var loops = new List<IntersectionVertexId[]>();

        while (remainingEdges.Count > 0)
        {
            // Pick an arbitrary remaining edge to seed the next loop.
            int seedEdgeValue = 0;
            foreach (var value in remainingEdges)
            {
                seedEdgeValue = value;
                break;
            }

            var endpoints = edgeById[seedEdgeValue];
            var startVertex = endpoints.Start;
            var currentVertex = endpoints.End;

            var loop = new List<IntersectionVertexId>
            {
                startVertex,
                currentVertex
            };

            remainingEdges.Remove(seedEdgeValue);

            while (true)
            {
                if (!adjacency.TryGetValue(currentVertex, out var incidentEdges))
                {
                    break;
                }

                IntersectionEdgeId nextEdge = default;
                bool foundNext = false;

                for (int i = 0; i < incidentEdges.Count; i++)
                {
                    var candidate = incidentEdges[i];
                    if (remainingEdges.Contains(candidate.Value))
                    {
                        nextEdge = candidate;
                        foundNext = true;
                        break;
                    }
                }

                if (!foundNext)
                {
                    // No unused incident edge from this vertex; loop should be closed.
                    break;
                }

                remainingEdges.Remove(nextEdge.Value);
                var nextEndpoints = edgeById[nextEdge.Value];
                var nextVertex = nextEndpoints.Start.Value == currentVertex.Value
                    ? nextEndpoints.End
                    : nextEndpoints.Start;

                if (nextVertex.Value == startVertex.Value)
                {
                    // Close the cycle.
                    loop.Add(startVertex);
                    break;
                }

                loop.Add(nextVertex);
                currentVertex = nextVertex;
            }

            loops.Add(loop.ToArray());
        }

        return loops;
    }
}
