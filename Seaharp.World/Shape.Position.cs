using Seaharp.Geometry;
using System.Collections.Generic;

namespace Seaharp.World;

// Positioning-related APIs for Shape (destructive)
public abstract partial class Shape
{
    // Translates all tetrahedrons by the given delta and replaces them with new instances.
    public void Position(long dx, long dy, long dz)
    {
        if (tetrahedrons.Count == 0) return;

        var updated = new List<Seaharp.Geometry.Tetrahedron>(tetrahedrons.Count);
        foreach (var t in tetrahedrons)
        {
            var a = new Point(t.A.X + dx, t.A.Y + dy, t.A.Z + dz);
            var b = new Point(t.B.X + dx, t.B.Y + dy, t.B.Z + dz);
            var c = new Point(t.C.X + dx, t.C.Y + dy, t.C.Z + dz);
            var d = new Point(t.D.X + dx, t.D.Y + dy, t.D.Z + dz);
            updated.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d));
        }

        tetrahedrons.Clear();
        tetrahedrons.AddRange(updated);
    }
}
