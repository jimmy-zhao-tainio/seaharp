namespace Seaharp.Geometry.Bridging;

public enum BridgeCase
{
    Empty,
    SharedEdge,
    VertexOnEdge,
    SharedVertex,
    Prism3Tets,
    Octa4Tets
}

public static class TriangleBridgeBuilder
{
    public static BridgeCase Classify(in Triangle t1, in Triangle t2)
    {
        if (TriangleOperations.TryGetSharedEdge(t1, t2, out _))
        {
            return BridgeCase.SharedEdge;
        }

        if (TriangleOperations.TryGetVertexOnEdge(t1, t2, out _) ||
            TriangleOperations.TryGetVertexOnEdge(t2, t1, out _))
        {
            return BridgeCase.VertexOnEdge;
        }

        if (GeometryChecks.GetSharedVertexCount(t1, t2) == 1)
        {
            return BridgeCase.SharedVertex;
        }

        if (GeometryChecks.DoesTriangleIntersectTriangle(t1, t2))
        {
            return BridgeCase.Empty;
        }

        var t2Strict = GeometryChecks.ArePointsStrictlyOnSameSideOfTrianglePlane(t1, t2.A, t2.B, t2.C);
        var t1Strict = GeometryChecks.ArePointsStrictlyOnSameSideOfTrianglePlane(t2, t1.A, t1.B, t1.C);

        if (t1Strict && t2Strict)
        {
            return BridgeCase.Prism3Tets;
        }

        return BridgeCase.Empty;
    }

    public static IReadOnlyList<Tetrahedron> Connect(in Triangle t1, in Triangle t2)
    {
        var classification = Classify(t1, t2);
        return BuildConnection(t1, t2, classification);
    }

    public static bool TryConnect(in Triangle t1, in Triangle t2, out List<Tetrahedron> tets)
    {
        var classification = Classify(t1, t2);
        if (classification == BridgeCase.Empty)
        {
            tets = new List<Tetrahedron>();
            return false;
        }

        var result = BuildConnection(t1, t2, classification);
        tets = new List<Tetrahedron>(result);
        return true;
    }

    public static BridgeCase Explain(in Triangle t1, in Triangle t2) => Classify(t1, t2);

    private static IReadOnlyList<Tetrahedron> BuildConnection(in Triangle t1, in Triangle t2, BridgeCase classification)
    {
        return classification switch
        {
            BridgeCase.SharedEdge => SharedEdgeBridgeStrategy.Build(t1, t2),
            BridgeCase.VertexOnEdge => VertexOnEdgeBridgeStrategy.Build(t1, t2),
            BridgeCase.SharedVertex => SharedVertexBridgeStrategy.Build(t1, t2),
            BridgeCase.Prism3Tets => PrismBridgeStrategy.Build(t1, t2),
            _ => Array.Empty<Tetrahedron>()
        };
    }
}
