using UnityEditor;
using Orchestrator;

[CustomEditor(typeof(OrchestratorSettingsSO))]
internal class OrchestratorSettingsSOEditor : Editor
{
  private bool _showAdvanced = false;

  public override void OnInspectorGUI()
  {
    serializedObject.Update();

    // Draw SystemList and haltOnBootFailure fields normally
    EditorGUILayout.PropertyField(serializedObject.FindProperty("systemList"));
    EditorGUILayout.PropertyField(serializedObject.FindProperty("haltOnBootFailure"));

    // Advanced foldout for updateLoopTime
    EditorGUILayout.Space();
    _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced");
    if (_showAdvanced)
    {
      EditorGUI.indentLevel++;
      EditorGUILayout.PropertyField(serializedObject.FindProperty("updateLoopTime"));
      EditorGUI.indentLevel--;
    }

    serializedObject.ApplyModifiedProperties();
  }
}