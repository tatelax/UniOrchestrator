using System;
using System.Collections.Generic;
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
        private Type[] _allTypes;
        private int _selectedIndex = 0;
        private List<Type> _filteredTypes = new();
        private GUIContent[] _filteredOptions;
        private enum DropdownState { NoSystems, AllAdded, CanAdd }
        private DropdownState _dropdownState = DropdownState.CanAdd;

        void OnEnable()
        {
            var so = (SystemListSO)target;
            _allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(ISystem).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.FullName)
                .ToArray();

            _list = new ReorderableList(so.systemTypeReferences, typeof(TypeReference), true, true, false, false)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "System Execution Order", EditorStyles.boldLabel);
                },
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var soInner = (SystemListSO)target;
                    if (index >= soInner.systemTypeReferences.Count) return;
                    var item = soInner.systemTypeReferences[index];
                    var buttonWidth = 80;
                    var spacing = 8;
                    var checkboxWidth = 20;
                    var labelRect = new Rect(rect.x, rect.y, rect.width - buttonWidth - checkboxWidth - 2 * spacing, rect.height);
                    var checkboxRect = new Rect(rect.x + rect.width - buttonWidth - checkboxWidth - spacing, rect.y + 2, checkboxWidth, rect.height - 4);

                    bool enabled = item.Enabled;
                    bool newEnabled = EditorGUI.Toggle(checkboxRect, enabled);
                    if (newEnabled != enabled)
                    {
                        Undo.RecordObject(soInner, "Toggle System Enabled");
                        item.Enabled = newEnabled;
                        EditorUtility.SetDirty(soInner);

                        Debug.Log($"[SystemListSO] System {(item.Type != null ? item.Type.Name : "(null)")} {(newEnabled ? "ENABLED" : "DISABLED")}", soInner);
                    }

                    EditorGUI.LabelField(labelRect, item.ToString(), EditorStyles.largeLabel);

                    var buttonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y + 2, buttonWidth - spacing, rect.height - 4);
                    GUI.backgroundColor = new Color(1.0f, 0.45f, 0.45f);
                    if (GUI.Button(buttonRect, "Remove"))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Remove", $"Are you sure you want to remove\n\n{item}\n\nfrom the list?", "Yes, Remove", "Cancel"))
                        {
                            string removedName = item.Type != null ? item.Type.Name : "(null)";
                            soInner.systemTypeReferences.RemoveAt(index);
                            EditorUtility.SetDirty(soInner);
                            _selectedIndex = 0;

                            Debug.Log($"[SystemListSO] System REMOVED: {removedName}", soInner);
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
            };
        }

        private void UpdateDropdownOptions(SystemListSO so)
        {
            _filteredTypes.Clear();
            if (_allTypes.Length == 0)
            {
                _dropdownState = DropdownState.NoSystems;
                _filteredOptions = new[] { new GUIContent("No systems found") };
                _filteredTypes.Add(null);
                return;
            }

            var existingNames = new HashSet<string>(so.systemTypeReferences.Where(tr => tr != null).Select(tr => tr.Name));
            foreach (var type in _allTypes)
            {
                if (existingNames.Contains(type.AssemblyQualifiedName)) continue;
                _filteredTypes.Add(type);
            }

            if (_filteredTypes.Count == 0)
            {
                _dropdownState = DropdownState.AllAdded;
                _filteredOptions = new[] { new GUIContent("All systems have been added") };
                _filteredTypes.Add(null);
            }
            else
            {
                _dropdownState = DropdownState.CanAdd;
                _filteredTypes.Insert(0, null);
                _filteredOptions = _filteredTypes
                    .Select((t, i) => i == 0 ? new GUIContent("Select a system…") : new GUIContent(t.Name))
                    .ToArray();
            }
        }

        public override void OnInspectorGUI()
        {
            var so = (SystemListSO)target;
            serializedObject.Update();
            UpdateDropdownOptions(so);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("System List", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This asset holds the list of ISystem implementations to include. Use the dropdown below to add new types.", MessageType.Info);
            EditorGUILayout.Space(10);

            _list.list = so.systemTypeReferences;
            _list.DoLayoutList();

            if (so.systemTypeReferences.Count == 0)
                EditorGUILayout.HelpBox("No systems added yet. Use the dropdown below to add system types.", MessageType.None);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Add New System Type", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            bool dropdownDisabled = _dropdownState != DropdownState.CanAdd;

            EditorGUI.BeginDisabledGroup(dropdownDisabled);

            int newSelected = EditorGUILayout.Popup(
                _selectedIndex,
                _filteredOptions,
                GUILayout.MinWidth(200), GUILayout.MaxWidth(600)
            );
            bool canAdd = !dropdownDisabled && newSelected > 0 && _filteredTypes[newSelected] != null;

            EditorGUI.BeginDisabledGroup(!canAdd);
            GUI.backgroundColor = canAdd ? new Color(0.4f, 0.85f, 0.6f) : Color.white;
            if (GUILayout.Button("Add", GUILayout.Width(80)))
            {
                Type toAdd = _filteredTypes[newSelected];
                if (toAdd != null && !so.systemTypeReferences.Any(tr => tr != null && tr.Name == toAdd.AssemblyQualifiedName))
                {
                    var tr = new TypeReference();
                    tr.SetType(toAdd);
                    so.systemTypeReferences.Add(tr);
                    EditorUtility.SetDirty(so);

                    Debug.Log($"[SystemListSO] System ADDED: {toAdd.Name}", so);
                }
                _selectedIndex = 0;
                GUI.FocusControl(null);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            _selectedIndex = newSelected;

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }
    }
}