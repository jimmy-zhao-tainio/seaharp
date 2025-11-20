using Geometry;
using World;
using Topology;
using Kernel;
using IO;

namespace Demo.Intersections;

internal static class Program
{
    private sealed class TrianglePairShape : Shape
    {
        public TrianglePairShape(in Triangle first, in Triangle second)
        {
            Mesh = new ClosedSurface(new[] { first, second });
        }
    }

    private static void Main()
    {
        SaveNoneExample();
        SavePointExample();
        SaveSegmentExample();
        SaveAreaExample();
        SaveSphereIntersectionExample();
    }

    private static void SaveNoneExample()
    {
        // Coplanar, disjoint triangles (no intersection).
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);

        var b0 = new Point(4, 0, 0);
        var b1 = new Point(6, 0, 0);
        var b2 = new Point(4, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        ValidateIntersection("none", in triA, in triB, IntersectionType.None);
        SavePair("intersection_none.stl", in triA, in triB);
    }

    private static void SavePointExample()
    {
        // One triangle lies in the plane z = 0. A second, vertical triangle
        // has a single vertex touching the interior of the first at its
        // centroid, so the intersection is exactly one point.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        // Centroid of triA is (2,2,0).
        var p = new Point(2, 2, 0);

        var b0 = p;                     // touching point
        var b1 = new Point(2, 2, 3);    // above the plane
        var b2 = new Point(2, 5, 3);    // above the plane, offset in Y

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(2, 2, -1));

        ValidateIntersection("point", in triA, in triB, IntersectionType.Point);
        SavePair("intersection_point.stl", in triA, in triB);
    }

    private static void SaveSegmentExample()
    {
        // Non-coplanar triangles intersecting along a segment on the y-axis
        // from (0,0,0) to (0,1,0).
        var a0 = new Point(0, -1, 0);
        var a1 = new Point(0, 1, 0);
        var a2 = new Point(1, 0, 0);

        var b0 = new Point(0, 0, -1);
        var b1 = new Point(0, 0, 1);
        var b2 = new Point(0, 2, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(1, 0, 0));

        ValidateIntersection("segment", in triA, in triB, IntersectionType.Segment);
        SavePair("intersection_segment.stl", in triA, in triB);
    }

    private static void SaveAreaExample()
    {
        // Coplanar triangles where one "cuts through" the other,
        // producing a clear area overlap that is not containment.
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(6, 0, 0);
        var a2 = new Point(0, 6, 0);

        var b0 = new Point(-2, 4, 0);
        var b1 = new Point(4, -2, 0);
        var b2 = new Point(8, 4, 0);

        var triA = new Triangle(a0, a1, a2, new Point(0, 0, 1));
        var triB = new Triangle(b0, b1, b2, new Point(0, 0, 1));

        ValidateIntersection("area", in triA, in triB, IntersectionType.Area);
        SavePair("intersection_area.stl", in triA, in triB);
    }

    private static void SaveSphereIntersectionExample()
    {
        long r = 200;
        var aCenter = new Point(0, 0, 0);
        var bCenter = new Point(150, 50, -30);

        var sphereA = new Sphere(r, subdivisions: 3, center: aCenter);
        var sphereB = new Sphere(r, subdivisions: 3, center: bCenter);

        var set = new IntersectionSet(
            sphereA.Mesh.Triangles,
            sphereB.Mesh.Triangles);

        var involved = new List<Triangle>();
        var seenA = new HashSet<int>();
        var seenB = new HashSet<int>();

        foreach (var intersection in set.Intersections)
        {
            if (seenA.Add(intersection.TriangleIndexA))
            {
                involved.Add(set.TrianglesA[intersection.TriangleIndexA]);
            }

            if (seenB.Add(intersection.TriangleIndexB))
            {
                involved.Add(set.TrianglesB[intersection.TriangleIndexB]);
            }
        }

        var outputPath = "spheres_intersection_set.stl";
        StlWriter.Write(involved, outputPath);
    }

    private static void ValidateIntersection(string label, in Triangle first, in Triangle second, IntersectionType expected)
    {
        var actual = IntersectionTypes.Classify(in first, in second);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Demo '{label}' expected {expected} but Intersection.Classify returned {actual}.");
        }
    }

    private static void SavePair(string path, in Triangle first, in Triangle second)
    {
        var world = new World.World();
        world.Add(new TrianglePairShape(in first, in second));
        world.Save(path);
    }
}
