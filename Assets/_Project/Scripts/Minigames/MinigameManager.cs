using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Điều phối 4 loại minigame, nhận kết quả RepairQuality.
/// Thiết kế interface chung IMinigame để 4 loại cùng tuân theo 1 chuẩn input/output.
/// </summary>
public class MinigameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FaultRandomizer faultRandomizer;

    private IMinigame _activeMinigame;

    public bool IsMinigameActive => _activeMinigame != null && _activeMinigame.IsActive;

    // Events
    public System.Action<IMinigame> OnMinigameStarted;
    public System.Action<RepairQuality> OnMinigameCompleted;

    /// <summary>
    /// Bắt đầu một minigame cụ thể với dữ liệu lỗi đã random.
    /// </summary>
    /// <param name="minigame">Loại minigame (Diagnosis/Soldering/Rewiring/Cleaning).</param>
    /// <param name="faultPool">Fault pool của món đồ.</param>
    /// <param name="difficultyLevel">Độ khó.</param>
    public void StartMinigame(IMinigame minigame, List<string> faultPool, int difficultyLevel)
    {
        if (_activeMinigame != null && _activeMinigame.IsActive)
        {
            Debug.LogWarning("[MinigameManager] Đã có minigame đang chạy!");
            return;
        }

        // Random hóa lỗi
        List<string> selectedFaults = faultRandomizer.RandomizeFaults(faultPool, difficultyLevel);

        _activeMinigame = minigame;
        _activeMinigame.OnMinigameCompleted += HandleMinigameCompleted;
        _activeMinigame.Initialize(selectedFaults, difficultyLevel);
        _activeMinigame.StartMinigame();

        OnMinigameStarted?.Invoke(minigame);
    }

    /// <summary>
    /// Hủy minigame hiện tại (VD: cúp điện, sự kiện đặc biệt).
    /// </summary>
    public void AbortCurrentMinigame()
    {
        if (_activeMinigame == null) return;

        _activeMinigame.AbortMinigame();
        CleanupMinigame();
    }

    private void HandleMinigameCompleted(RepairQuality quality)
    {
        OnMinigameCompleted?.Invoke(quality);
        CleanupMinigame();
    }

    private void CleanupMinigame()
    {
        if (_activeMinigame != null)
        {
            _activeMinigame.OnMinigameCompleted -= HandleMinigameCompleted;
            _activeMinigame = null;
        }
    }
}
