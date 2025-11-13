using Seaharp.Geometry;
using System;
using System.Collections.Generic;

namespace Seaharp.World;

// Rotation-related APIs for Shape (destructive)
public abstract partial class Shape
{
    // Rotates all tetrahedra by the given Euler angles (degrees) around X, Y, Z (in that order)
    // using double-precision math and integer rounding, then replaces them with new instances.
    // Also checks that no two distinct original vertices collide to the same rounded grid point.
    public void Rotate(double xDegrees = 0, double yDegrees = 0, double zDegrees = 0)
    {
        if (tetrahedra.Count == 0) return;

        // Precompute sines/cosines (radians)
        double rx = xDegrees * Math.PI / 180.0;
        double ry = yDegrees * Math.PI / 180.0;
        double rz = zDegrees * Math.PI / 180.0;
        double cx = Math.Cos(rx), sx = Math.Sin(rx);
        double cy = Math.Cos(ry), sy = Math.Sin(ry);
        double cz = Math.Cos(rz), sz = Math.Sin(rz);

        // Map unique original vertices -> rotated vertices
        var origToRot = new Dictionary<Point, Point>();
        var rotToOrig = new Dictionary<Point, Point>();

        foreach (var t in tetrahedra)
        {
            MapVertex(t.A);
            MapVertex(t.B);
            MapVertex(t.C);
            MapVertex(t.D);
        }

        // Rebuild tetrahedra from mapped vertices
        var updated = new List<Seaharp.Geometry.Tetrahedron>(tetrahedra.Count);
        foreach (var t in tetrahedra)
        {
            var a = origToRot[t.A];
            var b = origToRot[t.B];
            var c = origToRot[t.C];
            var d = origToRot[t.D];
            updated.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d));
        }

        tetrahedra.Clear();
        tetrahedra.AddRange(updated);

        void MapVertex(in Point p)
        {
            if (origToRot.ContainsKey(p)) return;

            // Apply X then Y then Z rotations using double precision
            double x = p.X, y = p.Y, z = p.Z;
            // Rotate around X
            double y1 = y * cx - z * sx;
            double z1 = y * sx + z * cx;
            double x1 = x;
            // Rotate around Y
            double x2 = x1 * cy + z1 * sy;
            double z2 = -x1 * sy + z1 * cy;
            double y2 = y1;
            // Rotate around Z
            double x3 = x2 * cz - y2 * sz;
            double y3 = x2 * sz + y2 * cz;
            double z3 = z2;

            var r = new Point((long)Math.Round(x3), (long)Math.Round(y3), (long)Math.Round(z3));

            if (rotToOrig.TryGetValue(r, out var existing) && !existing.Equals(p))
            {
                throw new InvalidOperationException($"Rotation maps distinct vertices {existing} and {p} to the same grid point {r}.");
            }
            rotToOrig[r] = p;
            origToRot[p] = r;
        }
    }
}
