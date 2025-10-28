using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Seaharp.Geometry;

public enum UnitScale
{
    Micrometer,
    Millimeter
}

public static class UnitScaleExtensions
{
    public static double ToMetersFactor(this UnitScale unit) =>
        unit switch
        {
            UnitScale.Micrometer => 1e-6,
            UnitScale.Millimeter => 1e-3,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported unit scale.")
        };

    public static double ToMillimetersFactor(this UnitScale unit) =>
        unit switch
        {
            UnitScale.Micrometer => 0.001,
            UnitScale.Millimeter => 1.0,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported unit scale.")
        };

    public static string Abbreviation(this UnitScale unit) =>
        unit switch
        {
            UnitScale.Micrometer => "µm",
            UnitScale.Millimeter => "mm",
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported unit scale.")
        };
}

public readonly record struct GridPoint(long X, long Y, long Z)
{
    public Vector3 ToVector3() => new(X, Y, Z);

    public override string ToString() => $"({X}, {Y}, {Z})";
}

public readonly record struct TriangleFace(GridPoint A, GridPoint B, GridPoint C)
{
    private static GridPoint[] SortVertices(GridPoint a, GridPoint b, GridPoint c)
    {
        var array = new[] { a, b, c };
        Array.Sort(array, GridPointComparer.Instance);
        return array;
    }

    public IReadOnlyList<GridPoint> CanonicalVertices => SortVertices(A, B, C);

    public IEnumerable<GridPoint> Vertices
    {
        get
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }

    public TriangleFace Canonicalize()
    {
        var sorted = SortVertices(A, B, C);
        return new TriangleFace(sorted[0], sorted[1], sorted[2]);
    }

    public override string ToString() => $"[{A}, {B}, {C}]";
}

internal sealed class GridPointComparer : IComparer<GridPoint>
{
    public static GridPointComparer Instance { get; } = new();

    public int Compare(GridPoint x, GridPoint y)
    {
        var result = x.X.CompareTo(y.X);
        if (result != 0)
        {
            return result;
        }
        result = x.Y.CompareTo(y.Y);
        if (result != 0)
        {
            return result;
        }
        return x.Z.CompareTo(y.Z);
    }
}

internal readonly record struct FaceKey(GridPoint V0, GridPoint V1, GridPoint V2)
{
    public static FaceKey From(TriangleFace face)
    {
        var sorted = face.Canonicalize();
        return new FaceKey(sorted.A, sorted.B, sorted.C);
    }

    public static FaceKey From(GridPoint a, GridPoint b, GridPoint c)
    {
        var sorted = new[] { a, b, c };
        Array.Sort(sorted, GridPointComparer.Instance);
        return new FaceKey(sorted[0], sorted[1], sorted[2]);
    }
}

public sealed class Tetrahedron : IEquatable<Tetrahedron>
{
    private readonly GridPoint[] _vertices;

    public Tetrahedron(GridPoint a, GridPoint b, GridPoint c, GridPoint d)
    {
        _vertices = new[] { a, b, c, d }
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ThenBy(p => p.Z)
            .ToArray();
    }

    public IReadOnlyList<GridPoint> Vertices => _vertices;

    public bool ContainsPointStrict(GridPoint point, double epsilon = 1e-6) =>
        ContainsPointStrict(point.ToVector3(), epsilon);

