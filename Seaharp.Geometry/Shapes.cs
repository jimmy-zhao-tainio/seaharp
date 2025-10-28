using System;
using System.Collections.Generic;
using System.Linq;
namespace Seaharp.Geometry;

public class Shape
{
    public Shape(Solid solid)
    {
        Solid = solid ?? throw new ArgumentNullException(nameof(solid));
    }

    public Solid Solid { get; }

    public UnitScale Unit => Solid.Unit;

    public (GridPoint Min, GridPoint Max) Bounds => Solid.GetBounds();

    public Shape Position(long dx = 0, long dy = 0, long dz = 0)
    {
        return new Shape(Solid.Translate(dx, dy, dz));
    }

    public Shape Rotate(double xDegrees = 0, double yDegrees = 0, double zDegrees = 0)
    {
        return Rotate(new RotationAngles(xDegrees, yDegrees, zDegrees));
    }

    public Shape Rotate(RotationAngles angles)
    {
        return new Shape(Solid.Rotate(angles));
    }

    public Shape CombineWith(params Shape[] others)
    {
        if (others == null || others.Length == 0)
        {
            return this;
        }

        var combined = others.Aggregate(Solid, (current, shape) => current.CombineWith(shape.Solid));
        return new Shape(combined);
    }

    public IEnumerable<TriangleFace> Faces(Func<TriangleFace, bool> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        return Solid.BoundaryFaces().Where(predicate);
    }

    public Shape With(Solid solid) => new Shape(solid);

    public static Shape Combine(params Shape[] shapes)
    {
        if (shapes == null || shapes.Length == 0)
        {
            throw new ArgumentException("At least one shape is required.", nameof(shapes));
        }

        var result = shapes[0].Solid;
        for (int i = 1; i < shapes.Length; i++)
        {
            result = result.CombineWith(shapes[i].Solid);
        }
        return new Shape(result);
    }
}

public sealed class Box : Shape
{
    public Box(UnitScale unit, int width, int depth, int height)
        : base(SolidFactory.CreateCuboid(unit, width, depth, height))
    {
    }
}
