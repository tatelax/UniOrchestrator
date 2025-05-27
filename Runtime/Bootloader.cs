using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Orchestrator
{
  internal sealed class OrchestratorSystems { }

  internal static class Bootloader
  {
    private static HashSet<ISystem> _loadingSystems;
    private static HashSet<ISystem> _loadedSystems;
  
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static async UniTask LoadSystems()
    {
      Debug.Log("Loading systems...");
      
      var systems = Resources.Load<SystemListSO>("ExampleSystems");

      _loadingSystems = LoadSystems(systems);

      Debug.Log($"Found {_loadingSystems.Count} systems");
      
      PlayerLoopSystem customSystemsParent = new ()
      {
        type = typeof(OrchestratorSystems)
      };

      List<PlayerLoopSystem> customSystems = new();
      
      var e = _loadingSystems.GetEnumerator();

      var systemFactories = new List<(ISystem system, Func<UniTask> factory)>();      
      
      while (e.MoveNext())
      {
        ISystem system = e.Current;

        if (system is null) 
          continue;
        
        systemFactories.Add((system, async () => await system.Init()));
 
        PlayerLoopSystem newSystem = new()
        {
          type = system.GetType(),
          updateDelegate = system.Update
        };

        customSystems.Add(newSystem);
      }

      uint successfulBoots = 0, failedBoots = 0;
      
      await UniTaskParallelRunner.RunTasksInParallel(
        systemFactories.Select(sf => sf.factory).ToList(),
        onStarted: i =>
        {
          Debug.Log($"Task {systemFactories[i].system.GetType().Name} started");
        },
        onCompleted: i =>
        {
          Debug.Log($"Task {systemFactories[i].system.GetType().Name} completed");
          successfulBoots++;
        },
        onFailed: (i, ex) =>
        {
          Debug.LogError($"Task {systemFactories[i].system.GetType().Name} failed: {ex.Message}");
          failedBoots++;
        }); 

      Debug.Log($"System init finished. {successfulBoots} successful boots. {failedBoots} failed boots.");
      
      customSystemsParent.subSystemList = customSystems.ToArray();
    
      PlayerLoopInterface.InsertSystemBefore(customSystemsParent, typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate));

      // var sb = new StringBuilder();
      // PlayerLoopInterface.ShowPlayerLoop(PlayerLoop.GetCurrentPlayerLoop(), sb, 0);
      // Debug.Log(sb.ToString());
    }

    private static void OnLoadCompleted()
    {
      throw new NotImplementedException();
    }

    private static HashSet<ISystem> LoadSystems(SystemListSO systemTypeListSO)
    {
      var systems = new HashSet<ISystem>();
      foreach (var typeName in systemTypeListSO.systemTypeNames)
      {
        // Get the type
        var type = Type.GetType(typeName);
        if (type == null)
        {
          Debug.LogError($"Type '{typeName}' not found. Did you rename or move the class?");
          continue;
        }
        if (!typeof(ISystem).IsAssignableFrom(type))
        {
          Debug.LogError($"Type '{typeName}' does not implement ISystem.");
          continue;
        }
        try
        {
          var instance = Activator.CreateInstance(type) as ISystem;
          if (instance != null)
            systems.Add(instance);
        }
        catch (Exception ex)
        {
          Debug.LogError($"Failed to create instance of {typeName}: {ex.Message}");
        }
      }
      return systems;
    }
  }
}