namespace Geometry;

public readonly struct RealPoint
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    public RealPoint(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public RealPoint(Point p)
    {
        X = p.X;
        Y = p.Y;
        Z = p.Z;
    }
}
