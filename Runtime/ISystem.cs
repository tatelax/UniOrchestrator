using Cysharp.Threading.Tasks;

namespace UniOrchestrator
{
  public interface ISystem
  {
    async UniTask Init() { await UniTask.CompletedTask; }

    void Ready() { }

    void Update() { }
  }
}
