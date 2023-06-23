using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PtLightType {
    Point,
    Spot,
    Directional,
    Area
}

public struct LightBufferObject {
    public int Type;
    public Matrix4x4 LocalToWorld;
    public Vector3 Intensity;
    public float SpotAngle;
    public Vector2 AreaSize;

    public LightBufferObject(PathTracingLight ptLight){
        this.Type = (int)ptLight.Type;
        this.LocalToWorld = ptLight.transform.localToWorldMatrix;
        this.Intensity = ptLight.Intensity * new Vector3(ptLight.Color.r, ptLight.Color.g, ptLight.Color.b);
        this.SpotAngle = ptLight.SpotAngle;
        this.AreaSize = ptLight.AreaSize;
    }
}

[RequireComponent(typeof(Light))]
public class PathTracingLight : MonoBehaviour
{
    public PtLightType Type;
    public Color Color;
    public float Intensity = 1.0f;
    public float SpotAngle = 90.0f;
    public Vector2 AreaSize = new Vector2(1,1);

#if UNITY_EDITOR

    public void ImportParametersFromUnityLight(){
       var light = GetComponent<Light>();

        Color = light.color;
        Intensity = light.intensity;

        var type = light.type;
        switch(type)
        {
            case LightType.Directional:
                this.Type = PtLightType.Directional;
                break;
            case LightType.Point:
                this.Type = PtLightType.Point;
                break;
            case LightType.Spot:
                this.Type = PtLightType.Spot;
                SpotAngle = light.spotAngle;
                break;            
            case LightType.Area:
                this.Type = PtLightType.Area;
                this.AreaSize = light.areaSize;
                break;
            default:
                this.Type = PtLightType.Point;
                break;
        }

    }
#endif

    // Start is called before the first frame update
    void OnEnable()
    {
        PathTracer.RegisterLight(this);

    }


    // Update is called once per frame
    void OnDisable()
    {
        PathTracer.UnregisterLight(this);
    }
}
