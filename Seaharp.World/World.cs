using System;
using System.Collections.Generic;

namespace Seaharp.World;

public sealed partial class World
{
    private readonly List<Shape> shapes = new();\n\n    public IReadOnlyList<Shape> Shapes => shapes;

    public void Add(Shape shape)
    {
        if (shape is null) throw new ArgumentNullException(nameof(shape));
        shapes.Add(shape);
    }

    // Saving moved to World.Save.cs (STL only)
}

