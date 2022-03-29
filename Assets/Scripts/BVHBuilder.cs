using System;
using System.Collections.Generic;
using UnityEngine;

public struct Bounds
{
    public Vector3 Minimum;
    public Vector3 Maximum;

    public static Bounds Union(Bounds a, Bounds b)
    {
        Bounds ab;
        ab.Maximum = Vector3.Max(a.Maximum, b.Maximum);
        ab.Minimum = Vector3.Min(a.Minimum, b.Minimum);
        return ab;
    }
    public static Bounds Union(Bounds a, Vector3 b)
    {
        Bounds ab;
        ab.Maximum = Vector3.Max(a.Maximum, b);
        ab.Minimum = Vector3.Min(a.Minimum, b);
        return ab;
    }

    public int MaximumExtent()
    {
        Vector3 diag = Maximum - Minimum;
        if (diag.x > diag.y && diag.x > diag.z)
            return 0;
        if (diag.y > diag.z)
            return 1;
        return 2;
    }
}

public struct PrimitiveInfo
{
    public int PrimIndex, MeshIndex;
    public Bounds Bounds;
    public Vector3 Center;
} 

public class BVHNode
{
    public Bounds Bounds;
    public int SplitAxis, FirstPrimOffset, PrimCount, MeshIndex;
    public BVHNode Child0, Child1;
    public int C0Index, C1Index;

    public void InitLeafNode(int first, int n, Bounds b, int MeshId)
    {

        FirstPrimOffset = first;
        PrimCount = n;
        Bounds = b;
        Child0 = Child1 = null;
        MeshIndex = MeshId;

    }

    public void InitInnerNode(int axis, BVHNode c0, BVHNode c1)
    {
        SplitAxis = axis;

        Child0 = c0;
        Child1 = c1;
        Bounds = Bounds.Union(c0.Bounds, c1.Bounds);

        PrimCount = 0;
    }
}

public class BVHBuilder
{
    private static List<PathTracer.MeshObject> _meshObjects = new List<PathTracer.MeshObject>();
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<int> _indices = new List<int>();

    private List<PrimitiveInfo> _primitiveInfos = new List<PrimitiveInfo>();

    private BVHNode _rootNode;
    private int _rootIndex;
    private List<BVHNode> _nodes = new List<BVHNode>();
    private List<int> _orderedIndices = new List<int>();

    private int _nPrims;

    public BVHBuilder(List<PathTracer.MeshObject> meshObjects, List<Vector3> vertices, List<int> indices)
    {
        _meshObjects = meshObjects;
        _vertices = vertices;
        _indices = indices;
        _nPrims = indices.Count / 3;

        CreatePrimitiveInfos();
    }

    private Vector4 Vector4FromVector3(Vector3 src, float w)
    {
        Vector4 result = new Vector4();
        result.x = src.x;
        result.y = src.y;
        result.z = src.z;
        result.w = w;
        return result;
    }

    private float GetVectorComponent(Vector3 v, int dim)
    {
        switch (dim)
        {
            case 0:
                return v.x;
            case 1:
                return v.y;
            case 2:
                return v.z;
        }

        return Single.NegativeInfinity;
    }

    private void CreatePrimitiveInfos()
    {
        for (int meshID = 0; meshID < _meshObjects.Count; meshID++)
        {
            PathTracer.MeshObject mesh = _meshObjects[meshID];
            for (int i = 0; i < mesh.TriangleCount; i++)
            {
                Vector3 v1 = mesh.ModelMatrix *
                             Vector4FromVector3(_vertices[_indices[mesh.IndexOffset + i * 3 + 0]], 1);
                Vector3 v2 = mesh.ModelMatrix *
                             Vector4FromVector3(_vertices[_indices[mesh.IndexOffset + i * 3 + 1]], 1);
                Vector3 v3 = mesh.ModelMatrix *
                             Vector4FromVector3(_vertices[_indices[mesh.IndexOffset + i * 3 + 2]], 1);

                Bounds bounds = new Bounds();
                bounds.Maximum = Vector3.Max(v1, Vector3.Max(v2, v3));
                bounds.Minimum = Vector3.Min(v1, Vector3.Min(v2, v3));

                PrimitiveInfo info = new PrimitiveInfo();
                info.Bounds = bounds;
                info.PrimIndex = mesh.IndexOffset + i * 3;
                info.Center = bounds.Minimum + 0.5f * (bounds.Maximum - bounds.Minimum);
                info.MeshIndex = meshID;
                _primitiveInfos.Add(info);
            }
        }
    }

    private int RegisterNode(BVHNode node)
    {
        var currentNumNodes = _nodes.Count;
        _nodes.Add(node);
        return currentNumNodes;
    }

    private BVHNode RecursiveBuild(int start, int end, List<int> orderedIndices)
    {
        BVHNode node = new BVHNode();

        Bounds bounds = _primitiveInfos[start].Bounds;
        for (int i = start; i < end; ++i)
            bounds = Bounds.Union(bounds, _primitiveInfos[i].Bounds);

        int nPrimitives = end - start;
        if (nPrimitives == 1)
        {
            int firstPrimsOffset = orderedIndices.Count;
            
            for (int i = start; i < end; ++i)
            {
                int primNum = _primitiveInfos[i].PrimIndex;
                orderedIndices.Add(_indices[primNum]);
                orderedIndices.Add(_indices[primNum+1]);
                orderedIndices.Add(_indices[primNum+2]);
            }
            node.InitLeafNode(firstPrimsOffset, nPrimitives, bounds, _primitiveInfos[start].MeshIndex);
            return node;
        }
        else
        {
            Bounds centroidBounds;
            centroidBounds.Maximum = _primitiveInfos[start].Center;
            centroidBounds.Minimum = _primitiveInfos[start].Center;
            for (int i = start; i < end; ++i)
            {
                centroidBounds = Bounds.Union(centroidBounds, _primitiveInfos[i].Center);
            }

            int dim = centroidBounds.MaximumExtent();
            int mid = (start + end) / 2;

            if (GetVectorComponent(centroidBounds.Maximum, dim) == GetVectorComponent(centroidBounds.Minimum, dim))
            {
                int firstPrimsOffset = orderedIndices.Count;

                for (int i = start; i < end; ++i)
                {
                    int primNum = _primitiveInfos[i].PrimIndex;
                    orderedIndices.Add(_indices[primNum]);
                    orderedIndices.Add(_indices[primNum + 1]);
                    orderedIndices.Add(_indices[primNum + 2]);
                }
                node.InitLeafNode(firstPrimsOffset, nPrimitives, bounds, _primitiveInfos[start].MeshIndex);
                return node;
            }
            else
            {
                var c0 = RecursiveBuild(start, mid, orderedIndices);
                var c1 = RecursiveBuild(mid, end, orderedIndices);
                node.InitInnerNode(dim, c0, c1);

                node.C0Index = RegisterNode(c0);
                node.C1Index = RegisterNode(c1);
                return node;
            }
        }

    }

    public void Build()
    {
        _rootNode = RecursiveBuild(0, _nPrims, _orderedIndices);
        _rootIndex = RegisterNode(_rootNode);
        Debug.Log(_rootNode.Bounds.Maximum);
        Debug.Log(_rootNode.Bounds.Minimum);
    }

    public List<BVHNode> GetBvhNodes()
    {
        return _nodes;
    }

    public int GetRootNodeIndex()
    {
        return _rootIndex;
    }

    public List<int> GetOrderedIndices()
    {
        return _orderedIndices;
    }
}