using System;
using System.Collections.Generic;

namespace Seaharp.World;

public abstract partial class Shape
{
    protected readonly List<Seaharp.Geometry.Tetrahedron> tetrahedrons = new();
    protected int degenerateSkips = 0;

    public IReadOnlyList<Seaharp.Geometry.Tetrahedron> Tetrahedrons => tetrahedrons;

    // Number of tetrahedrons skipped during construction due to degeneracy
    // (e.g., collinearity/coplanarity after rounding). Debug/diagnostic only.
    public int DegenerateTetrahedrons => degenerateSkips;
}
