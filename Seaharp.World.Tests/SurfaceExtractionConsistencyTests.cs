using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Geometry;

namespace Seaharp.World.Tests;

public class SurfaceExtractionConsistencyTests
{
    [Fact]
    public void Box_Surface_Matches_OldEqualityScan()
        => AssertConsistent(new Box(4, 3, 2));

    [Fact]
    public void Sphere_Surface_Matches_OldEqualityScan()
        => AssertConsistent(new Sphere(radius: 5, subdivisions: 1));

    [Fact]
    public void Cylinder_Surface_Matches_OldEqualityScan()
        => AssertConsistent(new Cylinder(radius: 6, thickness: 2, height: 6, segments: 16));

    private static void AssertConsistent(Shape shape)
    {
        // New path: via Surface snapshot keyed by TriangleKey
        var viaKey = new HashSet<TriangleKey>();
        foreach (var t in new Surface(shape).Triangles) viaKey.Add(TriangleKey.FromTriangle(t));

        // Old path: O(n^2) pairwise equality scan using TrianglePredicates.IsSame
        var all = new List<Seaharp.Geometry.Tetrahedron.Triangle>(shape.Tetrahedrons.Count * 4);
        foreach (var tet in shape.Tetrahedrons)
        {
            all.Add(tet.ABC); all.Add(tet.ABD); all.Add(tet.ACD); all.Add(tet.BCD);
        }

        var counts = new Dictionary<TriangleKey, int>(all.Count);
        for (int i = 0; i < all.Count; i++)
        {
            var keyI = TriangleKey.FromTriangle(all[i]);
            int c = 0;
            for (int j = 0; j < all.Count; j++)
            {
                if (i == j) continue;
                var keyJ = TriangleKey.FromTriangle(all[j]);
                if (keyI.Equals(keyJ)) { c++; break; }
            }
            if (c == 0)
            {
                if (counts.TryGetValue(keyI, out var k)) counts[keyI] = k + 1; else counts[keyI] = 1;
            }
        }

        var viaScan = new HashSet<TriangleKey>();
        foreach (var kv in counts) if (kv.Value >= 1) viaScan.Add(kv.Key);

        Assert.Equal(viaScan.Count, viaKey.Count);
        Assert.True(viaKey.IsSubsetOf(viaScan));
        Assert.True(viaScan.IsSubsetOf(viaKey));
    }
}
