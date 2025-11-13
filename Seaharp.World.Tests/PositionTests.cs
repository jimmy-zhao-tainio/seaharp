using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Geometry;

namespace Seaharp.World.Tests;

public class PositionTests
{
    [Fact]
    public void Position_TranslatesAllVertices_Exactly()
    {
        var shape = new Box(width: 3, depth: 2, height: 4);
        var before = Snapshot(shape.Tetrahedra);

        long dx = 10, dy = -5, dz = 7;
        shape.Position(dx, dy, dz);
        var after = shape.Tetrahedra;

        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            var t = after[i];
            Assert.Equal(new Point(before[i].A.X + dx, before[i].A.Y + dy, before[i].A.Z + dz), t.A);
            Assert.Equal(new Point(before[i].B.X + dx, before[i].B.Y + dy, before[i].B.Z + dz), t.B);
            Assert.Equal(new Point(before[i].C.X + dx, before[i].C.Y + dy, before[i].C.Z + dz), t.C);
            Assert.Equal(new Point(before[i].D.X + dx, before[i].D.Y + dy, before[i].D.Z + dz), t.D);
        }
    }

    [Fact]
    public void Position_Reversible_RestoresExactTetrahedra()
    {
        var shape = new Box(width: 5, depth: 3, height: 2);
        var before = Snapshot(shape.Tetrahedra);

        long dx = 123, dy = 456, dz = -789;
        shape.Position(dx, dy, dz);
        shape.Position(-dx, -dy, -dz);

        var after = shape.Tetrahedra;
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].A, after[i].A);
            Assert.Equal(before[i].B, after[i].B);
            Assert.Equal(before[i].C, after[i].C);
            Assert.Equal(before[i].D, after[i].D);
        }
    }

    private static List<(Point A, Point B, Point C, Point D)> Snapshot(IReadOnlyList<Seaharp.Geometry.Tetrahedron> tets)
    {
        var copy = new List<(Point, Point, Point, Point)>(tets.Count);
        for (int i = 0; i < tets.Count; i++)
        {
            var t = tets[i];
            copy.Add((t.A, t.B, t.C, t.D));
        }
        return copy;
    }
}

