using Seaharp.Geometry;
using System;
using System.Collections.Generic;

namespace Seaharp.World;

public enum Axis { X, Y, Z }

// Rotation-related APIs for Shape (destructive, 90° steps)
public abstract partial class Shape
{
    // Rotates all tetrahedrons around the given axis by quarterTurns * 90 degrees (right-handed)
    // and replaces them with new instances.
    public void Rotate(Axis axis, int quarterTurns)
    {
        if (tetrahedrons.Count == 0) return;

        int n = Mod4(quarterTurns);
        if (n == 0) return;

        var updated = new List<Seaharp.Geometry.Tetrahedron>(tetrahedrons.Count);
        foreach (var t in tetrahedrons)
        {
            var a = RotatePoint(t.A, axis, n);
            var b = RotatePoint(t.B, axis, n);
            var c = RotatePoint(t.C, axis, n);
            var d = RotatePoint(t.D, axis, n);
            updated.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d));
        }

        tetrahedrons.Clear();
        tetrahedrons.AddRange(updated);

        static int Mod4(int v)
        {
            int m = v % 4;
            return m < 0 ? m + 4 : m;
        }

        static Point RotatePoint(in Point p, Axis axis, int n)
        {
            long x = p.X, y = p.Y, z = p.Z;
            return axis switch
            {
                Axis.X => n switch
                {
                    1 => new Point(x, z, -y),
                    2 => new Point(x, -y, -z),
                    3 => new Point(x, -z, y),
                    _ => p
                },
                Axis.Y => n switch
                {
                    1 => new Point(z, y, -x),
                    2 => new Point(-x, y, -z),
                    3 => new Point(-z, y, x),
                    _ => p
                },
                Axis.Z => n switch
                {
                    1 => new Point(-y, x, z),
                    2 => new Point(-x, -y, z),
                    3 => new Point(y, -x, z),
                    _ => p
                },
                _ => p
            };
        }
    }
}
