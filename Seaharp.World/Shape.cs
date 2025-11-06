using System;
using System.Collections.Generic;

namespace Seaharp.World;

public abstract partial class Shape
{
    protected readonly List<Seaharp.Geometry.Tetrahedron> tetrahedrons = new();

    public IReadOnlyList<Seaharp.Geometry.Tetrahedron> Tetrahedrons => tetrahedrons;
}
