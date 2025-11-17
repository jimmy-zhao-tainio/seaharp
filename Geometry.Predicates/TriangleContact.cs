using System;

namespace Geometry.Predicates;

[Flags]
public enum TriangleContactKind
{
    None    = 0,
    Point   = 1 << 0,
    Segment = 1 << 1,
    Area    = 1 << 2
}

public readonly struct TriangleContact
{
    public TriangleContactKind Kind { get; }

    public TriangleContact(TriangleContactKind kind)
    {
        Kind = kind;
    }
}

