using Seaharp.Geometry; // for Triangle
using Seaharp.Topology; // for ClosedSurface

namespace Seaharp.World;

public abstract partial class Shape
{
    // Every shape exposes a closed surface mesh directly.
    public ClosedSurface Mesh { get; protected set; } = new ClosedSurface(Array.Empty<Triangle>());
}