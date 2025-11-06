using System.Collections.Generic;

namespace Seaharp.Geometry.Bridging;

internal static class SharedEdgeBridgeStrategy
{
    public static IReadOnlyList<Tetrahedron> Build(in Triangle t1, in Triangle t2)
    {
        if (!TriangleOperations.TryGetSharedEdge(t1, t2, out var edge))
        {
            return Array.Empty<Tetrahedron>();
        }

        var t1Opposite = TriangleOperations.GetOppositeVertex(t1, edge.Start, edge.End);
        var t2Opposite = TriangleOperations.GetOppositeVertex(t2, edge.Start, edge.End);

        return new List<Tetrahedron>
        {
            new Tetrahedron(edge.Start, edge.End, t1Opposite, t2Opposite)
        };
    }
}
