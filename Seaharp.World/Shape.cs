using System;
using System.Collections.Generic;

namespace Seaharp.World;

public abstract class Shape
{
    protected readonly List<Seaharp.Geometry.Tetrahedron> tetrahedrons = new();

    public IReadOnlyList<Seaharp.Geometry.Tetrahedron> Tetrahedrons => tetrahedrons;

    // Returns outward-oriented boundary triangles for this shape only.
    // A triangle is considered boundary if no other tetrahedron in this shape
    // has a triangle with the same three vertices (order-independent).
    public IEnumerable<Seaharp.Geometry.Tetrahedron.Triangle> GetBoundaryTriangles()
    {
        var triangles = new List<Seaharp.Geometry.Tetrahedron.Triangle>(tetrahedrons.Count * 4);
        foreach (var t in tetrahedrons)
        {
            triangles.Add(t.ABC);
            triangles.Add(t.ABD);
            triangles.Add(t.ACD);
            triangles.Add(t.BCD);
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            var tri = triangles[i];
            bool shared = false;
            for (int j = 0; j < triangles.Count; j++)
            {
                if (i == j) continue;
                if (SameTriangle(tri, triangles[j]))
                {
                    shared = true; break;
                }
            }
            if (!shared)
            {
                yield return tri;
            }
        }

        static bool SameTriangle(in Seaharp.Geometry.Tetrahedron.Triangle a, in Seaharp.Geometry.Tetrahedron.Triangle b)
        {
            return ContainsAll(a, b.P0, b.P1, b.P2);
        }

        static bool ContainsAll(
            in Seaharp.Geometry.Tetrahedron.Triangle tri,
            in Seaharp.Geometry.Point x,
            in Seaharp.Geometry.Point y,
            in Seaharp.Geometry.Point z)
        {
            int found = 0;
            if (tri.P0.Equals(x) || tri.P1.Equals(x) || tri.P2.Equals(x)) found++;
            if (tri.P0.Equals(y) || tri.P1.Equals(y) || tri.P2.Equals(y)) found++;
            if (tri.P0.Equals(z) || tri.P1.Equals(z) || tri.P2.Equals(z)) found++;
            return found == 3;
        }
    }
}
