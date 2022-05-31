using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

//[CustomPropertyDrawer(typeof(MaterialObject))]
public class PathTracingMaterial_Drawer : PropertyDrawer
{
    Color albedo = Color.white;

    public float GetPropertyHeight(SerializedProperty property, GUIContent label){
        return 60;
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);
        
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        EditorGUI.BeginChangeCheck();

        var AlbedoProperty = property.FindPropertyRelative("Albedo");
        var albedoRect = new Rect(position.x, position.y + 30, position.width, 30);
        albedo = new Color(AlbedoProperty.vector3Value.x, AlbedoProperty.vector3Value.y, AlbedoProperty.vector3Value.z);
        albedo = EditorGUI.ColorField(albedoRect, "Albedo", albedo);
        
        if(EditorGUI.EndChangeCheck())
            AlbedoProperty.vector3Value = new Vector3(albedo.r, albedo.g, albedo.b);
        
        EditorGUI.EndProperty();
    }
}
