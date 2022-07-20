using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class PathTracingObject : MonoBehaviour
{

    public List<PathTracingMaterial> Materials = new List<PathTracingMaterial>();

    public void LoadUnityMaterials() 
    {
        Materials.Clear();
        var renderer = GetComponent<MeshRenderer>();
        foreach(var material in renderer.sharedMaterials)
        {
            bool loadTextures = this.gameObject.activeInHierarchy;
            PathTracingMaterial mat = PathTracingMaterial.FromUnityMaterial(material, loadTextures);
            
            Materials.Add(mat);
        }
    }

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
        /*if (!_oldMaterials.Equals(Materials))
        {
            PathTracer.SetMeshObjectsNeedRebuilding();
            _oldMaterials = Materials;
        }*/
    }

    void OnApplicationQuit()
    {
        foreach(var material in Materials)
        {
            material.UnregisterTextures();
        }
    }

    void OnDisable()
    {
        PathTracer.UnregisterObject(this);
        
        foreach(var material in Materials)
        {
            material.UnregisterTextures();
        }
    }
}