    public bool ContainsPointStrict(Vector3 point, double epsilon = 1e-6)
    {
        var a = _vertices[0].ToVector3();
        var b = _vertices[1].ToVector3();
        var c = _vertices[2].ToVector3();
        var d = _vertices[3].ToVector3();

        var volume = SignedVolume(a, b, c, d);
        if (Math.Abs(volume) < epsilon)
        {
            return false;
        }

        var wa = SignedVolume(point, b, c, d) / volume;
        var wb = SignedVolume(a, point, c, d) / volume;
        var wc = SignedVolume(a, b, point, d) / volume;
        var wd = SignedVolume(a, b, c, point) / volume;

        const double sumTolerance = 1e-4;

        if (wa > epsilon && wb > epsilon && wc > epsilon && wd > epsilon)
        {
            var sum = wa + wb + wc + wd;
            if (Math.Abs(sum - 1.0) <= sumTolerance)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<TriangleFace> Faces
    {
        get
        {
            yield return CreateFace(0, 1, 2, 3);
            yield return CreateFace(0, 1, 3, 2);
            yield return CreateFace(0, 2, 3, 1);
            yield return CreateFace(1, 2, 3, 0);
        }
    }

    public bool Equals(Tetrahedron? other)
    {
        if (other is null)
        {
            return false;
        }

        for (var i = 0; i < 4; i++)
        {
            if (_vertices[i] != other._vertices[i])
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as Tetrahedron);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var vertex in _vertices)
        {
            hash.Add(vertex);
        }
        return hash.ToHashCode();
    }

    private static double SignedVolume(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var abx = (double)b.X - a.X;
        var aby = (double)b.Y - a.Y;
        var abz = (double)b.Z - a.Z;

        var acx = (double)c.X - a.X;
        var acy = (double)c.Y - a.Y;
        var acz = (double)c.Z - a.Z;

        var adx = (double)d.X - a.X;
        var ady = (double)d.Y - a.Y;
        var adz = (double)d.Z - a.Z;

        var crossX = aby * acz - abz * acy;
        var crossY = abz * acx - abx * acz;
        var crossZ = abx * acy - aby * acx;

        return crossX * adx + crossY * ady + crossZ * adz;
    }

    private TriangleFace CreateFace(int i0, int i1, int i2, int oppositeIndex)
    {
        var a = _vertices[i0];
        var b = _vertices[i1];
        var c = _vertices[i2];
        var opposite = _vertices[oppositeIndex];

        var normal = Vector3.Cross(ToVector(b) - ToVector(a), ToVector(c) - ToVector(a));
        if (Vector3.Dot(normal, ToVector(opposite) - ToVector(a)) > 0)
        {
            (b, c) = (c, b);
        }

        return new TriangleFace(a, b, c);
    }

    private static Vector3 ToVector(GridPoint point)
        => new((float)point.X, (float)point.Y, (float)point.Z);
}

public readonly record struct RotationAngles(double XDegrees, double YDegrees, double ZDegrees);

public sealed class Solid
{
    private readonly HashSet<Tetrahedron> _tetrahedra;

    public Solid(UnitScale unit, IEnumerable<Tetrahedron>? tetrahedra = null)
    {
        Unit = unit;
        _tetrahedra = tetrahedra != null
            ? new HashSet<Tetrahedron>(tetrahedra)
            : new HashSet<Tetrahedron>();
    }

    public UnitScale Unit { get; }

    public IReadOnlyCollection<Tetrahedron> Tetrahedra => _tetrahedra;

    public Solid Add(Tetrahedron tetra)
    {
        _tetrahedra.Add(tetra);
        return this;
    }

    public Solid Merge(Solid other)
    {
        EnsureUnitsMatch(other);
        foreach (var tetra in other._tetrahedra)
        {
            _tetrahedra.Add(tetra);
        }
        return this;
    }

    public Solid CombineWith(Solid other)
    {
        EnsureUnitsMatch(other);
        return new Solid(Unit, _tetrahedra.Concat(other._tetrahedra));
    }

    public Solid Translate(long dx, long dy, long dz)
    {
        var translated = _tetrahedra
            .Select(t => new Tetrahedron(
                TranslatePoint(t.Vertices[0]),
                TranslatePoint(t.Vertices[1]),
                TranslatePoint(t.Vertices[2]),
                TranslatePoint(t.Vertices[3])))
            .ToList();

        return new Solid(Unit, translated);

        GridPoint TranslatePoint(GridPoint p) => new(p.X + dx, p.Y + dy, p.Z + dz);
    }

    public Solid Rotate(RotationAngles angles)
    {
        var rotationMatrix = BuildRotationMatrix(angles);
        var rotated = _tetrahedra
            .Select(t => new Tetrahedron(
                RotatePoint(t.Vertices[0]),
                RotatePoint(t.Vertices[1]),
                RotatePoint(t.Vertices[2]),
                RotatePoint(t.Vertices[3])))
            .ToList();

        return new Solid(Unit, rotated);

        GridPoint RotatePoint(GridPoint p)
        {
            var vector = Vector3.Transform(p.ToVector3(), rotationMatrix);
            return new GridPoint(
                (long)Math.Round(vector.X),
                (long)Math.Round(vector.Y),
                (long)Math.Round(vector.Z));
        }
    }

