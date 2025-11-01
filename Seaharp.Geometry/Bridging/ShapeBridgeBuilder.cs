using System;
using System.Collections.Generic;
using System.Linq;

namespace Seaharp.Geometry.Bridging;

public static class ShapeBridgeBuilder
{
    public static bool TryBuildBridge(Shape lower, Shape upper, out Shape bridge, out Triangle lowerFace, out Triangle upperFace)
    {
        if (lower is null) throw new ArgumentNullException(nameof(lower));
        if (upper is null) throw new ArgumentNullException(nameof(upper));
        if (lower.Unit != upper.Unit) throw new InvalidOperationException("Mismatched units.");

        bridge = lower;
        lowerFace = default;
        upperFace = default;

        var lowerTopTriangles = lower.Faces(f => f.Vertices.All(v => v.Z == lower.Bounds.Max.Z)).ToArray();
        var upperBottomTriangles = upper.Faces(f => f.Vertices.All(v => v.Z == upper.Bounds.Min.Z)).ToArray();

        var allA = lower.Solid.BoundaryTriangles().ToArray();
        var allB = upper.Solid.BoundaryTriangles().ToArray();

        foreach (var lowerCandidate in lowerTopTriangles)
        {
            foreach (var upperCandidate in upperBottomTriangles)
            {
                if (TriangleBridgeBuilder.Classify(lowerCandidate, upperCandidate) != BridgeCase.Prism3Tets)
                {
                    continue;
                }

                var connection = TriangleBridgeBuilder.Connect(lowerCandidate, upperCandidate);
                if (connection.Count != 3)
                {
                    continue;
                }

                if (!BridgeSearch.IsBridgeClear(lowerCandidate, upperCandidate, connection, allA, allB))
                {
                    continue;
                }

                bridge = lower.With(new Solid(lower.Unit, connection));
                lowerFace = lowerCandidate;
                upperFace = upperCandidate;
                return true;
            }
        }

        return false;
    }

    public static Shape BuildBridge(Shape lower, Shape upper)
    {
        return TryBuildBridge(lower, upper, out var bridge, out _, out _) ? bridge : lower.With(new Solid(lower.Unit));
    }
}

