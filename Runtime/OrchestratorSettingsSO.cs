using UnityEngine;

namespace Orchestrator
{
  [CreateAssetMenu(menuName = "Orchestrator/Settings", fileName = "New Orchestrator Settings")]
  internal sealed class OrchestratorSettingsSO : ScriptableObject
  {
    [SerializeField] private SystemListSO systemList;
    [SerializeField] private bool haltOnBootFailure;

    public SystemListSO SystemList => systemList;
  }
}
