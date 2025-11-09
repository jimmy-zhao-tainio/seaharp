using System;

namespace Seaharp.Geometry;

// Axis-aligned bounding box in integer grid space (long).
public readonly struct AabbL
{
    public readonly long MinX, MinY, MinZ;
    public readonly long MaxX, MaxY, MaxZ;

    public AabbL(long minX, long minY, long minZ, long maxX, long maxY, long maxZ)
    {
        MinX = minX; MinY = minY; MinZ = minZ;
        MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
    }

    public static AabbL FromPoints(in Point a, in Point b, in Point c)
    {
        long minX = Math.Min(a.X, Math.Min(b.X, c.X));
        long minY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        long minZ = Math.Min(a.Z, Math.Min(b.Z, c.Z));
        long maxX = Math.Max(a.X, Math.Max(b.X, c.X));
        long maxY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
        long maxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));
        return new AabbL(minX, minY, minZ, maxX, maxY, maxZ);
    }

    public static AabbL FromTriangle(in Triangle t)
        => FromPoints(t.P0, t.P1, t.P2);

    public bool Overlaps(in AabbL o)
    {
        if (MaxX < o.MinX || o.MaxX < MinX) return false;
        if (MaxY < o.MinY || o.MaxY < MinY) return false;
        if (MaxZ < o.MinZ || o.MaxZ < MinZ) return false;
        return true;
    }
}

