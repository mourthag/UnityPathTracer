
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

        EditorGUILayout.PropertyField(serializedObject.FindProperty( "Type"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty( "Color"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty( "Intensity"));

        if(ptLight.Type == PtLightType.Point)
        {
            //Nothing Custom yet
        }
        if(ptLight.Type == PtLightType.Spot)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty( "SpotAngle"));

        }
        if(ptLight.Type == PtLightType.Area)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty( "AreaSize"));
        }

        serializedObject.ApplyModifiedProperties();


    }

}
