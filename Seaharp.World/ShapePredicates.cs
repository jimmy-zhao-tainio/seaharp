using Seaharp.CSG;

namespace Seaharp.World;

public static class ShapePredicates
{
    // Valid when the extracted surface is manifold
    public static bool IsValid(Shape shape) => SurfacePredicates.IsManifold(shape.ToSurface());
    public static bool HasCoplanarEdgeConflicts(Shape shape) => !IsValid(shape);
}







