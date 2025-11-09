using System;
using System.Collections.Generic;
using Seaharp.Geometry;

namespace Seaharp.World;

// Simple BVH over triangle faces of a Polyhedron for broad-phase queries.
internal sealed class PolyhedronBvh
{
    private readonly Polyhedron _poly;
    private readonly Node _root;

    private readonly struct FaceBox
    {
        public readonly int Index;
        public readonly AabbL Box;
        public FaceBox(int i, in AabbL b) { Index = i; Box = b; }
    }

    private sealed class Node
    {
        public AabbL Box;
        public Node? Left;
        public Node? Right;
        public int Start; // start index into Faces
        public int Count; // number of faces in leaf (if Count>0)
    }

    private readonly FaceBox[] _faces;

    public PolyhedronBvh(Polyhedron poly)
    {
        _poly = poly ?? throw new ArgumentNullException(nameof(poly));
        _faces = BuildFaceBoxes(poly);
        _root = Build(0, _faces.Length);
    }

    public void Query(in AabbL box, List<int> results)
    {
        QueryNode(_root, box, results);
    }

    private static FaceBox[] BuildFaceBoxes(Polyhedron p)
    {
        var faces = p.Triangles;
        var verts = p.Vertices;
        var arr = new FaceBox[faces.Count];
        for (int i = 0; i < faces.Count; i++)
        {
            var (a, b, c) = faces[i];
            var tbox = AabbL.FromPoints(verts[a], verts[b], verts[c]);
            arr[i] = new FaceBox(i, tbox);
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
            var b = _faces[i].Box;
            if (b.MinX < minX) minX = b.MinX;
            if (b.MinY < minY) minY = b.MinY;
            if (b.MinZ < minZ) minZ = b.MinZ;
            if (b.MaxX > maxX) maxX = b.MaxX;
            if (b.MaxY > maxY) maxY = b.MaxY;
            if (b.MaxZ > maxZ) maxZ = b.MaxZ;
        }
        node.Box = new AabbL(minX, minY, minZ, maxX, maxY, maxZ);

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
        Array.Sort(_faces, start, count, new FaceBoxComparer(axis));
        int mid = start + count / 2;
        node.Left = Build(start, mid);
        node.Right = Build(mid, end);
        return node;
    }

    private void QueryNode(Node node, in AabbL box, List<int> results)
    {
        if (!node.Box.Overlaps(box)) return;
        if (node.Count > 0)
        {
            for (int i = 0; i < node.Count; i++)
            {
                var fb = _faces[node.Start + i];
                if (fb.Box.Overlaps(box)) results.Add(fb.Index);
            }
            return;
        }
        if (node.Left is not null) QueryNode(node.Left, box, results);
        if (node.Right is not null) QueryNode(node.Right, box, results);
    }

    private sealed class FaceBoxComparer : IComparer<FaceBox>
    {
        private readonly int _axis;
        public FaceBoxComparer(int axis) { _axis = axis; }
        public int Compare(FaceBox x, FaceBox y)
        {
            long cx = Center(x.Box, _axis);
            long cy = Center(y.Box, _axis);
            return cx.CompareTo(cy);
        }
        private static long Center(in AabbL b, int axis)
            => axis == 0 ? (b.MinX + b.MaxX) : axis == 1 ? (b.MinY + b.MaxY) : (b.MinZ + b.MaxZ);
    }
}

