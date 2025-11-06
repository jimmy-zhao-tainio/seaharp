using System.Collections.Generic;

namespace Seaharp.Geometry.Bridging;

internal static class SharedVertexBridgeStrategy
{
    public static IReadOnlyList<Tetrahedron> Build(in Triangle t1, in Triangle t2)
    {
        if (!TryExtractSharedVertexConfiguration(t1, t2, out var shared, out var b, out var c, out var d, out var e))
        {
            return Array.Empty<Tetrahedron>();
        }

        var equatorVolume = IntegerMath.AbsoluteTetrahedronVolume6(b, c, d, e);
        var includeEquator = equatorVolume != default;

        var capDVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, b, c, d);
        var capEVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, b, c, e);
        var complementDVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, b, d, e);
        var complementEVolume = IntegerMath.AbsoluteTetrahedronVolume6(shared, c, d, e);

        var candidateDMin = BridgeVolumeUtilities.MinVolumes(capDVolume, complementDVolume, includeEquator ? equatorVolume : (Int128?)null);
        var candidateEMin = BridgeVolumeUtilities.MinVolumes(capEVolume, complementEVolume, includeEquator ? equatorVolume : (Int128?)null);

        var result = new List<Tetrahedron>();
        if (includeEquator)
        {
            result.Add(new Tetrahedron(b, c, d, e));
        }

        if (candidateEMin > candidateDMin)
        {
            result.Add(new Tetrahedron(shared, b, c, e));
            result.Add(new Tetrahedron(shared, c, d, e));
        }
        else
        {
            result.Add(new Tetrahedron(shared, b, c, d));
            result.Add(new Tetrahedron(shared, b, d, e));
        }

        return result;
    }

    private static bool TryExtractSharedVertexConfiguration(
        in Triangle t1,
        in Triangle t2,
        out Point shared,
        out Point b,
        out Point c,
        out Point d,
        out Point e)
    {
        shared = default;
        b = default;
        c = default;
        d = default;
        e = default;

        var vertices1 = new[] { t1.A, t1.B, t1.C };
        var vertices2 = new[] { t2.A, t2.B, t2.C };

        Point? sharedCandidate = null;
        foreach (var v in vertices1)
        {
            if (v.Equals(vertices2[0]) || v.Equals(vertices2[1]) || v.Equals(vertices2[2]))
            {
                sharedCandidate = v;
                break;
            }
        }

        if (sharedCandidate is null)
        {
            return false;
        }

        shared = sharedCandidate.Value;
        var others1 = TriangleOperations.GetRemainingVertices(t1, shared);
        var others2 = TriangleOperations.GetRemainingVertices(t2, shared);

        b = others1.Item1;
        c = others1.Item2;
        d = others2.Item1;
        e = others2.Item2;
        return true;
    }
}
