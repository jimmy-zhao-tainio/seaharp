using System;

namespace Seaharp.Geometry;

internal readonly record struct Int128Vector(Int128 X, Int128 Y, Int128 Z)
{
    public static Int128Vector FromPoints(in Point origin, in Point target) =>
        new(
            (Int128)target.X - (Int128)origin.X,
            (Int128)target.Y - (Int128)origin.Y,
            (Int128)target.Z - (Int128)origin.Z);

    public bool IsZero => X == 0 && Y == 0 && Z == 0;

    public static Int128Vector Cross(in Int128Vector left, in Int128Vector right) =>
        new(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);

    public static Int128 Dot(in Int128Vector left, in Int128Vector right) =>
        left.X * right.X + left.Y * right.Y + left.Z * right.Z;
}
