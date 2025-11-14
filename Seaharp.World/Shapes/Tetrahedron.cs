using Seaharp.Topology;

namespace Seaharp.World;

public sealed class Tetrahedron : Shape
{
    public Tetrahedron(Seaharp.Geometry.Point a, Seaharp.Geometry.Point b, Seaharp.Geometry.Point c, Seaharp.Geometry.Point d)
    {
        A = a; B = b; C = c; D = d;
        var tetrahedron = new Seaharp.Geometry.Tetrahedron(A, B, C, D);
        Mesh = ClosedSurface.FromTetrahedra(new[] { tetrahedron });
    }

    public Seaharp.Geometry.Point A { get; }
    public Seaharp.Geometry.Point B { get; }
    public Seaharp.Geometry.Point C { get; }
    public Seaharp.Geometry.Point D { get; }
}
