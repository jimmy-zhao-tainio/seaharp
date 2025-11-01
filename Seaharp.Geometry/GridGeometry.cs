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

public readonly record struct Point(long X, long Y, long Z)
{
    public Vector3 ToVector3() => new(X, Y, Z);

    public override string ToString() => $"({X}, {Y}, {Z})";
}

public readonly record struct Triangle(Point A, Point B, Point C)
{
    private static Point[] SortVertices(Point a, Point b, Point c)
    {
        var array = new[] { a, b, c };
        Array.Sort(array, PointComparer.Instance);
        return array;
    }

    public IReadOnlyList<Point> CanonicalVertices => SortVertices(A, B, C);

    public IEnumerable<Point> Vertices
    {
        get
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }

    public Triangle Canonicalize()
    {
        var sorted = SortVertices(A, B, C);
        return new Triangle(sorted[0], sorted[1], sorted[2]);
    }

    public override string ToString() => $"[{A}, {B}, {C}]";
}

internal sealed class PointComparer : IComparer<Point>
{
    public static PointComparer Instance { get; } = new();

    public int Compare(Point x, Point y)
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

internal readonly record struct TriangleKey(Point V0, Point V1, Point V2)
{
    public static TriangleKey From(Triangle triangle)
    {
        var sorted = triangle.Canonicalize();
        return new TriangleKey(sorted.A, sorted.B, sorted.C);
    }

    public static TriangleKey From(Point a, Point b, Point c)
    {
        var sorted = new[] { a, b, c };
        Array.Sort(sorted, PointComparer.Instance);
        return new TriangleKey(sorted[0], sorted[1], sorted[2]);
    }
}

public sealed class Tetrahedron : IEquatable<Tetrahedron>
{
    private readonly Point[] _vertices;

    public Tetrahedron(Point a, Point b, Point c, Point d)
    {
        _vertices = new[] { a, b, c, d }
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ThenBy(p => p.Z)
            .ToArray();
    }

    public IReadOnlyList<Point> Vertices => _vertices;

    public bool ContainsPointStrict(Point point, double epsilon = 1e-6)
    {
        var a = _vertices[0];
        var b = _vertices[1];
        var c = _vertices[2];
        var d = _vertices[3];

        var totalVolume = (double)IntegerMath.SignedTetrahedronVolume6(a, b, c, d);
        if (Math.Abs(totalVolume) < epsilon)
        {
            return false;
        }

        var weightA = (double)IntegerMath.SignedTetrahedronVolume6(point, b, c, d) / totalVolume;
        var weightB = (double)IntegerMath.SignedTetrahedronVolume6(a, point, c, d) / totalVolume;
        var weightC = (double)IntegerMath.SignedTetrahedronVolume6(a, b, point, d) / totalVolume;
        var weightD = (double)IntegerMath.SignedTetrahedronVolume6(a, b, c, point) / totalVolume;

        const double sumTolerance = 1e-4;

        if (weightA > epsilon && weightB > epsilon && weightC > epsilon && weightD > epsilon)
        {
            var sum = weightA + weightB + weightC + weightD;
            if (Math.Abs(sum - 1.0) <= sumTolerance)
            {
                return true;
            }
        }

        return false;
    }

    public bool ContainsPointStrict(Vector3 point, double epsilon = 1e-6) =>
        ContainsPointStrict(new Point(
            (long)Math.Round(point.X),
            (long)Math.Round(point.Y),
            (long)Math.Round(point.Z)), epsilon);

    public IEnumerable<Triangle> Faces
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

    private Triangle CreateFace(int i0, int i1, int i2, int oppositeIndex)
    {
        var a = _vertices[i0];
        var b = _vertices[i1];
        var c = _vertices[i2];
        var opposite = _vertices[oppositeIndex];

        var face = new Triangle(a, b, c);
        if (GeometryChecks.IsPointOnPositiveSideOfTrianglePlane(face, opposite))
        {
            face = new Triangle(face.A, face.C, face.B);
        }

        return face;
    }

