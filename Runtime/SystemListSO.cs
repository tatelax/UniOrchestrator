using System.Collections.Generic;
using UnityEngine;

namespace UniOrchestrator
{
  [CreateAssetMenu(menuName = "Orchestrator/System List", fileName = "New System List", order = 1)]
  public class SystemListSO : ScriptableObject
  {
    public List<TypeReference> systemTypeReferences = new();
  }
}