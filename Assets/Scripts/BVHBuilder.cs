using System;
using System.Collections.Generic;
using UnityEngine;

public enum BVHSplitStrategy {
    EqualCount,
    SAH
}

[System.Serializable]
public struct BVHBuilderOptions {
    public BVHSplitStrategy strategy;
    public int SAHBuckets;
    public int SAHBundleCost;
    public int SAHPrimCost;
}

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

    public void Init(Vector3 a)
    {       
        Maximum = Vector3.Max(Maximum, a);
        Minimum = Vector3.Min(Minimum, a);
    }

    public Vector3 Offset(Vector3 point)
    {
        Vector3 o = point - Minimum;
            
        if (Maximum.x > Minimum.x) o.x /= Maximum.x - Minimum.x;
        if (Maximum.y > Minimum.y) o.y /= Maximum.y - Minimum.y;
        if (Maximum.z > Minimum.z) o.z /= Maximum.z - Minimum.z;
        return o;
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
    public float SurfaceArea()
    {
        Vector3 distance = Maximum - Minimum;
        float areaTop = distance.z * distance.x;
        float areaRight = distance.y * distance.z;
        float areaFront = distance.x * distance.y;
        return 2 * (areaTop + areaRight + areaFront);

    }
}

public struct SAHBucketInfo {
    public int count;
    public Bounds bounds;
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
    private List<MeshObject> _meshObjects = new List<MeshObject>();
    private List<Vertex> _vertices = new List<Vertex>();
    private List<int> _indices = new List<int>();

    private List<PrimitiveInfo> _primitiveInfos = new List<PrimitiveInfo>();

    private BVHBuilderOptions _options;

    private BVHNode _rootNode;
    private int _rootIndex;
    private List<BVHNode> _nodes = new List<BVHNode>();
    private List<int> _orderedIndices = new List<int>();

    private int _nPrims;

