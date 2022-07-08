using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MaterialObject
{
    public Vector3 Albedo;
    public int AlbedoTexId;
    public Vector3 Emissive, Transmission;
    public float IOR, Metalness, Roughness;
}

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class PathTracingObject : MonoBehaviour
{
    public static List<Texture> MaterialTextures = new List<Texture>();
    //public static Texture2DArray MaterialTextures;
    public List<MaterialObject> Materials = new List<MaterialObject>();
    private List<MaterialObject> _oldMaterials = new List<MaterialObject>();

    public static Texture2DArray CreateTexArray()
    {
        if(MaterialTextures.Count == 0)
        {
            Texture2DArray newArray = new Texture2DArray(2048, 
                                            2048, 
                                            1, 
                                            TextureFormat.DXT1, 
                                            false);
            return newArray;
        } else {

            Texture2DArray newArray = new Texture2DArray(2048, 
                                                        2048, 
                                                        MaterialTextures.Count, 
                                                        TextureFormat.DXT1, 
                                                        false);
            for(int i = 0; i < MaterialTextures.Count; i++)
            {
                Graphics.CopyTexture(MaterialTextures[i], 0, 0, newArray, i, 0);
            }
            newArray.Apply();
            return newArray;
        }
    }

    public void LoadUnityMaterials() 
    {
        Materials.Clear();
        var renderer = GetComponent<MeshRenderer>();
        foreach(var material in renderer.sharedMaterials)
        {
            MaterialObject mat = new MaterialObject();

            mat.IOR = 1.459f;

            if(material.GetTexture("_MainTex"))
            {
                mat.AlbedoTexId = MaterialTextures.Count;
                MaterialTextures.Add(material.GetTexture("_MainTex"));
            }
            else
                mat.AlbedoTexId = -1;

            mat.Albedo.x = material.GetColor("_Color").r;
            mat.Albedo.y = material.GetColor("_Color").g;
            mat.Albedo.z = material.GetColor("_Color").b;

            mat.Emissive.x = material.GetColor("_EmissionColor").r;
            mat.Emissive.y = material.GetColor("_EmissionColor").g;
            mat.Emissive.z = material.GetColor("_EmissionColor").b;

            mat.Metalness = material.GetFloat("_Metallic");
            mat.Roughness = 1.0f - material.GetFloat("_Glossiness") - 0.0001f;

            if (material.name.Contains( "Glass"))
            {
                mat.Transmission = new Vector3(1, 1, 1);
                mat.Albedo = new Vector3(0, 0, 0);
            }

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
        
        var renderer = GetComponent<MeshRenderer>();
        foreach(var material in renderer.sharedMaterials)
        {
            if(material.GetTexture("_MainTex"))
            {
                MaterialTextures.Remove(material.GetTexture("_MainTex"));
            }
        }
    }
}
