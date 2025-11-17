using Geometry; // for Triangle
using Topology; // for ClosedSurface

namespace World;

public abstract partial class Shape
{
    // Every shape exposes a closed surface mesh directly.
    public ClosedSurface Mesh { get; protected set; } = new ClosedSurface(Array.Empty<Triangle>());
}