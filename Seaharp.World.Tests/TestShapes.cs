using Seaharp.Geometry;

namespace Seaharp.World.Tests;

// Shared test shapes used across multiple test classes.
internal sealed class TwoTetsShareEdgeShape : Seaharp.World.Shape
{
    public TwoTetsShareEdgeShape(Point a, Point b, Point c, Point d, Point e, Point f)
    {
        // Two tetrahedra sharing only edge AB (no shared face)
        tetrahedra.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d));
        tetrahedra.Add(new Seaharp.Geometry.Tetrahedron(a, b, e, f));
    }
}

