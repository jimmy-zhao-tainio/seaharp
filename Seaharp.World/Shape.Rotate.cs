using Seaharp.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Seaharp.World;

// Rotation-related APIs for Shape (destructive)
public abstract partial class Shape
{
    // Rotates all tetrahedrons by the given Euler angles (degrees) around X, Y, Z (in that order)
    // and replaces them with new instances.
    public void Rotate(double xDegrees = 0, double yDegrees = 0, double zDegrees = 0)
    {
        if (tetrahedrons.Count == 0) return;

        var m = BuildRotationMatrix(xDegrees, yDegrees, zDegrees);
        var updated = new List<Seaharp.Geometry.Tetrahedron>(tetrahedrons.Count);
        foreach (var t in tetrahedrons)
        {
            var a = RotatePoint(t.A, m);
            var b = RotatePoint(t.B, m);
            var c = RotatePoint(t.C, m);
            var d = RotatePoint(t.D, m);
            updated.Add(new Seaharp.Geometry.Tetrahedron(a, b, c, d));
        }

        tetrahedrons.Clear();
        tetrahedrons.AddRange(updated);

        static Matrix4x4 BuildRotationMatrix(double xDeg, double yDeg, double zDeg)
        {
            float rx = (float)(xDeg * Math.PI / 180.0);
            float ry = (float)(yDeg * Math.PI / 180.0);
            float rz = (float)(zDeg * Math.PI / 180.0);
            var mx = Matrix4x4.CreateRotationX(rx);
            var my = Matrix4x4.CreateRotationY(ry);
            var mz = Matrix4x4.CreateRotationZ(rz);
            // Apply X then Y then Z
            return Matrix4x4.Multiply(Matrix4x4.Multiply(mx, my), mz);
        }

        static Point RotatePoint(in Point p, in Matrix4x4 m)
        {
            var v = new Vector3(p.X, p.Y, p.Z);
            var r = Vector3.Transform(v, m);
            return new Point((long)Math.Round(r.X), (long)Math.Round(r.Y), (long)Math.Round(r.Z));
        }
    }
}
