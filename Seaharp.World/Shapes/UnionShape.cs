using Seaharp.Topology;

namespace Seaharp.World;

// Shape that represents the boolean union of two shapes/surfaces.
// Uses MeshBoolean internally and exposes a ClosedSurface via Mesh.
public sealed class UnionShape : Shape
{
    private readonly ClosedSurface a;
    private readonly ClosedSurface b;
    private ClosedSurface? union;

    public UnionShape(Shape left, Shape right)
    {
        a = left.Mesh;
        b = right.Mesh;
    }

    public UnionShape(ClosedSurface left, ClosedSurface right)
    {
        a = left; b = right;
    }

    public override ClosedSurface Mesh
    {
        get
        {
            if (union is null)
            {
                union = MeshBoolean.Union(a, b);
            }
            return union;
        }
    }
}

