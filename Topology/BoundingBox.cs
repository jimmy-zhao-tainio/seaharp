using Geometry;

namespace Topology;

public readonly struct BoundingBox
{
    public readonly Point Min;
    public readonly Point Max;

    public BoundingBox(Point min, Point max)
    {
        Min = min;
        Max = max;
    }

    public static BoundingBox FromPoints(in Point a, in Point b, in Point c)
    {
        long minX = Math.Min(a.X, Math.Min(b.X, c.X));
        long minY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        long minZ = Math.Min(a.Z, Math.Min(b.Z, c.Z));
        long maxX = Math.Max(a.X, Math.Max(b.X, c.X));
        long maxY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
        long maxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));
        return new BoundingBox(new Point(minX, minY, minZ), new Point(maxX, maxY, maxZ));
    }

    public static BoundingBox FromTriangle(in Triangle t)
        => FromPoints(t.P0, t.P1, t.P2);

    public static BoundingBox Union(in BoundingBox a, in BoundingBox b)
        => new BoundingBox(
            new Point(Math.Min(a.Min.X, b.Min.X), Math.Min(a.Min.Y, b.Min.Y), Math.Min(a.Min.Z, b.Min.Z)),
            new Point(Math.Max(a.Max.X, b.Max.X), Math.Max(a.Max.Y, b.Max.Y), Math.Max(a.Max.Z, b.Max.Z)));

    public bool Intersects(in BoundingBox other)
    {
        return !(other.Min.X > Max.X || other.Max.X < Min.X ||
                 other.Min.Y > Max.Y || other.Max.Y < Min.Y ||
                 other.Min.Z > Max.Z || other.Max.Z < Min.Z);
    }

    public static BoundingBox Empty => new BoundingBox(
        new Point(long.MaxValue, long.MaxValue, long.MaxValue),
        new Point(long.MinValue, long.MinValue, long.MinValue));
}