    public BVHBuilder(List<MeshObject> meshObjects, List<Vertex> vertices, List<int> indices, BVHBuilderOptions options)
    {
        _options = options;
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
            MeshObject mesh = _meshObjects[meshID];
            for (int i = 0; i < mesh.TriangleCount; i++)
            {
                Vector3 v1 = mesh.ModelMatrix *
                             Vector4FromVector3(_vertices[_indices[mesh.IndexOffset + i * 3 + 0]].Position, 1);
                Vector3 v2 = mesh.ModelMatrix *
                             Vector4FromVector3(_vertices[_indices[mesh.IndexOffset + i * 3 + 1]].Position, 1);
                Vector3 v3 = mesh.ModelMatrix *
                             Vector4FromVector3(_vertices[_indices[mesh.IndexOffset + i * 3 + 2]].Position, 1);

                Bounds bounds = new Bounds
                {
                    Maximum = Vector3.Max(v1, Vector3.Max(v2, v3)),
                    Minimum = Vector3.Min(v1, Vector3.Min(v2, v3))
                };

                PrimitiveInfo info = new PrimitiveInfo
                {
                    Bounds = bounds,
                    PrimIndex = mesh.IndexOffset + i * 3,
                    Center = bounds.Minimum + 0.5f * (bounds.Maximum - bounds.Minimum),
                    MeshIndex = meshID
                };
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

    private int PartitionPrimInfoByBucket(int start, int end, Bounds centroidBounds, int minCostSplitBucket, int dim)
    {
        List<PrimitiveInfo> left = new List<PrimitiveInfo>();
        List<PrimitiveInfo> right = new List<PrimitiveInfo>();

        for(int i = start; i < end; i++)
        {
            int b = (int)(_options.SAHBuckets * GetVectorComponent(centroidBounds.Offset(_primitiveInfos[i].Center),dim));
            if (b == _options.SAHBuckets) b = _options.SAHBuckets - 1;
            if( b <= minCostSplitBucket)
                left.Add(_primitiveInfos[i]);
            else
                right.Add(_primitiveInfos[i]);
        }

        int count = end - start;
        int leftCount = left.Count;
        left.AddRange(right);
        _primitiveInfos.RemoveRange(start, count);
        _primitiveInfos.InsertRange(start, left);
        return leftCount + start;
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
                switch(_options.strategy)
                {
                    case BVHSplitStrategy.EqualCount:
                        mid = (start + end) / 2;
                        break;
                    case BVHSplitStrategy.SAH:
                        if (nPrimitives <= 4)
                            mid = (start + end) / 2;
                        else {

                            SAHBucketInfo[] buckets = new SAHBucketInfo[_options.SAHBuckets];

                            bool isSameMesh = true;

                            //group primitives into buckets
                            for(int i = start; i < end; i++)
                            {
                                isSameMesh = isSameMesh & _primitiveInfos[i].MeshIndex == _primitiveInfos[start].MeshIndex;

                                int b = (int)(_options.SAHBuckets * GetVectorComponent(centroidBounds.Offset(_primitiveInfos[i].Center), dim));
                                if (b == _options.SAHBuckets) b = _options.SAHBuckets -1;
                                
                                if(buckets[b].count == 0)
                                    buckets[b].bounds = _primitiveInfos[i].Bounds;
                                else
                                    buckets[b].bounds = Bounds.Union(buckets[b].bounds, _primitiveInfos[i].Center);
                                
                                buckets[b].count++;
                            }

                            //Evaluate cost for each split
                            float[] cost = new float[_options.SAHBuckets-1];
                            for(int i = 0; i < _options.SAHBuckets -1; i++)
                            {
                                Bounds b0 = new Bounds(),b1 = new Bounds();
                                int cost0 = 0, cost1 = 0;
                                //left side
                                for(int j = 0; j<=i; j++)
                                {
                                    b0 = Bounds.Union(b0, buckets[j].bounds);
                                    cost0 += buckets[j].count;
                                }
                                //right side
                                for(int j = i+1; j<_options.SAHBuckets; j++)
                                {
                                    b1 = Bounds.Union(b1, buckets[j].bounds);
                                    cost1 += buckets[j].count;
                                }
                                cost[i] = 0.125f + (cost0 * b0.SurfaceArea() + cost1 * b1.SurfaceArea()) / bounds.SurfaceArea(); 
                            }

                            //determine bucket with min cost
                            float minCost = cost[0];
                            int minCostSplitBucket = 0;
                            for (int i = 1; i < _options.SAHBuckets - 1; ++i) {
                                if (cost[i] < minCost) {
                                    minCost = cost[i];
                                    minCostSplitBucket = i;
                                }
                            }
                    
                            float leafCost = _options.SAHPrimCost * nPrimitives;
                            if(minCost < leafCost)
                            {
                                //split
                                mid = PartitionPrimInfoByBucket(start, end, centroidBounds, minCostSplitBucket, dim);
                            }
                            else {
                                if(isSameMesh) {
                                    int firstPrimsOffset = orderedIndices.Count;

                                    for (int i = start; i < end; ++i)
                                    {
                                        int primNum = _primitiveInfos[i].PrimIndex;
                                        
                                        orderedIndices.Add(_indices[primNum]);
                                        orderedIndices.Add(_indices[primNum + 1]);
                                        orderedIndices.Add(_indices[primNum + 2]);
                                    }
                                    //TODO: this might interfere with the mesh index
                                    node.InitLeafNode(firstPrimsOffset, nPrimitives, bounds, _primitiveInfos[start].MeshIndex);
                                    return node;
                                }
                                else
                                    mid = (start + end) / 2;
                            }
                        }

                        
                        break;

                }

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
        _orderedIndices.Clear();
        _rootNode = RecursiveBuild(0, _nPrims, _orderedIndices);
        _rootIndex = RegisterNode(_rootNode);
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