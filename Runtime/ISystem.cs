using Cysharp.Threading.Tasks;

namespace Orchestrator
{
  public interface ISystem
  {
    async UniTask Init() { await UniTask.CompletedTask; }
    void Update() { }
  }
}
