using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class PathTracingObject : MonoBehaviour
{
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

    // Update is called once per frame
            void OnDisable()
    {
        PathTracer.UnregisterObject(this);
    }
}
