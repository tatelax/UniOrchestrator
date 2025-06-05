using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Orchestrator;

[CustomPropertyDrawer(typeof(TypeReference))]
public class TypeReferenceDrawer : PropertyDrawer
{
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
  {
    var typeNameProp = property.FindPropertyRelative("_typeName");

    // Find all types that match your criteria, e.g., subclasses of IMyInterface
    var availableTypes = AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(a => a.GetTypes())
      .Where(t => typeof(ISystem).IsAssignableFrom(t))
      .OrderBy(t => t.Name)
      .ToArray();

    var typeNames = availableTypes.Select(t => t.FullName).ToArray();
    var displayNames = availableTypes.Select(t => t.Name).ToArray();

    int selectedIndex = Mathf.Max(0, Array.IndexOf(typeNames, typeNameProp.stringValue));
    int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayNames);

    if (newIndex != selectedIndex)
    {
      typeNameProp.stringValue = typeNames[newIndex];
    }
  }
}
