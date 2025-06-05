using UnityEngine;

namespace Orchestrator
{
  public static class OrchestratorLogger
  {
    private const string Prefix = "[Orchestrator] ";

    public static void Log(string message)
    {
      Debug.Log(Prefix + message);
    }

    public static void Log(string message, Object context)
    {
      Debug.Log(Prefix + message, context);
    }

    public static void LogWarning(string message)
    {
      Debug.LogWarning(Prefix + message);
    }

    public static void LogWarning(string message, Object context)
    {
      Debug.LogWarning(Prefix + message, context);
    }

    public static void LogError(string message)
    {
      Debug.LogError(Prefix + message);
    }

    public static void LogError(string message, Object context)
    {
      Debug.LogError(Prefix + message, context);
    }

    public static void LogException(System.Exception exception)
    {
      Debug.LogException(exception);
    }

    public static void LogException(System.Exception exception, Object context)
    {
      Debug.LogException(exception, context);
    }
  }
}