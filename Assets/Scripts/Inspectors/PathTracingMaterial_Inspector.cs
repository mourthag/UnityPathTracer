using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(PathTracingMaterial))]
public class PathTracingMaterial_Drawer : PropertyDrawer
{

    override public float GetPropertyHeight(SerializedProperty property, GUIContent label){
        float totalHeight = 0;

        float spacing = EditorGUIUtility.standardVerticalSpacing;

        totalHeight += EditorGUIUtility.singleLineHeight;
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Albedo"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Emission"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Transmission"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("IOR"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Metalness"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Roughness"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("AlbedoTexture"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("MRTexture"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("NormalTexture"), label);
        totalHeight += spacing;
        totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("EmissionTexture"), label);
        
        return totalHeight;
    }

    Rect CalculateRect(Rect previousRect, SerializedProperty newProp)
    {
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return new Rect(previousRect.x, previousRect.y + previousRect.height + spacing, previousRect.width, EditorGUI.GetPropertyHeight(newProp));
    }
    

    // Draw the property inside the given rect
    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);
        
        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 1;

        var rect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
        
        EditorGUI.PropertyField(rect, property.FindPropertyRelative("Albedo"));


        var EmissionProp = property.FindPropertyRelative("Emission");
        rect = CalculateRect(rect, EmissionProp);
        EditorGUI.PropertyField(rect, EmissionProp);
        
        var TransmissionProp = property.FindPropertyRelative("Transmission");
        rect = CalculateRect(rect, TransmissionProp);
        EditorGUI.PropertyField(rect, TransmissionProp);
        
        var IORProp = property.FindPropertyRelative("IOR");
        rect = CalculateRect(rect, IORProp);
        EditorGUI.PropertyField(rect, IORProp);

        var MetalnessProp = property.FindPropertyRelative("Metalness");
        rect = CalculateRect(rect, MetalnessProp);
        EditorGUI.Slider(rect, MetalnessProp, 0.0f, 1.0f);

        var RoughnessProp = property.FindPropertyRelative("Roughness");
        rect = CalculateRect(rect, RoughnessProp);
        EditorGUI.Slider(rect, RoughnessProp, 0.0f, 1.0f);

        var AlbedoTexProp = property.FindPropertyRelative("AlbedoTexture");
        rect = CalculateRect(rect, AlbedoTexProp);
        EditorGUI.PropertyField(rect, AlbedoTexProp);

        var MRTexProp = property.FindPropertyRelative("MRTexture");
        rect = CalculateRect(rect, MRTexProp);
        EditorGUI.PropertyField(rect, MRTexProp);

        var NormalTexProp = property.FindPropertyRelative("NormalTexture");
        rect = CalculateRect(rect, NormalTexProp);
        EditorGUI.PropertyField(rect, NormalTexProp);

        var EmissionTexProp = property.FindPropertyRelative("EmissionTexture");
        rect = CalculateRect(rect, EmissionTexProp);
        EditorGUI.PropertyField(rect, EmissionTexProp);

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}