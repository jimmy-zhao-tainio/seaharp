using System;
using System.Collections.Generic;
using System.Linq;

namespace Seaharp.Geometry;

/// <summary>
/// Finds the first mutually visible pair of boundary triangles between two solids and produces their loft.
/// </summary>
public static class FirstVisibleLoft
{
    /// <summary>
    /// Finds the first pair of boundary triangles between solids <paramref name="A"/> and <paramref name="B"/>
    /// that are mutually visible (strict-side), classify as <see cref="LoftCase.Prism3Tets"/>, and whose loft tetrahedra
    /// do not intersect the remaining boundary triangles of either solid.
    /// </summary>
    public static bool TryFind(
        Solid A,
        Solid B,
        out TriangleFace tA,
        out TriangleFace tB,
        out List<Tetrahedron> tets)
    {
        if (A is null)
        {
            throw new ArgumentNullException(nameof(A));
        }
        if (B is null)
        {
            throw new ArgumentNullException(nameof(B));
        }

        tA = default;
        tB = default;
        tets = new List<Tetrahedron>();

        var facesA = A.BoundaryFaces().ToArray();
        var facesB = B.BoundaryFaces().ToArray();

        foreach (var fa in facesA)
        {
            foreach (var fb in facesB)
            {
                var kind = TriangleLofting.Classify(fa, fb);
                if (kind != LoftCase.Prism3Tets)
                {
                    continue;
                }

                var loft = TriangleLofting.Loft(fa, fb);
                if (loft.Count != 3)
                {
                    continue;
                }

                if (!LoftIsClear(fa, fb, loft, facesA, facesB))
                {
                    continue;
                }

                tA = fa;
                tB = fb;
                tets = loft.ToList();
                return true;
            }
        }

        return false;
    }

    private static bool LoftIsClear(
        TriangleFace tA,
        TriangleFace tB,
        IReadOnlyList<Tetrahedron> tets,
        IReadOnlyList<TriangleFace> allA,
        IReadOnlyList<TriangleFace> allB)
    {
        foreach (var tetra in tets)
        {
            foreach (var face in FacesOf(tetra))
            {
                foreach (var tri in allA)
                {
                    if (SameFace(tri, tA) || SharesVertex(tri, tA))
                    {
                        continue;
                    }
                    if (TriangleLofting.TrianglesIntersect(face, tri))
                    {
                        return false;
                    }
                }

                foreach (var tri in allB)
                {
                    if (SameFace(tri, tB) || SharesVertex(tri, tB))
                    {
                        continue;
                    }
                    if (TriangleLofting.TrianglesIntersect(face, tri))
                    {
                        return false;
                    }
                }
            }
        }

        foreach (var tetra in tets)
        {
            foreach (var vertex in tetra.Vertices)
            {
                foreach (var tri in allA)
                {
                    if (SameFace(tri, tA) || SharesVertex(tri, tA))
                    {
                        continue;
                    }
                    if (Exact.Orient3D(tri.A, tri.B, tri.C, vertex) == 0)
                    {
                        return false;
                    }
                }

                foreach (var tri in allB)
                {
                    if (SameFace(tri, tB) || SharesVertex(tri, tB))
                    {
                        continue;
                    }
                    if (Exact.Orient3D(tri.A, tri.B, tri.C, vertex) == 0)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool SameFace(TriangleFace u, TriangleFace v) => FaceKey.From(u) == FaceKey.From(v);
    private static bool SharesVertex(TriangleFace u, TriangleFace v)
    {
        var vertices = new HashSet<GridPoint>(u.Vertices);
        foreach (var vertex in v.Vertices)
        {
            if (vertices.Contains(vertex))
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<TriangleFace> FacesOf(Tetrahedron tetra)
    {
        var vertices = tetra.Vertices;
        yield return new TriangleFace(vertices[0], vertices[1], vertices[2]);
        yield return new TriangleFace(vertices[0], vertices[2], vertices[3]);
        yield return new TriangleFace(vertices[0], vertices[3], vertices[1]);
        yield return new TriangleFace(vertices[1], vertices[3], vertices[2]);
    }
}
