using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Helpers;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Orchestrator
{
    public enum BootStatus
    {
        NotStarted,
        Loading,
        Completed,
        Failed
    }

    public enum SystemStatus
    {
        Pending,
        Starting,
        Running,
        Failed
    }

    internal sealed class OrchestratorSystems { }

    public static class Orchestrator
    {
        public static BootStatus Status { get; private set; } = BootStatus.NotStarted;

        private static readonly Dictionary<ISystem, SystemStatus> _systems = new();

        public static IReadOnlyDictionary<ISystem, SystemStatus> Systems => _systems;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static async UniTask Boot()
        {
            Status = BootStatus.Loading;
            OrchestratorLogger.Log("Loading systems...");

            var allSettingsFiles = Resources.LoadAll<OrchestratorSettingsSO>("");

            if (allSettingsFiles.Length == 0)
            {
                OrchestratorLogger.LogError("Unable to find an Orchestrator settings file in Resources. Did you create one?");
                return;
            }

            if (allSettingsFiles.Length > 1)
            {
                OrchestratorLogger.LogError("Found multiple settings files. There should only be one.");
                return;
            }

            var settings = allSettingsFiles[0];
            var systemList = settings.SystemList;

            if (systemList is null)
            {
                OrchestratorLogger.LogError("No systems list specified. Did you assign it in the settings scriptable object?");
                return;
            }

            var systemInstances = LoadSystems(systemList);

            if (systemInstances.Count == 0)
            {
                OrchestratorLogger.LogError("No systems found. Add systems in the system list scriptable object.");
                return;
            }

            OrchestratorLogger.Log($"Found {systemInstances.Count} systems");

            PlayerLoopSystem customSystemsParent = new()
            {
                type = typeof(OrchestratorSystems)
            };

            List<PlayerLoopSystem> customSystems = new();

            var systemFactories = new List<(ISystem system, Func<UniTask> factory)>();

            foreach (var system in systemInstances)
            {
                _systems[system] = SystemStatus.Pending;
                systemFactories.Add((system, async () => await system.Init()));

                customSystems.Add(new PlayerLoopSystem
                {
                    type = system.GetType(),
                    updateDelegate = system.Update
                });
            }

            uint successfulBoots = 0, failedBoots = 0;

            await UniTaskParallelRunner.RunTasksInParallel(
                systemFactories.Select(sf => sf.factory).ToList(),
                onStarted: i =>
                {
                    var system = systemFactories[i].system;
                    _systems[system] = SystemStatus.Starting;
                    OrchestratorLogger.Log($"Task \"{system.GetType().Name}\" started");
                },
                onCompleted: i =>
                {
                    var system = systemFactories[i].system;
                    _systems[system] = SystemStatus.Running;
                    OrchestratorLogger.Log($"Task \"{system.GetType().Name}\" completed");
                    successfulBoots++;
                },
                onFailed: (i, ex) =>
                {
                    var system = systemFactories[i].system;
                    _systems[system] = SystemStatus.Failed;
                    OrchestratorLogger.LogError($"Task \"{system.GetType().Name}\" failed: {ex.Message} \n {ex.StackTrace}");
                    failedBoots++;
                });

            Status = BootStatus.Completed;

            OrchestratorLogger.Log($"System init finished. {successfulBoots} successful boots. {failedBoots} failed boots.");

            if (settings.HaltOnBootFailure && failedBoots > 0)
            {
                OrchestratorLogger.LogError("Halted boot process due to boot failure and Halt On Boot Failure is enabled in Orchestrator settings.");
                return;
            }

            customSystemsParent.subSystemList = customSystems.ToArray();
            PlayerLoopInterface.InsertSystemBefore(customSystemsParent, settings.PlayerLoopBeforeMethod());
        }

        private static HashSet<ISystem> LoadSystems(SystemListSO systemTypeListSO)
        {
            var systems = new HashSet<ISystem>();
            foreach (var typeReference in systemTypeListSO.systemTypeReferences)
            {
                if (!typeReference.Enabled)
                    continue;
                
                var type = typeReference.Type;

                if (type == null)
                {
                    OrchestratorLogger.LogError($"Type '{typeReference}' not found. Did you rename or move the class?");
                    continue;
                }
                
                if (!typeof(ISystem).IsAssignableFrom(type))
                {
                    OrchestratorLogger.LogError($"Type '{typeReference}' does not implement ISystem.");
                    continue;
                }
                
                try
                {
                    if (Activator.CreateInstance(type) is ISystem instance)
                        systems.Add(instance);
                }
                catch (Exception ex)
                {
                    OrchestratorLogger.LogError($"Failed to create instance of {typeReference}: {ex.Message}");
                }
            }
            return systems;
        }

        public static ISystem[] GetAllSystems()
        {
            return _systems.Keys.ToArray();
        }

        public static async UniTask<T> GetSystemAsync<T>() where T : class, ISystem
        {
            var systemInstance = _systems.Keys.OfType<T>().FirstOrDefault();
            if (systemInstance == null)
                throw new InvalidOperationException($"System of type {typeof(T).Name} not found.");

            while (_systems[systemInstance] != SystemStatus.Running)
            {
                await UniTask.Yield();
            }

            return systemInstance;
        }

        public static T GetSystem<T>() where T : class, ISystem
        {
            var systemInstance = _systems.Keys.OfType<T>().FirstOrDefault();
            if (systemInstance == null)
                throw new InvalidOperationException($"System of type {typeof(T).Name} not found.");

            return systemInstance;
        }
    }
}