using System.Collections.Generic;
using UnityEngine;

namespace Orchestrator
{
  [CreateAssetMenu(menuName = "Orchestrator/System List", fileName = "New System List")]
  public class SystemListSO : ScriptableObject
  {
    public List<string> systemTypeNames = new();
  }
}