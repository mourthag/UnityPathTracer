using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(PathTracer))]
public class PathTracer_Inspector : Editor
{
    int selectedTab = 0;
    string[] tabs = {"Rendering", "Settings" ,"Info"};

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        var pathTracer = (PathTracer)target;

        if(selectedTab == 0)
        {
            if(pathTracer.IsCreatingBVH())
                ProgressBar(0.5f, "Creating BVH...");
            if(pathTracer.IsRendering())
            {
                DoubleText("Current Sample: ", "" + pathTracer.GetProgress() * pathTracer.MaxSamples);
                ProgressBar(pathTracer.GetProgress(), "Remaining: " + pathTracer.GetRemainingTime().ToString(@"hh\:mm\:ss"));
            }
            else
                GUILayout.Label("Rendering has not started yet. Start it by pressing the Play button!");
        }
        else if(selectedTab == 1)
        {
            base.DrawDefaultInspector();
        }
        else if(selectedTab == 2)
        {
            DoubleText("Mesh Count: ", PathTracer.GetMeshCount().ToString());
            DoubleText("Vertex Count: ", PathTracer.GetVertCount().ToString());
            DoubleText("Triangle Count: ", PathTracer.GetTriCount().ToString());
            DoubleText("BVH Node Count: ", PathTracer.GetBVHNodeCount().ToString());
            
            DoubleText("Samples per Second", pathTracer.GetSPS().ToString());
            DoubleText("SPP per Second: ", pathTracer.GetSPPPS().ToString());
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DoubleText(string label, string value)
    {
        GUILayout.BeginHorizontal();

        GUIStyle rightAligned = new GUIStyle(GUI.skin.GetStyle("Label"));
        rightAligned.alignment = TextAnchor.MiddleRight;
        GUIStyle leftAligned = new GUIStyle(GUI.skin.GetStyle("Label"));
        leftAligned.alignment = TextAnchor.MiddleLeft;

        GUILayout.Label(label, leftAligned );
        GUILayout.Label(value, rightAligned);
        
        GUILayout.EndHorizontal();
    }

    void ProgressBar (float value, string label)
    {
        // Get a rect for the progress bar using the same margins as a textfield:
        Rect rect = GUILayoutUtility.GetRect (18, 18, "TextField");
        EditorGUI.ProgressBar (rect, value, label);
        EditorGUILayout.Space ();
    }
}