    private static Vector3 ToVector(Point point)
        => new((float)point.X, (float)point.Y, (float)point.Z);
}

public readonly record struct RotationAngles(double XDegrees, double YDegrees, double ZDegrees);

public sealed class Solid
{
    private readonly HashSet<Tetrahedron> _tetrahedra;

    public Solid(UnitScale unit, IEnumerable<Tetrahedron>? tetrahedrons = null)
    {
        Unit = unit;
        _tetrahedra = tetrahedrons != null
            ? new HashSet<Tetrahedron>(tetrahedrons)
            : new HashSet<Tetrahedron>();
    }

    public UnitScale Unit { get; }

    public IReadOnlyCollection<Tetrahedron> Tetrahedrons => _tetrahedra;

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

        Point TranslatePoint(Point p) => new(p.X + dx, p.Y + dy, p.Z + dz);
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

        Point RotatePoint(Point p)
        {
            var vector = Vector3.Transform(p.ToVector3(), rotationMatrix);
            return new Point(
                (long)Math.Round(vector.X),
                (long)Math.Round(vector.Y),
                (long)Math.Round(vector.Z));
        }
    }

    public bool SharesFaceWith(Solid other)
    {
        EnsureUnitsMatch(other);

        var faces = new HashSet<Triangle>(GetCanonicalFaces(this));
        foreach (var face in GetCanonicalFaces(other))
        {
            if (faces.Contains(face))
            {
                return true;
            }
        }
        return false;
    }

    public IEnumerable<Triangle> Faces => _tetrahedra.SelectMany(t => t.Faces);

    public IEnumerable<Triangle> BoundaryTriangles()
    {
        var counts = new Dictionary<TriangleKey, (int Count, Triangle Triangle)>();

        foreach (var tetra in _tetrahedra)
        {
            foreach (var face in tetra.Faces)
            {
                var key = TriangleKey.From(face);
                if (counts.TryGetValue(key, out var entry))
                {
                    counts[key] = (entry.Count + 1, entry.Triangle);
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
                yield return entry.Triangle;
            }
        }
    }

    private static IEnumerable<Triangle> GetCanonicalFaces(Solid solid) =>
        solid._tetrahedra.SelectMany(t => t.Faces);

    public (Point Min, Point Max) GetBounds()
    {
        if (_tetrahedra.Count == 0)
        {
            return (new Point(0, 0, 0), new Point(0, 0, 0));
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

        return (new Point(minX, minY, minZ), new Point(maxX, maxY, maxZ));
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
        var origin = new Point(0, 0, 0);
        var tetrahedrons = CreateBoxTetrahedrons(origin, width, depth, height);
        return new Solid(unit, tetrahedrons);
    }

    private static IEnumerable<Tetrahedron> CreateBoxTetrahedrons(Point origin, int width, int depth, int height)
    {
        var p000 = origin;
        var p100 = new Point(origin.X + width, origin.Y, origin.Z);
        var p010 = new Point(origin.X, origin.Y + depth, origin.Z);
        var p001 = new Point(origin.X, origin.Y, origin.Z + height);
        var p110 = new Point(origin.X + width, origin.Y + depth, origin.Z);
        var p101 = new Point(origin.X + width, origin.Y, origin.Z + height);
        var p011 = new Point(origin.X, origin.Y + depth, origin.Z + height);
        var p111 = new Point(origin.X + width, origin.Y + depth, origin.Z + height);

        yield return new Tetrahedron(p000, p100, p010, p001);
        yield return new Tetrahedron(p100, p110, p010, p111);
        yield return new Tetrahedron(p100, p010, p001, p111);
        yield return new Tetrahedron(p010, p001, p011, p111);
        yield return new Tetrahedron(p100, p001, p101, p111);
    }
}
