namespace Seaharp.Geometry.Computation;

// Simple double-precision ray (origin, direction unit vector)
public readonly struct Ray
{
    public readonly double Ox, Oy, Oz;
    public readonly double Dx, Dy, Dz;

    public Ray(double ox, double oy, double oz, double dx, double dy, double dz)
    {
        var len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (len == 0) throw new ArgumentException("Ray direction cannot be zero.");
        Ox = ox; Oy = oy; Oz = oz;
        Dx = dx / len; Dy = dy / len; Dz = dz / len;
    }
}