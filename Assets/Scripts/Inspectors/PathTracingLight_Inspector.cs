
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathTracingLight))]
public class PathTracingLight_Inspector : Editor
{

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        var ptLight = (PathTracingLight)target;
        if(GUILayout.Button("Load from Unity Light"))
        {
            ptLight.ImportParametersFromUnityLight();
        }

        base.DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();


    }

}
