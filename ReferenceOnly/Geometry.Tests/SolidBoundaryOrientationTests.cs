using System.Linq;
using System.Numerics;
using Seaharp.Geometry;
using Xunit;

namespace Seaharp.Geometry.Tests;

public class SolidBoundaryOrientationTests
{
    [Fact]
    public void BoxFactory_ProducesOutwardFacingBoundaryTriangles()
    {
        var box = new Box(UnitScale.Millimeter, width: 2, depth: 3, height: 4);

        AssertBoundaryTrianglesFaceOutward(box.Solid);
    }

    [Fact]
    public void SingleTetrahedronSolid_ProducesOutwardFacingBoundaryTriangles()
    {
        var tetrahedron = new Tetrahedron(
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(0, 2, 0),
            new Point(0, 0, 2));

        var solid = new Solid(UnitScale.Millimeter, new[] { tetrahedron });

        AssertBoundaryTrianglesFaceOutward(solid);
    }

    [Fact]
    public void TransformedBox_PreservesBoundaryTriangleOrientation()
    {
        var transformed = new Box(UnitScale.Millimeter, width: 3, depth: 2, height: 5)
            .Rotate(zDegrees: 90)
            .Position(dx: 7, dy: -4, dz: 6);

        AssertBoundaryTrianglesFaceOutward(transformed.Solid);
    }

    private static void AssertBoundaryTrianglesFaceOutward(Solid solid)
    {
        var boundaryTriangles = solid.BoundaryTriangles().ToList();
        Assert.NotEmpty(boundaryTriangles);

        const float tolerance = 1e-4f;

        foreach (var triangle in boundaryTriangles)
        {
            var normal = GeometryChecks.GetTriangleUnitNormal(triangle);
            Assert.True(normal != Vector3.Zero, $"Triangle {triangle} produced a zero normal.");

            var centroid = GeometryChecks.GetTriangleCentroidVector(triangle);
            var owningTetrahedron = solid.Tetrahedrons.FirstOrDefault(t => t.Faces.Any(face => face.Equals(triangle)));
            Assert.True(owningTetrahedron is not null, $"Could not locate tetrahedron for triangle {triangle}.");

            var barycenter = ComputeTetrahedronBarycenter(owningTetrahedron!);
            var directionToExterior = centroid - barycenter;
            Assert.True(directionToExterior.LengthSquared() > tolerance, $"Triangle {triangle} centroid coincides with the owning tetrahedron barycenter.");

            var dot = Vector3.Dot(normal, directionToExterior);
            Assert.True(dot > tolerance, $"Triangle {triangle} normal is not outward-facing (dot={dot}).");
        }
    }

    private static Vector3 ComputeTetrahedronBarycenter(Tetrahedron tetrahedron)
    {
        var sum = Vector3.Zero;
        foreach (var vertex in tetrahedron.Vertices)
        {
            sum += vertex.ToVector3();
        }
        return sum / tetrahedron.Vertices.Count;
    }
}