    public bool SharesFaceWith(Solid other)
    {
        EnsureUnitsMatch(other);

        var faces = new HashSet<TriangleFace>(GetCanonicalFaces(this));
        foreach (var face in GetCanonicalFaces(other))
        {
            if (faces.Contains(face))
            {
                return true;
            }
        }
        return false;
    }

    public IEnumerable<TriangleFace> Faces => _tetrahedra.SelectMany(t => t.Faces);

    public IEnumerable<TriangleFace> BoundaryFaces()
    {
        var counts = new Dictionary<FaceKey, (int Count, TriangleFace Face)>();

        foreach (var tetra in _tetrahedra)
        {
            foreach (var face in tetra.Faces)
            {
                var key = FaceKey.From(face);
                if (counts.TryGetValue(key, out var entry))
                {
                    counts[key] = (entry.Count + 1, entry.Face);
                }
                else
                {
                    counts[key] = (1, face);
                }
            }
        }

        foreach (var entry in counts.Values)
        {
            if (entry.Count == 1)
            {
                yield return entry.Face;
            }
        }
    }

    private static IEnumerable<TriangleFace> GetCanonicalFaces(Solid solid) =>
        solid._tetrahedra.SelectMany(t => t.Faces);

    public (GridPoint Min, GridPoint Max) GetBounds()
    {
        if (_tetrahedra.Count == 0)
        {
            return (new GridPoint(0, 0, 0), new GridPoint(0, 0, 0));
        }

        long minX = long.MaxValue, minY = long.MaxValue, minZ = long.MaxValue;
        long maxX = long.MinValue, maxY = long.MinValue, maxZ = long.MinValue;

        foreach (var tetra in _tetrahedra)
        {
            foreach (var vertex in tetra.Vertices)
            {
                if (vertex.X < minX) minX = vertex.X;
                if (vertex.Y < minY) minY = vertex.Y;
                if (vertex.Z < minZ) minZ = vertex.Z;

                if (vertex.X > maxX) maxX = vertex.X;
                if (vertex.Y > maxY) maxY = vertex.Y;
                if (vertex.Z > maxZ) maxZ = vertex.Z;
            }
        }

        return (new GridPoint(minX, minY, minZ), new GridPoint(maxX, maxY, maxZ));
    }

    private void EnsureUnitsMatch(Solid other)
    {
        if (Unit != other.Unit)
        {
            throw new InvalidOperationException("Cannot combine solids with mismatched units.");
        }
    }

    private static Matrix4x4 BuildRotationMatrix(RotationAngles angles)
    {
        var rx = Matrix4x4.CreateRotationX(DegreesToRadians(angles.XDegrees));
        var ry = Matrix4x4.CreateRotationY(DegreesToRadians(angles.YDegrees));
        var rz = Matrix4x4.CreateRotationZ(DegreesToRadians(angles.ZDegrees));
        return Matrix4x4.Multiply(Matrix4x4.Multiply(rx, ry), rz);
    }

    private static float DegreesToRadians(double degrees) => (float)(degrees * Math.PI / 180.0);
}

public static class SolidFactory
{
    public static Solid CreateCuboid(UnitScale unit, int width, int depth, int height)
    {
        var origin = new GridPoint(0, 0, 0);
        var tetrahedra = BoxTetrahedra(origin, width, depth, height);
        return new Solid(unit, tetrahedra);
    }

    private static IEnumerable<Tetrahedron> BoxTetrahedra(GridPoint origin, int width, int depth, int height)
    {
        var p000 = origin;
        var p100 = new GridPoint(origin.X + width, origin.Y, origin.Z);
        var p010 = new GridPoint(origin.X, origin.Y + depth, origin.Z);
        var p001 = new GridPoint(origin.X, origin.Y, origin.Z + height);
        var p110 = new GridPoint(origin.X + width, origin.Y + depth, origin.Z);
        var p101 = new GridPoint(origin.X + width, origin.Y, origin.Z + height);
        var p011 = new GridPoint(origin.X, origin.Y + depth, origin.Z + height);
        var p111 = new GridPoint(origin.X + width, origin.Y + depth, origin.Z + height);

        yield return new Tetrahedron(p000, p100, p010, p001);
        yield return new Tetrahedron(p100, p110, p010, p111);
        yield return new Tetrahedron(p100, p010, p001, p111);
        yield return new Tetrahedron(p010, p001, p011, p111);
        yield return new Tetrahedron(p100, p001, p101, p111);
    }
}
