using Seaharp.Topology;

namespace Seaharp.World;

public static class ShapePredicates
{
    // Valid when the extracted surface is manifold
    public static bool IsValid(Shape shape) => SurfacePredicates.IsManifold(shape.ExtractSurface());
    public static bool HasCoplanarEdgeConflicts(Shape shape) => !IsValid(shape);
}







