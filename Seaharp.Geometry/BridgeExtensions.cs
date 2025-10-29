using System;
using System.Collections.Generic;
using System.Linq;

namespace Seaharp.Geometry;

public static class BridgeExtensions
{
    public static bool TryBridge(Shape lower, Shape upper, out Shape bridge, out TriangleFace lowerFace, out TriangleFace upperFace)
    {
        if (lower is null) throw new ArgumentNullException(nameof(lower));
        if (upper is null) throw new ArgumentNullException(nameof(upper));
        if (lower.Unit != upper.Unit) throw new InvalidOperationException("Mismatched units.");

        bridge = lower;
        lowerFace = default;
        upperFace = default;

        var lowerTop = lower.Faces(f => f.Vertices.All(v => v.Z == lower.Bounds.Max.Z)).ToArray();
        var upperBottom = upper.Faces(f => f.Vertices.All(v => v.Z == upper.Bounds.Min.Z)).ToArray();

        var allA = lower.Solid.BoundaryFaces().ToArray();
        var allB = upper.Solid.BoundaryFaces().ToArray();

        foreach (var lf in lowerTop)
        {
            foreach (var uf in upperBottom)
            {
                if (TriangleLofting.Classify(lf, uf) != LoftCase.Prism3Tets)
                {
                    continue;
                }

                var loft = TriangleLofting.Loft(lf, uf);
                if (loft.Count != 3)
                {
                    continue;
                }

                if (!FirstVisibleLoft.LoftIsClear(lf, uf, loft, allA, allB))
                {
                    continue;
                }

                bridge = lower.With(new Solid(lower.Unit, loft));
                lowerFace = lf;
                upperFace = uf;
                return true;
            }
        }

        return false;
    }

    public static Shape Bridge(Shape lower, Shape upper)
    {
        return TryBridge(lower, upper, out var bridge, out _, out _) ? bridge : lower.With(new Solid(lower.Unit));
    }
}

