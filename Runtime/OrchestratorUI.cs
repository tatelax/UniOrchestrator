using System.Text;
using TMPro;
using UnityEngine;

namespace Orchestrator
{
  public class OrchestratorUI : MonoBehaviour
  {
    [Header("Assign your TextMeshProUGUI here")]
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly StringBuilder _sb = new();

    void Update()
    {
      if (!statusText)
        return;

      _sb.Clear();

      // Bootloader status
      _sb.AppendLine($"<b>Bootloader Status:</b> <color=#00ffff>{Orchestrator.Status}</color>\n");

      // System statuses
      _sb.AppendLine("<b>Systems:</b>");
      foreach (var kvp in Orchestrator.Systems)
      {
        string color = kvp.Value switch
        {
          SystemStatus.Pending => "#888888",
          SystemStatus.Starting => "#FFD600",
          SystemStatus.Running => "#00BFFF",
          SystemStatus.Completed => "#37FF6A",
          SystemStatus.Failed => "#FF4B4B",
          _ => "#FFFFFF"
        };

        _sb.AppendLine($"<color={color}>{kvp.Key}</color>: {kvp.Value}");
      }

      statusText.text = _sb.ToString();
    }
  }
}