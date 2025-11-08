using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World;

// Surface-related APIs for Shape
public abstract partial class Shape
{
    // Returns outward-oriented boundary triangles for this shape only.
    // A surface is the set of boundary triangles: any triangle not shared by
    // another tetrahedron in the same shape (vertex-set equality, order-agnostic).
    public IEnumerable<Seaharp.Geometry.Tetrahedron.Triangle> GetSurface()
    {
        var map = new Dictionary<TriangleKey, (int count, Seaharp.Geometry.Tetrahedron.Triangle tri)>(tetrahedrons.Count * 4);

        void Acc(in Seaharp.Geometry.Tetrahedron.Triangle t)
        {
            var key = TriangleKey.FromTriangle(t);
            if (map.TryGetValue(key, out var e)) map[key] = (e.count + 1, e.tri);
            else map[key] = (1, t);
        }

        foreach (var t in tetrahedrons)
        {
            Acc(t.ABC);
            Acc(t.ABD);
            Acc(t.ACD);
            Acc(t.BCD);
        }

        foreach (var kv in map)
        {
            if (kv.Value.count == 1)
                yield return kv.Value.tri;
        }
    }
}

