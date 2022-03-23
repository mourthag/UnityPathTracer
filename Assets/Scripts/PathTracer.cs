using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathTracer : MonoBehaviour
{
    private static bool _meshObjectsNeedRebuilding = false;
    private static List<PathTracingObject> _ptObjects = new List<PathTracingObject>();

    struct MeshObject
    {
        public Matrix4x4 ModelMatrix;
        public int IndexOffset;
        public int TriangleCount;
        public int MaterialIndex;
    }

    struct MaterialBufferObject
    {
        public int type;
        public Vector3 albedo;
        public Vector3 specular;
        public Vector3 emissive;
    }

    private static List<MeshObject> _meshObjects = new List<MeshObject>();
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<int> _indices = new List<int>();
    private static List<MaterialBufferObject> _materialBufferObjects = new List<MaterialBufferObject>();

    private ComputeBuffer _meshObjectsBuffer;
    private ComputeBuffer _verticesBuffer;
    private ComputeBuffer _indicesBuffer;
    private ComputeBuffer _materialBuffer;

    public ComputeShader PathTracingShader;

    public Texture SkyboxTexture;


    public uint MaxSamples;


    private RenderTexture _target;
    private RenderTexture _prevResults;

    private Camera _camera;
    private uint _currentSample = 0;
    private Material _addMaterial;

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

    public static void SetMeshObjectsNeedRebuilding()
    {
        _meshObjectsNeedRebuilding = true;

    }

    private void RebuildMeshObjectBuffers()
    {
        if (!_meshObjectsNeedRebuilding)
            return;

        _meshObjectsNeedRebuilding = false;
        _currentSample = 0;
        

        //Clear all buffers
        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();

        foreach (var ptObject in _ptObjects)
        {
            //Fetch Mesh and MeshRenderer to access their data
            var meshRenderer = ptObject.GetComponent<MeshRenderer>();
            var mesh = ptObject.GetComponent<MeshFilter>().sharedMesh;

            //Add vertices and remember previous count to offset the indices
            int firstVertex = _vertices.Count;
            _vertices.AddRange(mesh.vertices);

            //Get the index offset and add vertex indices shifted by the current vertex offset
            int firstIndex = _indices.Count;
            var indices = mesh.GetIndices(0);
            _indices.AddRange(indices.Select(index => index + firstVertex));

            //Add material and get its index
            int matIndex = _materialBufferObjects.Count();
            MaterialBufferObject materialObject = new MaterialBufferObject{
                type = (int)ptObject.Type,
                albedo = ptObject.Albedo,
                specular = ptObject.Specular,
                emissive = ptObject.Emissive
            };
            _materialBufferObjects.Add(materialObject);
            Debug.Log(materialObject.specular);

            //Create an object to boundle information of a Mesh and add it
            MeshObject meshObject = new MeshObject
            {
                ModelMatrix = meshRenderer.localToWorldMatrix,
                IndexOffset = firstIndex,
                TriangleCount = indices.Length / 3,
                MaterialIndex = matIndex
            };
            _meshObjects.Add(meshObject);

        }

        CreateComputeBuffer<MeshObject>(ref _meshObjectsBuffer, _meshObjects, 76);
        CreateComputeBuffer<Vector3>(ref _verticesBuffer, _vertices, 12);
        CreateComputeBuffer<int>(ref _indicesBuffer, _indices, 4);
        CreateComputeBuffer<MaterialBufferObject>(ref _materialBuffer, _materialBufferObjects, 40);
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
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RebuildMeshObjectBuffers();
        Render(destination);
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

        Debug.Log(_currentSample);
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
    }
}
