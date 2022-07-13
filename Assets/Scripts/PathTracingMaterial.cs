using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public struct PtMaterialBufferObject
{
    public Vector3 Albedo, Emission, Transmission;
    public float IOR, Metalness, Roughness;
    public int AlbedoTextureId, EmissionTextureId, NormalTextureId, MRTextureId;
}

[Serializable]
public class PathTracingMaterial
{
    public static List<Texture2D> MaterialAlbedoTextures = new List<Texture2D>();
    public static List<Texture2D> MaterialMRTextures = new List<Texture2D>();
    public static List<Texture2D> MaterialNormalTextures = new List<Texture2D>();
    public static List<Texture2D> MaterialEmissionTextures = new List<Texture2D>();

    public Color Albedo;
    public Color Emission;
    public Color Transmission;
    public float IOR = 1.459f, Metalness, Roughness;

    public Texture2D AlbedoTexture;
    public Texture2D MRTexture;
    public Texture2D NormalTexture;
    public Texture2D EmissionTexture;

    private int _AlbedoTextureID = -1;
    private int _MRTextureID = -1;
    private int _NormalTextureID = -1;
    private int _EmissionTextureID = -1;

    private static List<Texture2DArray> _CreateTexArrays(List<Texture2D> TexList)
    {
        var texArrays = new List<Texture2DArray>();
        Debug.Log(TexList.Count);
        if(TexList.Count == 0)
        {
            //return empty texture
            texArrays.Add(new Texture2DArray(2048, 2048, 1, TextureFormat.RGBA32, false));
        } else {

            Texture2DArray tex2dArray = new Texture2DArray(2048, 
                                                           2048, 
                                                           TexList.Count, 
                                                           TexList[0].format, 
                                                           false);

            for(int i = 0; i < TexList.Count; i++)
            {
                Graphics.CopyTexture(TexList[i], 0, 0, tex2dArray, i, 0);
            }
            texArrays.Add(tex2dArray);
        }
        return texArrays;
    }

    public static List<Texture2DArray> CreateAlbedoTextureArrays()
    {
        return _CreateTexArrays(MaterialAlbedoTextures);
    }

    public static List<Texture2DArray> CreateNormalTextureArrays()
    {
        return _CreateTexArrays(MaterialNormalTextures);
    }

    public static List<Texture2DArray> CreateMRTextureArrays()
    {
        return _CreateTexArrays(MaterialMRTextures);
    }
    
    public static List<Texture2DArray> CreateEmissionTextureArrays()
    {
        return _CreateTexArrays(MaterialEmissionTextures);
    }

    public static PathTracingMaterial FromUnityMaterial(Material unityMaterial, bool loadTextures)
    {
        var mat = new PathTracingMaterial();
        mat.IOR = 1.459f;

        if(loadTextures && unityMaterial.GetTexture("_MainTex"))
            mat.AlbedoTexture = (Texture2D)unityMaterial.GetTexture("_MainTex");
        if(loadTextures && unityMaterial.GetTexture("_MetallicGlossMap"))
            mat.MRTexture = (Texture2D)unityMaterial.GetTexture("_MetallicGlossMap");
        if(loadTextures && unityMaterial.GetTexture("_BumpMap"))
            mat.NormalTexture = (Texture2D)unityMaterial.GetTexture("_BumpMap");
        if(loadTextures && unityMaterial.GetTexture("_EmissionMap"))
            mat.EmissionTexture = (Texture2D)unityMaterial.GetTexture("_EmissionMap");

        mat.Albedo = unityMaterial.GetColor("_Color");
        mat.Emission = unityMaterial.GetColor("_EmissionColor");
        mat.Metalness = unityMaterial.GetFloat("_Metallic");
        mat.Roughness = 1.0f - unityMaterial.GetFloat("_Glossiness") - 0.0001f;

        if (unityMaterial.name.Contains( "Glass"))
        {
            mat.Transmission = new Color(1, 1, 1);
            mat.Albedo = new Color(0, 0, 0);
        }
        return mat;

    }

    ~PathTracingMaterial()
    {
        UnregisterTextures();
    }

    public void RegisterTextures()
    {
        //The following line also removes the same texture from other materials, equlality check and taking the index should help
        //UnregisterTextures();
        if(AlbedoTexture) {
            _AlbedoTextureID = RegisterTextureToList(MaterialAlbedoTextures, AlbedoTexture);
        }
        if(MRTexture){
            _MRTextureID = RegisterTextureToList(MaterialMRTextures, MRTexture); 
        }
        if(NormalTexture){
            _NormalTextureID = RegisterTextureToList(MaterialNormalTextures, NormalTexture); 
        }
        if(EmissionTexture){
            _EmissionTextureID = RegisterTextureToList(MaterialEmissionTextures, EmissionTexture); 
        }    
    }
    
    private int RegisterTextureToList(List<Texture2D> list, Texture2D texture)
    {
        if(!list.Contains(texture))
        {
            list.Add(texture);
        }
        return list.IndexOf(texture);
    }

    public void UnregisterTextures() {
        if(AlbedoTexture) {
            _AlbedoTextureID = -1;
            MaterialAlbedoTextures.Remove(AlbedoTexture);
        }
        if(MRTexture){
            _MRTextureID = -1;
            MaterialMRTextures.Remove(MRTexture);    
        }
        if(NormalTexture){
            _NormalTextureID = -1;
            MaterialNormalTextures.Remove(NormalTexture);
        }
        if(EmissionTexture)
        {
            _EmissionTextureID = -1;
            MaterialEmissionTextures.Add(EmissionTexture);
        }   

    }

    public PtMaterialBufferObject ToPtMaterialBufferObject()
    {
        PtMaterialBufferObject obj = new PtMaterialBufferObject();

        obj.Albedo = new Vector3(Albedo.r, Albedo.g, Albedo.b);
        obj.Emission = new Vector3(Emission.r, Emission.g, Emission.b);
        obj.Transmission = new Vector3(Transmission.r, Transmission.g, Transmission.b);

        obj.Roughness = Roughness;
        obj.Metalness = Metalness;
        obj.IOR = IOR;

        obj.AlbedoTextureId = _AlbedoTextureID; 
        obj.EmissionTextureId = _EmissionTextureID; 
        obj.MRTextureId = _MRTextureID; 
        obj.NormalTextureId = _NormalTextureID; 

        return obj;
    }
}
