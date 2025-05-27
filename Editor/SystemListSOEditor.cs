using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Orchestrator.Editor
{
    [CustomEditor(typeof(SystemListSO))]
    public class SystemListSOEditor : UnityEditor.Editor
    {
        private ReorderableList _list;
        private string[] _allTypeNames;
        private Type[] _allTypes;
        private int _selectedIndex = 0; // Start at 0 for placeholder

        private static readonly string ListTooltip = "Drag to reorder. These are the names of all concrete system types (implementing ISystem) you've added.";
        private static readonly string DropdownTooltip = "Select an available system type to add it to the list.";
        private static readonly string AddButtonTooltip = "Add the selected system type to the list. Each type can only be added once.";
        private static readonly string RemoveButtonTooltip = "Remove this system type from the list.";

        void OnEnable()
        {
            var so = (SystemListSO)target;

            // Gather all concrete, non-abstract ISystem types
            _allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(ISystem).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.FullName)
                .ToArray();

            _allTypeNames = _allTypes.Select(t => t.FullName).ToArray();

            // Setup ReorderableList
            _list = new ReorderableList(so.systemTypeNames, typeof(string), true, true, false, false)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, new GUIContent(
                        "System Execution Order",
                        ListTooltip
                    ), EditorStyles.boldLabel);
                },
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var item = so.systemTypeNames[index];

                    var buttonWidth = 80;
                    var spacing = 8;
                    var buttonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y + 2, buttonWidth - spacing, rect.height - 4);
                    var labelRect = new Rect(rect.x, rect.y, rect.width - buttonWidth - spacing, rect.height);

                    EditorGUI.LabelField(labelRect, new GUIContent(item, item), EditorStyles.largeLabel);

                    GUI.backgroundColor = new Color(1.0f, 0.45f, 0.45f); // soft red

                    if (GUI.Button(buttonRect, new GUIContent("Remove", RemoveButtonTooltip)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Confirm Remove",
                                $"Are you sure you want to remove\n\n{item}\n\nfrom the list?",
                                "Yes, Remove",
                                "Cancel"))
                        {
                            so.systemTypeNames.RemoveAt(index);
                            EditorUtility.SetDirty(so);
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
            };
        }

        private GUIContent[] GetSystemTypeOptions()
        {
            var options = new GUIContent[_allTypeNames.Length + 1];
            options[0] = new GUIContent("Select a system…", "Pick a system type to add.");
            for (int i = 0; i < _allTypeNames.Length; i++)
                options[i + 1] = new GUIContent(_allTypeNames[i], _allTypeNames[i]);
            return options;
        }

        public override void OnInspectorGUI()
        {
            var so = (SystemListSO)target;
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                new GUIContent(
                    "System List",
                    "Manage systems that will be executed. Drag to reorder. Add only types implementing ISystem."
                ),
                EditorStyles.whiteLargeLabel
            );
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This asset holds the list of ISystem implementations to include. Use the dropdown below to add new types.", MessageType.Info);

            EditorGUILayout.Space(10);

            _list.list = so.systemTypeNames;
            _list.DoLayoutList();

            if (so.systemTypeNames.Count == 0)
            {
                EditorGUILayout.HelpBox("No systems added yet. Use the dropdown below to add system types.", MessageType.None);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                new GUIContent("Add New System Type", DropdownTooltip),
                EditorStyles.boldLabel
            );

            // Dropdown with placeholder, no label next to dropdown
            var options = GetSystemTypeOptions();

            EditorGUILayout.BeginHorizontal();
            _selectedIndex = EditorGUILayout.Popup(
                _selectedIndex,
                options,
                GUILayout.MinWidth(200), GUILayout.MaxWidth(600)
            );

            // Can only add if a real type is selected (index > 0) and not already present
            bool canAdd = _selectedIndex > 0
                          && !so.systemTypeNames.Contains(_allTypeNames[_selectedIndex - 1]);

            EditorGUI.BeginDisabledGroup(!canAdd);
            GUI.backgroundColor = canAdd ? new Color(0.4f, 0.85f, 0.6f) : Color.white; // soft green
            if (GUILayout.Button(new GUIContent("Add", AddButtonTooltip), GUILayout.Width(80)))
            {
                string typeName = _allTypeNames[_selectedIndex - 1];
                if (!so.systemTypeNames.Contains(typeName))
                {
                    so.systemTypeNames.Add(typeName);
                    EditorUtility.SetDirty(so);
                }
                _selectedIndex = 0; // reset to placeholder
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }
    }
}