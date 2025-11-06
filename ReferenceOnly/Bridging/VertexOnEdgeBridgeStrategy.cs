using System.Collections.Generic;

namespace Seaharp.Geometry.Bridging;

internal static class VertexOnEdgeBridgeStrategy
{
    public static IReadOnlyList<Tetrahedron> Build(in Triangle t1, in Triangle t2)
    {
        Triangle host = t1;
        Triangle other = t2;
        VertexOnEdgeMatch match;

        if (!TriangleOperations.TryGetVertexOnEdge(host, other, out match))
        {
            host = t2;
            other = t1;
            if (!TriangleOperations.TryGetVertexOnEdge(host, other, out match))
            {
                return Array.Empty<Tetrahedron>();
            }
        }

        var onV = match.VertexOnEdge;
        var e0 = match.HostEdge.Start;
        var e1 = match.HostEdge.End;
        var o0 = match.OtherVertex0;
        var o1 = match.OtherVertex1;

        var hostThird = TriangleOperations.GetOppositeVertex(host, e0, e1);

        var equatorVolume = IntegerMath.AbsoluteTetrahedronVolume6(e0, e1, o0, o1);
        var includeEquator = equatorVolume != default;

        var hostCapO0Volume = IntegerMath.AbsoluteTetrahedronVolume6(host.A, host.B, host.C, o0);
        var hostCapO1Volume = IntegerMath.AbsoluteTetrahedronVolume6(host.A, host.B, host.C, o1);
        var otherCapVolume = IntegerMath.AbsoluteTetrahedronVolume6(hostThird, onV, o0, o1);

        var candidate0Min = BridgeVolumeUtilities.MinVolumes(hostCapO0Volume, otherCapVolume, includeEquator ? equatorVolume : (Int128?)null);
        var candidate1Min = BridgeVolumeUtilities.MinVolumes(hostCapO1Volume, otherCapVolume, includeEquator ? equatorVolume : (Int128?)null);

        var result = new List<Tetrahedron>();

        if (includeEquator)
        {
            result.Add(new Tetrahedron(e0, e1, o0, o1));
        }

        if (candidate1Min > candidate0Min)
        {
            result.Add(new Tetrahedron(host.A, host.B, host.C, o1));
        }
        else
        {
            result.Add(new Tetrahedron(host.A, host.B, host.C, o0));
        }

        result.Add(new Tetrahedron(hostThird, onV, o0, o1));

        return result;
    }
}
