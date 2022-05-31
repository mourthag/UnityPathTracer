using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CanEditMultipleObjects]
[CustomEditor(typeof(PathTracingObject))]
public class PathTracingObject_Inspector : Editor
{

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if(GUILayout.Button("Load Material parameters from attached Materials"))
        {
            foreach(var tgt in targets)
            {
                var ptObject = (PathTracingObject)tgt;
                ptObject.LoadUnityMaterials();
            }
            
        }

        base.DrawDefaultInspector();
        
        serializedObject.ApplyModifiedProperties();
    }
}
