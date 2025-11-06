using System;

namespace Seaharp.Geometry;

public static class Units
{
    private static UnitScale _current = UnitScale.Millimeter;

    public static UnitScale Current
    {
        get => _current;
        set => _current = value;
    }
}

