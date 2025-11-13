using GPoint = Seaharp.Geometry.Point;

namespace Seaharp.World.Tests;

// Shared test shapes used across multiple test classes.
internal sealed class TwoTetsShareEdgeShape : Seaharp.World.Shape
{
    public TwoTetsShareEdgeShape(GPoint a, GPoint b, GPoint c, GPoint d, GPoint e, GPoint f)
    {
        // Two tetrahedra sharing only edge AB (no shared face)
        tetrahedra.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d));
        tetrahedra.Add(new Seaharp.Geometry.Tetrahedron(a, b, e, f));
    }
}

