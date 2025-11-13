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
    public void TwoTetrahedra_Intersection_IsExactTriangle()
    {
        // Tetrahedron A: base on z=0 plane (triangle with corners (0,0,0),(2,0,0),(0,2,0)) and apex (0,0,2)
        var a0 = new Point(0, 0, 0);
        var a1 = new Point(2, 0, 0);
        var a2 = new Point(0, 2, 0);
        var a3 = new Point(0, 0, 2);
        var A = new Seaharp.World.Tetrahedron(a0, a1, a2, a3);

        // Tetrahedron B: three vertices below z=0 and one above; its intersection with z=0 is the
        // exact triangle with corners (1,0,0), (0,1,0), (1,1,0), all inside A's base triangle.
        var b0 = new Point(0, 0, 1);   // above
        var b1 = new Point(2, 0, -1);  // below
        var b2 = new Point(0, 2, -1);  // below
        var b3 = new Point(2, 2, -1);  // below
        var B = new Seaharp.World.Tetrahedron(b0, b1, b2, b3);

        var surfaceA = ClosedSurface.FromTetrahedra(A.Tetrahedra);
        var surfaceB = ClosedSurface.FromTetrahedra(B.Tetrahedra);

        var loops = IntersectionSegments.BuildLoops(surfaceA, surfaceB);
        Assert.NotNull(loops);
        Assert.NotEmpty(loops);

        var p1 = new Point(1,0,0);
        var p2 = new Point(1,1,0);
        var p3 = new Point(0,1,0);
        var expectedSet = new HashSet<Point> { p1, p2, p3 };

        bool MatchesExpected(IReadOnlyList<Point> lp)
        {
            var n = lp.Count;
            var closed = (n >= 2) && lp[0].Equals(lp[n-1]);
            var uniq = closed ? lp.Take(n-1).ToList() : lp.ToList();
            if (uniq.Count != 3) return false;
            // All points present
            if (!uniq.All(p => expectedSet.Contains(p))) return false;
            // Check cyclic order equals one of the two orientations
            var seq = uniq;
            int idx = seq.FindIndex(q => q.Equals(p1));
            if (idx >= 0)
            {
                var r0 = seq[idx];
                var r1 = seq[(idx+1)%3];
                var r2 = seq[(idx+2)%3];
                if ((r0.Equals(p1) && r1.Equals(p2) && r2.Equals(p3)) ||
                    (r0.Equals(p1) && r1.Equals(p3) && r2.Equals(p2)))
                    return true;
            }
            // Fallback: set match is sufficient
            return true;
        }

        // There should be at least one exact triangle loop matching the expected vertices
        Assert.Contains(loops, l => MatchesExpected(l));

        // Additionally, ensure those expected vertices lie on z=0
        foreach (var l in loops)
        {
            foreach (var p in l)
            {
                if (expectedSet.Contains(p)) Assert.Equal(0, p.Z);
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

    [Fact]
    public void TwoSpheres_CutsAndLoops_Consistent_AndTouchedTrianglesSuperset()
    {
        var centerA = new Point(0, 0, 0);
        var centerB = new Point(35, 11, -7);
        var r = 80;

        var a = new Sphere(r, subdivisions: 3, center: centerA);
        var b = new Sphere(r, subdivisions: 3, center: centerB);

        var surfaceA = ClosedSurface.FromTetrahedra(a.Tetrahedra);
        var surfaceB = ClosedSurface.FromTetrahedra(b.Tetrahedra);

        var loops = IntersectionSegments.BuildLoops(surfaceA, surfaceB);
        var cuts = IntersectionSegments.BuildCuts(surfaceA, surfaceB);

        // 1) Every cut segment should appear in the loop edge set (undirected)
        var loopEdges = new HashSet<EdgeKey>();
        foreach (var loop in loops)
        {
            int n = loop.Count;
            if (n < 2) continue;
            for (int i = 0; i < n - 1; i++)
            {
                loopEdges.Add(new EdgeKey(loop[i], loop[i + 1]));
            }
        }

        var cutEdges = new HashSet<EdgeKey>();
        for (int i = 0; i < cuts.CutsA.Length; i++)
            foreach (var seg in cuts.CutsA[i]) cutEdges.Add(new EdgeKey(seg.P, seg.Q));
        for (int j = 0; j < cuts.CutsB.Length; j++)
            foreach (var seg in cuts.CutsB[j]) cutEdges.Add(new EdgeKey(seg.P, seg.Q));

        // Loop edges must cover all cut edges (loops are built from segments)
        Assert.True(cutEdges.Count > 0);
        foreach (var e in cutEdges)
            Assert.Contains(e, loopEdges);

        // 2) Touched triangles must be a superset of triangles marked by BuildCuts
        var touched = IntersectionSegments.ExtractTouchedTriangles(surfaceA, surfaceB);

        // Build maps triangle -> index for original surfaces
        var idxA = new Dictionary<Triangle, int>();
        var idxB = new Dictionary<Triangle, int>();
        for (int i = 0; i < surfaceA.Triangles.Count; i++) idxA[surfaceA.Triangles[i]] = i;
        for (int j = 0; j < surfaceB.Triangles.Count; j++) idxB[surfaceB.Triangles[j]] = j;

        var touchedAIdx = new HashSet<int>(touched.A.Select(t => idxA[t]));
        var touchedBIdx = new HashSet<int>(touched.B.Select(t => idxB[t]));

        for (int i = 0; i < cuts.CutsA.Length; i++)
            if (cuts.CutsA[i].Count > 0) Assert.Contains(i, touchedAIdx);
        for (int j = 0; j < cuts.CutsB.Length; j++)
            if (cuts.CutsB[j].Count > 0) Assert.Contains(j, touchedBIdx);
    }
}
