using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public static class UniTaskParallelRunner
{
  // Run UniTasks in parallel and notify when each completes or errors
  public static async UniTask RunTasksInParallel(
    IEnumerable<Func<UniTask>> taskFactories,
    Action<int> onStarted = null,
    Action<int> onCompleted = null,
    Action<int, Exception> onFailed = null)
  {
    var tasks = new List<UniTask>();
    int i = 0;
    foreach (var factory in taskFactories)
    {
      int taskIndex = i++;
      tasks.Add(RunWithCallbacks(factory, taskIndex, onStarted, onCompleted, onFailed));
    }

    await UniTask.WhenAll(tasks);
  }

  private static async UniTask RunWithCallbacks(
    Func<UniTask> factory, int index,
    Action<int> onStarted,
    Action<int> onCompleted,
    Action<int, Exception> onFailed)
  {
    onStarted?.Invoke(index);
    try
    {
      await factory();
      onCompleted?.Invoke(index);
    }
    catch (Exception ex)
    {
      onFailed?.Invoke(index, ex);
    }
  }
}