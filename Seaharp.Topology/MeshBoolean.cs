using System;
using System.Collections.Generic;
using Seaharp.Geometry;
using Seaharp.Geometry.Computation;

namespace Seaharp.Topology;

// Minimal, grid-snapped boolean union over ClosedSurface using per-triangle chord cuts.
// Iteratively splits triangles by all chords (per triangle), triangulates the resulting
// polygons, and classifies by centroid against the opposite surface.
//
// TODO: Coplanar overlaps — currently treated as boundary; add explicit merge strategy to
//       avoid double faces and to retain outer shells deterministically.
// TODO: Crossing chords — if multiple chords cross within a triangle, introduce interior
//       vertices (at chord intersections) to split correctly.
// TODO: Post-check — add manifold verification (edge degree == 2) and optional repair step
//       for slivers or zero-area triangles produced by snapping.
public static class MeshBoolean
{
    public static ClosedSurface Union(ClosedSurface a, ClosedSurface b, double epsilon = Tolerances.PlaneSideEpsilon)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var cuts = IntersectionSegments.BuildCuts(a, b, epsilon);
        var result = new List<Triangle>();

        // Helper to add triangles uniquely by unordered key
        var seen = new HashSet<TriangleKey>();
        void AddUnique(Triangle t)
        {
            var key = TriangleKey.FromTriangle(t);
            if (seen.Add(key)) result.Add(t);
        }

        // Keep pieces of A that are outside B (boundary-inclusive)
        ProcessSurface(a, b, cuts.CutsA, keepOutside: true, AddUnique, epsilon);
        // Keep pieces of B that are outside A (boundary-inclusive)
        ProcessSurface(b, a, cuts.CutsB, keepOutside: true, AddUnique, epsilon);

        return new ClosedSurface(result);
    }

    private static void ProcessSurface(
        ClosedSurface src,
        ClosedSurface other,
        List<(Point P, Point Q)>[] cutsPerTri,
        bool keepOutside,
        Action<Triangle> emit,
        double epsilon)
    {
        var tris = src.Triangles;
        var otherTris = other.Triangles;
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            var cuts = cutsPerTri[i];
            var seamEdges = new HashSet<EdgeKey>(cuts.Count);
            for (int ce = 0; ce < cuts.Count; ce++)
            {
                var seg = cuts[ce];
                if (!seg.P.Equals(seg.Q)) seamEdges.Add(new EdgeKey(seg.P, seg.Q));
            }
            if (cuts.Count == 0)
            {
                // Classify triangle by centroid
                var cx = (t.P0.X + t.P1.X + t.P2.X) / 3.0;
                var cy = (t.P0.Y + t.P1.Y + t.P2.Y) / 3.0;
                var cz = (t.P0.Z + t.P1.Z + t.P2.Z) / 3.0;
                var c = new Point(
                    (long)Math.Round(cx, MidpointRounding.AwayFromZero),
                    (long)Math.Round(cy, MidpointRounding.AwayFromZero),
                    (long)Math.Round(cz, MidpointRounding.AwayFromZero));
                var cls = InsideClosedSurface.Classify(otherTris, c, epsilon);
                bool keep0 = keepOutside ? (cls != InsideClosedSurface.Classification.Inside) : (cls == InsideClosedSurface.Classification.Inside);
                if (keep0) emit(t);
                continue;
            }

            // Start with the original triangle polygon
            var polygons = new List<List<Point>> { new List<Point> { t.P0, t.P1, t.P2 } };

            foreach (var seg in cuts)
            {
                var nextPolys = new List<List<Point>>();
                foreach (var poly in polygons)
                {
                    // Check if both endpoints lie on current polygon boundary
                    bool OnBoundary(List<Point> pg, Point pnt)
                    {
                        for (int e = 0; e < pg.Count; e++)
                        {
                            var a = pg[e]; var b = pg[(e + 1) % pg.Count];
                            if (IsOnSegment(a, b, pnt)) return true;
                        }
                        return false;
                    }

                    if (OnBoundary(poly, seg.P) && OnBoundary(poly, seg.Q))
                    {
                        var split = TriangleSplitter.SplitPolygonByChord(poly, seg.P, seg.Q);
                        bool any = false;
                        foreach (var sp in split)
                        {
                            nextPolys.Add(new List<Point>(sp));
                            any = true;
                        }
                        if (!any) nextPolys.Add(poly); // if split failed, keep original
                    }
                    else
                    {
                        nextPolys.Add(poly);
                    }
                }
                polygons = nextPolys;
            }

            // Triangulate and classify all resulting polygons
            foreach (var poly in polygons)
            {
                if (poly.Count < 3) continue;
                foreach (var tt in TriangleSplitter.TriangulatePolygon(t, poly))
                {
                    var cx = (tt.P0.X + tt.P1.X + tt.P2.X) / 3.0;
                    var cy = (tt.P0.Y + tt.P1.Y + tt.P2.Y) / 3.0;
                    var cz = (tt.P0.Z + tt.P1.Z + tt.P2.Z) / 3.0;
                    var c = new Point(
                        (long)Math.Round(cx, MidpointRounding.AwayFromZero),
                        (long)Math.Round(cy, MidpointRounding.AwayFromZero),
                        (long)Math.Round(cz, MidpointRounding.AwayFromZero));
                    var cls = InsideClosedSurface.Classify(otherTris, c, epsilon);
                    bool keep1 = keepOutside ? (cls != InsideClosedSurface.Classification.Inside) : (cls == InsideClosedSurface.Classification.Inside);
                    bool touchesSeam = seamEdges.Contains(new EdgeKey(tt.P0, tt.P1)) ||
                                       seamEdges.Contains(new EdgeKey(tt.P1, tt.P2)) ||
                                       seamEdges.Contains(new EdgeKey(tt.P2, tt.P0));
                    if (keep1 || touchesSeam) emit(tt);
                    // TODO: If centroid classification is ambiguous near the boundary,
                    //       fall back to signed-plane tests against intersection loop.
                }
            }
        }
    }

    private static bool IsOnSegment(in Point a, in Point b, in Point p)
    {
        long minX = Math.Min(a.X, b.X), maxX = Math.Max(a.X, b.X);
        long minY = Math.Min(a.Y, b.Y), maxY = Math.Max(a.Y, b.Y);
        long minZ = Math.Min(a.Z, b.Z), maxZ = Math.Max(a.Z, b.Z);
        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY || p.Z < minZ || p.Z > maxZ) return false;
        var v0 = new Vector(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var v1 = new Vector(p.X - a.X, p.Y - a.Y, p.Z - a.Z);
        var c = v0.Cross(v1);
        return c.X == 0 && c.Y == 0 && c.Z == 0;
        // TODO: If we allow small non-axis-aligned edge segments after snapping, consider
        //       using an epsilon-based colinearity check here.
    }
}
