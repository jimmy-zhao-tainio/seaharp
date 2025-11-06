using System;

namespace Seaharp.Geometry.Bridging;

internal static class BridgeVolumeUtilities
{
    public static Int128 Zero => default;

    public static Int128 MinVolumes(Int128 first, Int128 second, Int128? third = null, Int128? fourth = null)
    {
        var min = first < second ? first : second;
        if (third.HasValue && third.Value < min)
        {
            min = third.Value;
        }
        if (fourth.HasValue && fourth.Value < min)
        {
            min = fourth.Value;
        }
        return min;
    }

    public static Int128 MinVolumes(Int128 first, Int128 second, Int128 third)
    {
        var min = first < second ? first : second;
        if (third < min)
        {
            min = third;
        }
        return min;
    }
}
