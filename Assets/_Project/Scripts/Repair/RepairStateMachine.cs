using UnityEngine;

/// <summary>
/// State machine quản lý quy trình sửa chữa: Nhận đồ → Đang sửa → Trả đồ.
/// Điều phối giữa ItemInspect (khám bệnh) và MinigameManager (sửa chữa).
/// </summary>
public class RepairStateMachine : MonoBehaviour
{
    public enum RepairState
    {
        Idle,           // Đang chờ khách
        Receiving,      // Nhận đồ từ khách
        Inspecting,     // Soi/khám bệnh sơ bộ (FPP)
        Repairing,      // Đang chơi minigame sửa chữa
        Completed,      // Sửa xong, chờ trả đồ
        Returning       // Trả đồ cho khách, thu tiền
    }

    [SerializeField] private RepairState currentState = RepairState.Idle;

    public RepairState CurrentState => currentState;

    // Events
    public System.Action<RepairState> OnStateChanged;
    public System.Action<RepairQuality> OnRepairCompleted;

    /// <summary>
    /// Chuyển sang trạng thái mới.
    /// </summary>
    public void TransitionTo(RepairState newState)
    {
        if (currentState == newState) return;

        ExitState(currentState);
        currentState = newState;
        EnterState(newState);

        OnStateChanged?.Invoke(newState);
    }

    private void EnterState(RepairState state)
    {
        switch (state)
        {
            case RepairState.Idle:
                // Sẵn sàng nhận khách mới
                break;
            case RepairState.Receiving:
                // Hiển thị animation nhận đồ
                break;
            case RepairState.Inspecting:
                // Kích hoạt ItemInspect
                break;
            case RepairState.Repairing:
                // Kích hoạt MinigameManager
                break;
            case RepairState.Completed:
                // Hiển thị kết quả RepairQuality
                break;
            case RepairState.Returning:
                // Animation trả đồ, tính tiền
                break;
        }
    }

    private void ExitState(RepairState state)
    {
        // Dọn dẹp khi rời trạng thái cũ
    }

    /// <summary>
    /// Gọi khi minigame kết thúc, truyền kết quả chất lượng.
    /// </summary>
    public void CompleteRepair(RepairQuality quality)
    {
        OnRepairCompleted?.Invoke(quality);
        TransitionTo(RepairState.Completed);
    }

    /// <summary>
    /// Gọi khi trả đồ cho khách xong, quay về Idle.
    /// </summary>
    public void ReturnToIdle()
    {
        TransitionTo(RepairState.Idle);
    }
}
