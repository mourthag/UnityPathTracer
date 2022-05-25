using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public enum OutputType {
    Shaded = 0,
    Normals = 1,
    UVs = 2,
    WorldPosition = 3
}

public class PathTracer : MonoBehaviour
{
    private static bool _meshObjectsNeedRebuilding = false;
    private static bool _lightsNeedRebuilding = false;
    private static List<PathTracingObject> _ptObjects = new List<PathTracingObject>();

    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
    }

    public struct MeshObject
    {
        public Matrix4x4 ModelMatrix;
        public Matrix4x4 NormalMatrix;
        public int IndexOffset;
        public int TriangleCount;
        public int MaterialIndex;
    }

    public struct BVHBufferNode
    {
        public Vector3 BoundsMaximum;
        public Vector3 BoundsMinimum;
        public int FirstPrimOffset, PrimCount, MeshIndex;
        public int C0Index, C1Index;
    }

    private static List<MeshObject> _meshObjects = new List<MeshObject>();
    private static List<Vertex> _vertices = new List<Vertex>();
    private static List<int> _indices = new List<int>();
    private static List<PathTracingObject.MaterialObject> _materialBufferObjects = new List<PathTracingObject.MaterialObject>();
    private static List<BVHBufferNode> _bvhBufferNodes = new List<BVHBufferNode>();

    private static List<LightBufferObject> _lightBufferObjects = new List<LightBufferObject>();

    private ComputeBuffer _meshObjectsBuffer;
    private ComputeBuffer _verticesBuffer;
    private ComputeBuffer _indicesBuffer;
    private ComputeBuffer _materialBuffer;
    private ComputeBuffer _bvhBuffer;
    private ComputeBuffer _lightsBuffer;

    public ComputeShader PathTracingShader;

    public Texture SkyboxTexture;

    public OutputType OutputType;

    public uint MaxSamples;
    public bool UseBVH;
    public bool BackfaceCulling;

    private BVHBuilder _bvhBuilder;

    private RenderTexture _target;
    private RenderTexture _prevResults;

    private Camera _camera;
    private uint _currentSample = 0;
    private Material _addMaterial;

    //Output image
    public bool SaveAsPNG;
    private bool _wasImageSaved;

    //Performance
    private DateTime _startTime;

    public static void RegisterObject(PathTracingObject obj)
    {
        _ptObjects.Add(obj);
        _meshObjectsNeedRebuilding = true;
    }

    public static void UnregisterObject(PathTracingObject obj)
    {
        _ptObjects.Remove(obj);
        _meshObjectsNeedRebuilding = true;
    }

    public static void RegisterLight(PathTracingLight ptLight)
    {
        _lightBufferObjects.Add(new LightBufferObject(ptLight));
        _lightsNeedRebuilding = true;
    }

    public static void UnregisterLight(PathTracingLight ptLight)
    {
        _lightBufferObjects.Remove(new LightBufferObject(ptLight));
        _lightsNeedRebuilding = true;
    }

    public static void SetMeshObjectsNeedRebuilding()
    {
        _meshObjectsNeedRebuilding = true;

    }

    private void RebuildLightsBuffer()
    {
        if(!_lightsNeedRebuilding)
            return;

        CreateComputeBuffer<LightBufferObject>(ref _lightsBuffer, _lightBufferObjects, 44);
    }

    private void RebuildMeshObjectBuffers()
    {
        if (!_meshObjectsNeedRebuilding)
            return;
        //TODO export reset method
        _meshObjectsNeedRebuilding = false;
        _currentSample = 0;
        _wasImageSaved = false;
        _startTime = DateTime.Now;
        

        //Clear all buffers
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();

        foreach (var ptObject in _ptObjects)
        {
            //Fetch Mesh and MeshRenderer to access their data
            var meshRenderer = ptObject.GetComponent<MeshRenderer>();
            var mesh = ptObject.GetComponent<MeshFilter>().mesh;

            IEnumerable<Vertex> query = mesh.vertices.Zip(mesh.normals,
                (position, normal) => new Vertex
                {
                    Position = position,
                    Normal = normal.normalized,
                });
            if(mesh.uv.Length > 0)
                query = mesh.uv.Zip(query, (uv, vert) => new Vertex{
                    Position = vert.Position,
                    Normal = vert.Normal,
                    UV = uv
                });

            //Add vertices and normals and remember previous count to offset the indices
            int firstVertex = _vertices.Count;

            _vertices.AddRange(query.ToArray());

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var submesh = mesh.GetSubMesh(i);

                //Get the index offset and add vertex indices shifted by the current vertex offset
                int firstIndex = _indices.Count;
                var indices = mesh.GetIndices(i);
                _indices.AddRange(indices.Select(index => index + firstVertex));

                //Add material and get its index
                int matIndex = _materialBufferObjects.Count();
                _materialBufferObjects.Add(ptObject.Materials[i]);

                Matrix4x4 normalMat = meshRenderer.worldToLocalMatrix.transpose;

                //Create an object to boundle information of a Mesh and add it
                MeshObject meshObject = new MeshObject
                {
                    ModelMatrix = meshRenderer.localToWorldMatrix,
                    NormalMatrix = normalMat,
                    IndexOffset = firstIndex,
                    TriangleCount = indices.Length / 3,
                    MaterialIndex = matIndex
                };
                _meshObjects.Add(meshObject);
            }



        }

        CreateComputeBuffer<MeshObject>(ref _meshObjectsBuffer, _meshObjects, 140);
        CreateComputeBuffer<Vertex>(ref _verticesBuffer, _vertices, 32);
        CreateComputeBuffer<int>(ref _indicesBuffer, _indices, 4);
        CreateComputeBuffer<PathTracingObject.MaterialObject>(ref _materialBuffer, _materialBufferObjects, 48);
        if (UseBVH)
        {
            CreateBVH();

            CreateComputeBuffer<BVHBufferNode>(ref  _bvhBuffer, _bvhBufferNodes, 44);
            CreateComputeBuffer<int>(ref _indicesBuffer, _bvhBuilder.GetOrderedIndices(), 4);
        }

        TimeSpan renderTime = DateTime.Now - _startTime;
        Debug.Log("Scene/BVH construction took " + renderTime.TotalSeconds + " seconds!");

        _startTime = DateTime.Now;

    }

    private void CreateBVH()
    {
        _bvhBufferNodes.Clear();
        Debug.Log("Total triangles: " + _indices.Count / 3);
        _bvhBuilder = new BVHBuilder(_meshObjects, _vertices, _indices);
        _bvhBuilder.Build();

        var bvhNodes = _bvhBuilder.GetBvhNodes();

        foreach (var bvhNode in bvhNodes)
        {
            BVHBufferNode bufferNode = new BVHBufferNode();
            bufferNode.BoundsMaximum = bvhNode.Bounds.Maximum;
            bufferNode.BoundsMinimum = bvhNode.Bounds.Minimum;
            bufferNode.PrimCount = bvhNode.PrimCount;
            bufferNode.C0Index = bvhNode.Child0 == null ? -1 : bvhNode.C0Index;
            bufferNode.C1Index = bvhNode.Child1 == null ? -1 : bvhNode.C1Index;
            bufferNode.FirstPrimOffset = bvhNode.FirstPrimOffset;
            bufferNode.MeshIndex = bvhNode.MeshIndex;

            _bvhBufferNodes.Add(bufferNode);
        }
    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }
        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }
    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            PathTracingShader.SetBuffer(0, name, buffer);
        }
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void SetShaderParameters()
    {
        PathTracingShader.SetInt("OutputType", (int)OutputType);

        PathTracingShader.SetTexture(0, "Result", _target);
        PathTracingShader.SetTexture(0, "PrevResult", _prevResults);
        PathTracingShader.SetFloat("_CurrentSample", _currentSample);

        PathTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        PathTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        PathTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        PathTracingShader.SetFloat("_Seed", Random.value);
        PathTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));

        SetComputeBuffer("_MeshObjects", _meshObjectsBuffer);
        SetComputeBuffer("_Vertices", _verticesBuffer);
        SetComputeBuffer("_Indices", _indicesBuffer);
        SetComputeBuffer("_Materials", _materialBuffer);
        SetComputeBuffer("_Lights", _lightsBuffer);

        if (BackfaceCulling)
            PathTracingShader.EnableKeyword("BACKFACECULLING");
        else
            PathTracingShader.DisableKeyword("BACKFACECULLING");

        if(_lightBufferObjects.Count > 0)
            PathTracingShader.EnableKeyword("USE_LIGHTS");
        else
            PathTracingShader.DisableKeyword("USE_LIGHTS");

        if (UseBVH)
        {
            PathTracingShader.EnableKeyword("USE_BVH");
            SetComputeBuffer("_BVHNodes", _bvhBuffer);
            PathTracingShader.SetInt("RootNode", _bvhBuilder.GetRootNodeIndex());
        }
        else
            PathTracingShader.DisableKeyword("USE_BVH");
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RebuildMeshObjectBuffers();
        RebuildLightsBuffer();
        Render(destination);

        if(_currentSample >= MaxSamples && !_wasImageSaved)
        {
            RenderTexture.active = destination;
            Texture2D imgTex = new Texture2D(Screen.width, Screen.height);
            imgTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            imgTex.Apply();
            byte[] bytes = imgTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.dataPath + "/RenderOutput.png", bytes);
            _wasImageSaved = true;
        }
    }

    private void Render(RenderTexture destination)
    {


        if (_currentSample >= MaxSamples)
            return;

        // Make sure we have a current render target
        InitRenderTexture();

        
        //Update Shader uniforms
        SetShaderParameters();

        // Set the target and dispatch the compute shader
        uint groupSizeX, groupSizeY, groupSizeZ;
        PathTracingShader.GetKernelThreadGroupSizes(0, out groupSizeX, out groupSizeY, out groupSizeZ);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / groupSizeX);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / groupSizeY);
        PathTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        
        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/MultiplePassShader"));

        _addMaterial.SetFloat("_CurrentSample", _currentSample);

        Graphics.Blit(_target, destination);

        TimeSpan renderTime = DateTime.Now - _startTime;

        Debug.Log("Sample " + _currentSample + " took " + renderTime.TotalSeconds + " seconds. This is " + (_currentSample + 1)/renderTime.TotalSeconds + " spp per second!");
        _currentSample++;
    }
    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
                _target.Release();
            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) {enableRandomWrite = true};
            _target.Create();
        }
        if (_prevResults == null || _prevResults.width != Screen.width || _prevResults.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_prevResults != null)
                _prevResults.Release();
            // Get a render target for Ray Tracing
            _prevResults = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _prevResults.enableRandomWrite = true;
            _prevResults.Create();
        }
    }

    private void OnDisable()
    {
        _indicesBuffer?.Release();
        _verticesBuffer?.Release();
        _meshObjectsBuffer?.Release();
        _materialBuffer?.Release();
        _bvhBuffer?.Release();
        _lightsBuffer.Release();
    }
}
