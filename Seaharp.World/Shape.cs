using System;
using System.Collections.Generic;

namespace Seaharp.World;

public abstract partial class Shape
{
    protected readonly List<Seaharp.Geometry.Tetrahedron> tetrahedra = new();
    protected int degenerateSkips = 0;

    public IReadOnlyList<Seaharp.Geometry.Tetrahedron> Tetrahedra => tetrahedra;

    // Number of tetrahedra skipped during construction due to degeneracy
    // (e.g., collinearity/coplanarity after rounding). Debug/diagnostic only.
    public int DegenerateTetrahedra => degenerateSkips;

    // TODO: Consider evolving Shape to produce a ClosedSurface directly (or hold one),
    //       instead of mandating a tetrahedra decomposition. This would simplify IO
    //       (STL export), boolean ops, and reduce duplication between World and Topology.
}
