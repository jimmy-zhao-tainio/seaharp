using System;

namespace Seaharp.Geometry;

public static class TetrahedronOperations
{
    // Returns six times the absolute volume of the tetrahedron.
    public static Int128 AbsoluteVolume6(Tetrahedron tetrahedron)
    {
        var vertices = tetrahedron.Vertices;
        if (vertices.Count != 4)
        {
            throw new InvalidOperationException("A tetrahedron must expose exactly four vertices.");
        }

        return IntegerMath.AbsoluteTetrahedronVolume6(
            vertices[0],
            vertices[1],
            vertices[2],
            vertices[3]);
    }
}
