using System;
using System.Collections.Generic;
using Seaharp.Geometry;
using Seaharp.Geometry.Computational;

namespace Seaharp.World;

// Bounding box tree over Surface triangles.

// Simple BVH over triangles of a Surface for broad-phase queries.
internal sealed class BoundingBoxTree
{
    private readonly Seaharp.Surface.ClosedSurface _surface;
    private readonly Node _root;

    private readonly struct TriangleBox
    {
        public readonly int Index;
        public readonly BoundingBox Box;
        public TriangleBox(int i, in BoundingBox b) { Index = i; Box = b; }
    }

    private sealed class Node
    {
        public BoundingBox Box;
        public Node? Left;
        public Node? Right;
        public int Start; // start index into triangles
        public int Count; // number of triangles in leaf (if Count>0)
    }

    private readonly TriangleBox[] _triangles;

    public BoundingBoxTree(Seaharp.Surface.ClosedSurface surface)
    {
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _triangles = BuildTriangleBoxes(surface);
        _root = Build(0, _triangles.Length);
    }

    public void Query(in BoundingBox box, List<int> results)
    {
        QueryNode(_root, box, results);
    }

    private static TriangleBox[] BuildTriangleBoxes(Seaharp.Surface.ClosedSurface s)
    {
        var tris = s.Triangles;
        var arr = new TriangleBox[tris.Count];
        for (int i = 0; i < tris.Count; i++)
        {
            var t = tris[i];
            var box = BoundingBox.FromPoints(t.P0, t.P1, t.P2);
            arr[i] = new TriangleBox(i, box);
        }
        return arr;
    }

    private Node Build(int start, int end)
    {
        var node = new Node();
        // compute bounding box for range
        long minX = long.MaxValue, minY = long.MaxValue, minZ = long.MaxValue;
        long maxX = long.MinValue, maxY = long.MinValue, maxZ = long.MinValue;
        for (int i = start; i < end; i++)
        {
            var b = _triangles[i].Box;
            if (b.Min.X < minX) minX = b.Min.X;
            if (b.Min.Y < minY) minY = b.Min.Y;
            if (b.Min.Z < minZ) minZ = b.Min.Z;
            if (b.Max.X > maxX) maxX = b.Max.X;
            if (b.Max.Y > maxY) maxY = b.Max.Y;
            if (b.Max.Z > maxZ) maxZ = b.Max.Z;
        }
        node.Box = new BoundingBox(new Point(minX, minY, minZ), new Point(maxX, maxY, maxZ));

        int count = end - start;
        if (count <= 8)
        {
            node.Start = start;
            node.Count = count;
            return node;
        }

        // choose split axis by extent
        long ex = maxX - minX, ey = maxY - minY, ez = maxZ - minZ;
        int axis = ex >= ey && ex >= ez ? 0 : (ey >= ez ? 1 : 2);

        // sort range by centroid along axis and split in the middle
        Array.Sort(_triangles, start, count, new TriangleBoxComparer(axis));
        int mid = start + count / 2;
        node.Left = Build(start, mid);
        node.Right = Build(mid, end);
        return node;
    }

    private void QueryNode(Node node, in BoundingBox box, List<int> results)
    {
        if (!node.Box.Intersects(box)) return;
        if (node.Count > 0)
        {
            for (int i = 0; i < node.Count; i++)
            {
                var tb = _triangles[node.Start + i];
                if (tb.Box.Intersects(box)) results.Add(tb.Index);
            }
            return;
        }
        if (node.Left is not null) QueryNode(node.Left, box, results);
        if (node.Right is not null) QueryNode(node.Right, box, results);
    }

    private sealed class TriangleBoxComparer : IComparer<TriangleBox>
    {
        private readonly int _axis;
        public TriangleBoxComparer(int axis) { _axis = axis; }
        public int Compare(TriangleBox x, TriangleBox y)
        {
            long cx = Center(x.Box, _axis);
            long cy = Center(y.Box, _axis);
            return cx.CompareTo(cy);
        }
        private static long Center(in BoundingBox b, int axis)
            => axis == 0 ? (b.Min.X + b.Max.X) : axis == 1 ? (b.Min.Y + b.Max.Y) : (b.Min.Z + b.Max.Z);
    }
}
