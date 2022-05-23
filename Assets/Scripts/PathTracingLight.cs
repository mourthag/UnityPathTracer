using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PtLightType {
    Point,
    Spot,
    Directional
}

public struct LightBufferObject {
    public int Type;
    public Vector3 Position;
    public Vector3 Intensity;

    public LightBufferObject(PathTracingLight ptLight){
        this.Type = (int)ptLight.Type;
        if(ptLight.Type == PtLightType.Directional)
            this.Position = ptLight.transform.forward;
        else
            this.Position = ptLight.transform.position;
        this.Intensity = ptLight.Intensity * new Vector3(ptLight.Color.r, ptLight.Color.g, ptLight.Color.b);
    }
}

[RequireComponent(typeof(Light))]
public class PathTracingLight : MonoBehaviour
{
    public PtLightType Type;
    public Color Color;
    public float Intensity = 1.0f;

    
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
                break;
            default:
                this.Type = PtLightType.Point;
                break;
        }

    }

    // Start is called before the first frame update
    void OnEnable()
    {
        ImportParametersFromUnityLight();
        PathTracer.RegisterLight(this);

    }


    // Update is called once per frame
    void OnDisable()
    {
        PathTracer.UnregisterLight(this);
    }
}
