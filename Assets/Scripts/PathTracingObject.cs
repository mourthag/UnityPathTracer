using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PathTracingObject : MonoBehaviour
{
    [Serializable]
    public struct MaterialObject
    {
        public Vector3 Albedo, Specular, Emissive, Transmission;
        public float IOR, Metalness, Roughness;
    }

    public MaterialObject Material;
    private MaterialObject _oldMaterial;

    // Start is called before the first frame update
    void OnEnable()
    {
        PathTracer.RegisterObject(this);
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            PathTracer.SetMeshObjectsNeedRebuilding();
            transform.hasChanged = false;
        }
    }

    private void OnValidate()
    {
        if (!_oldMaterial.Equals(Material))
        {
            PathTracer.SetMeshObjectsNeedRebuilding();
            _oldMaterial = Material;
        }
    }

    // Update is called once per frame
    void OnDisable()
    {
        PathTracer.UnregisterObject(this);
    }
}
