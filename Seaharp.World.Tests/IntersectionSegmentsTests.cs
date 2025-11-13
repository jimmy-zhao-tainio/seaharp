using System;
using System.Collections.Generic;
using Xunit;
using Seaharp.World;
using Seaharp.Topology;
using Seaharp.Geometry;
using System.Linq;

namespace Seaharp.World.Tests;

public class IntersectionSegmentsTests
{
    [Fact]
    public void TwoSpheres_SlightlyUnaligned_ProducesClosedGridLoops()
    {
        // Two equal spheres with a non-axis-aligned offset to make the circle skewed on grid
        var centerA = new Point(0, 0, 0);
        var centerB = new Point(35, 11, -7); // not axis-aligned
        var r = 80;

        var a = new Sphere(r, subdivisions: 3, center: centerA);
        var b = new Sphere(r, subdivisions: 3, center: centerB);

        var surfaceA = ClosedSurface.FromTetrahedra(a.Tetrahedra);
        var surfaceB = ClosedSurface.FromTetrahedra(b.Tetrahedra);

        var loops = IntersectionSegments.BuildLoops(surfaceA, surfaceB);

        Assert.NotNull(loops);
        Assert.NotEmpty(loops); // Should intersect -> at least one loop

        foreach (var loop in loops)
        {
            Assert.True(loop.Count >= 4, "Closed loop must have at least 3 edges (first==last)");
            Assert.Equal(loop[0], loop[^1]);

            // Ensure points are snapped to grid (type is Point) and loop vertices (except duplicated last) are unique enough
            var seen = new HashSet<Point>();
            for (int i = 0; i < loop.Count - 1; i++)
            {
                Assert.True(seen.Add(loop[i]), "Loop contains duplicate consecutive vertices");
            }
        }
    }

    [Fact]
    public void TwoSpheres_Intersection_IsCircular_WithExpectedRadius()
    {
        var centerA = new Point(0, 0, 0);
        var centerB = new Point(35, 11, -7); // non-axis aligned
        var r = 80;

        var a = new Sphere(r, subdivisions: 3, center: centerA);
        var b = new Sphere(r, subdivisions: 3, center: centerB);

        var surfaceA = ClosedSurface.FromTetrahedra(a.Tetrahedra);
        var surfaceB = ClosedSurface.FromTetrahedra(b.Tetrahedra);

        var loops = IntersectionSegments.BuildLoops(surfaceA, surfaceB);
        Assert.NotEmpty(loops);

        // Choose the largest loop as the circle
        var loop = loops.OrderByDescending(x => x.Count).First();
        Assert.True(loop.Count >= 12, "Intersection loop should have enough samples");
        Assert.Equal(loop[0], loop[^1]);

        // Theoretical circle: plane normal is along center delta, center at midpoint
        double dx = centerB.X - centerA.X;
        double dy = centerB.Y - centerA.Y;
        double dz = centerB.Z - centerA.Z;
        double d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        Assert.True(d > 0 && d < 2 * r, "Spheres should intersect in a circle");

        double nx = dx / d, ny = dy / d, nz = dz / d; // unit normal
        double mx = (centerA.X + centerB.X) * 0.5;
        double my = (centerA.Y + centerB.Y) * 0.5;
        double mz = (centerA.Z + centerB.Z) * 0.5;

        double expectedR = Math.Sqrt(r * r - (d * 0.5) * (d * 0.5));

        // Analyze distances to plane and circle radius
        var radii = new double[loop.Count - 1];
        double maxPlaneOff = 0.0;
        for (int i = 0; i < loop.Count - 1; i++)
        {
            var p = loop[i];
            double vx = p.X - mx;
            double vy = p.Y - my;
            double vz = p.Z - mz;
            double h = vx * nx + vy * ny + vz * nz; // signed distance to plane (in grid units)
            maxPlaneOff = Math.Max(maxPlaneOff, Math.Abs(h));
            // in-plane component length
            double px = vx - h * nx;
            double py = vy - h * ny;
            double pz = vz - h * nz;
            double rad = Math.Sqrt(px * px + py * py + pz * pz);
            radii[i] = rad;
        }

        // Plane fit should be tight despite grid snapping
        Assert.InRange(maxPlaneOff, 0.0, 2.0);

        double mean = radii.Average();
        double var = radii.Select(v => (v - mean) * (v - mean)).Average();
        double std = Math.Sqrt(var);

        // Mean radius should be close to theory, and low variance (near circle)
        Assert.InRange(Math.Abs(mean - expectedR), 0.0, 3.0);
        Assert.InRange(std, 0.0, 3.0);
    }
    [Fact]
    public void TwoSpheres_WellSeparated_NoLoops()
    {
        var a = new Sphere(40, subdivisions: 1, center: new Point(0, 0, 0));
        var b = new Sphere(40, subdivisions: 1, center: new Point(200, 200, 200));

        var surfaceA = ClosedSurface.FromTetrahedra(a.Tetrahedra);
        var surfaceB = ClosedSurface.FromTetrahedra(b.Tetrahedra);

        var loops = IntersectionSegments.BuildLoops(surfaceA, surfaceB);
        Assert.Empty(loops);
    }
}
