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
        public Vector3 Albedo, Emissive, Transmission;
        public float IOR, Metalness, Roughness;
    }

    public List<MaterialObject> Materials = new List<MaterialObject>();
    private List<MaterialObject> _oldMaterials = new List<MaterialObject>();

    // Start is called before the first frame update
    void OnEnable()
    {
        PathTracer.RegisterObject(this);
        var renderer = GetComponent<MeshRenderer>();
        foreach(var material in renderer.materials)
        {
            MaterialObject mat = new MaterialObject();

            mat.Albedo.x = material.GetColor("_Color").r;
            mat.Albedo.y = material.GetColor("_Color").g;
            mat.Albedo.z = material.GetColor("_Color").b;

            mat.Emissive.x = material.GetColor("_EmissionColor").r;
            mat.Emissive.y = material.GetColor("_EmissionColor").g;
            mat.Emissive.z = material.GetColor("_EmissionColor").b;

            mat.Metalness = 1.0f;// material.GetFloat("_Metallic");
            mat.Roughness = 1.0f - material.GetFloat("_Glossiness") - 0.0001f;
            Materials.Add(mat);
        }
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
        if (!_oldMaterials.Equals(Materials))
        {
            PathTracer.SetMeshObjectsNeedRebuilding();
            _oldMaterials = Materials;
        }
    }

    // Update is called once per frame
    void OnDisable()
    {
        PathTracer.UnregisterObject(this);
    }
}
