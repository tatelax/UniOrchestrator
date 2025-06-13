using System;
using UnityEngine;

namespace UniOrchestrator
{
  public enum RunUpdateBefore
  {
    ScriptRunBehaviourUpdate
  }
  
  [CreateAssetMenu(menuName = "Orchestrator/Settings", fileName = "New Orchestrator Settings", order = 0)]
  public sealed class OrchestratorSettingsSO : ScriptableObject
  {
    [SerializeField] private SystemListSO systemList;
    [SerializeField] private bool haltOnBootFailure;
    
    // Advanced
    [SerializeField] private RunUpdateBefore updateLoopTime = RunUpdateBefore.ScriptRunBehaviourUpdate;
    
    public SystemListSO SystemList => systemList;
    public bool HaltOnBootFailure => haltOnBootFailure;

    public Type PlayerLoopBeforeMethod()
    {
      return updateLoopTime switch
      {
        RunUpdateBefore.ScriptRunBehaviourUpdate => typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate),
        _ => null
      };
    }
  }
}
