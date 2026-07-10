using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Điều phối và quản lý toàn bộ Minigame Nối dây (Rewiring) - Minigame số 3.
/// Thực thi chuẩn IMinigame theo thiết kế đơn giản, chuẩn xác:
/// - Nối thông mạch tất cả các cọc mà KHÔNG CÓ dây nào đâm chéo/cắt ngang qua nhau -> HOÀN HẢO (Perfect).
/// - Nối thông mạch nhưng có đường dây đâm chéo/cắt đè lên nhau -> NỐI ẨU (Passable - rủi ro chập mạch).
/// </summary>
public class RewiringController : MonoBehaviour, IMinigame
{
    [Header("Cấu hình Minigame Nối dây")]
    [SerializeField] private string minigameName = "Nối dây";

    [Header("Danh sách Cọc và Dây hiện tại")]
    [SerializeField] private List<RewiringTerminal> allTerminals = new List<RewiringTerminal>();
    [SerializeField] private List<RewiringWire> activeWires = new List<RewiringWire>();

    private bool _isActive = false;
    private int _currentDifficulty = 1;
    private List<string> _assignedFaults = new List<string>();

    // IMinigame Properties
    public string MinigameName => minigameName;
    public bool IsActive => _isActive;

    // IMinigame Event
    public event System.Action<RepairQuality> OnMinigameCompleted;

    /// <summary>
    /// Khởi tạo minigame với danh sách lỗi từ hệ thống random và cấp độ khó.
    /// </summary>
    public void Initialize(List<string> faults, int difficultyLevel)
    {
        _assignedFaults = faults != null ? new List<string>(faults) : new List<string>();
        _currentDifficulty = Mathf.Max(1, difficultyLevel);
    }

    /// <summary>
    /// Bắt đầu minigame.
    /// </summary>
    public void StartMinigame()
    {
        _isActive = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hủy minigame (khi cúp điện, hết giờ hoặc thoát ra).
    /// </summary>
    public void AbortMinigame()
    {
        if (!_isActive) return;
        _isActive = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Xóa toàn bộ danh sách cọc (khi chuyển/tạo lại bo mạch mới).
    /// </summary>
    public void ClearAllTerminals()
    {
        allTerminals.Clear();
    }

    /// <summary>
    /// Đăng ký cọc mới vào danh sách quản lý.
    /// </summary>
    public void RegisterTerminal(RewiringTerminal terminal)
    {
        if (terminal != null && !allTerminals.Contains(terminal))
        {
            allTerminals.Add(terminal);
        }
    }

    /// <summary>
    /// Thêm sợi dây nối giữa 2 cọc vào danh sách dây đang chạy trên mạch.
    /// </summary>
    public void AddWire(RewiringWire wire)
    {
        if (wire != null && !activeWires.Contains(wire))
        {
            activeWires.Add(wire);
            wire.StartTerminal?.ConnectWire();
            wire.EndTerminal?.ConnectWire();
        }
    }

    /// <summary>
    /// Xóa một sợi dây khỏi danh sách quản lý (khi vẽ lại dây mới).
    /// </summary>
    public void RemoveWire(RewiringWire wire)
    {
        if (wire != null && activeWires.Contains(wire))
        {
            activeWires.Remove(wire);
        }
    }

    /// <summary>
    /// Hoàn tác (Undo) đường dây vừa được nối gần nhất.
    /// </summary>
    public RewiringWire UndoLastWire()
    {
        if (activeWires.Count == 0) return null;

        RewiringWire lastWire = activeWires[activeWires.Count - 1];
        if (lastWire != null)
        {
            lastWire.StartTerminal?.DisconnectWire();
            lastWire.EndTerminal?.DisconnectWire();
            activeWires.RemoveAt(activeWires.Count - 1);
            if (Application.isPlaying) Destroy(lastWire.gameObject);
            else DestroyImmediate(lastWire.gameObject);
        }
        else
        {
            activeWires.RemoveAt(activeWires.Count - 1);
        }
        return lastWire;
    }

    /// <summary>
    /// Xóa toàn bộ dây trên mạch (Reset lượt đấu dây).
    /// </summary>
    public void ClearAllWires()
    {
        foreach (var wire in activeWires)
        {
            if (wire != null)
            {
                wire.StartTerminal?.DisconnectWire();
                wire.EndTerminal?.DisconnectWire();
                if (Application.isPlaying) Destroy(wire.gameObject);
                else DestroyImmediate(wire.gameObject);
            }
        }
        activeWires.Clear();
    }

    /// <summary>
    /// Đếm tổng số điểm đâm chéo / đè lên nhau giữa các đường vẽ tự do (hoặc đoạn thẳng) trên bo mạch.
    /// </summary>
    public int CountTotalIntersections()
    {
        int count = 0;
        for (int i = 0; i < activeWires.Count; i++)
        {
            for (int j = i + 1; j < activeWires.Count; j++)
            {
                if (activeWires[i] != null && activeWires[j] != null)
                {
                    count += activeWires[i].CountIntersectionsWith(activeWires[j]);
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Kiểm tra xem tất cả các cọc trên bo mạch đã được nối thông cặp đúng màu hay chưa.
    /// </summary>
    public bool AreAllTerminalsConnected()
    {
        if (allTerminals.Count == 0 || activeWires.Count == 0) return false;

        // Đồng bộ: Đảm bảo các cọc đang được nối bởi dây (và dây phải nối tới đích IsFullyConnected) có trạng thái IsConnected = true
        HashSet<RewiringTerminal> connectedTerminals = new HashSet<RewiringTerminal>();
        foreach (var wire in activeWires)
        {
            if (wire != null && wire.IsFullyConnected)
            {
                wire.StartTerminal.ConnectWire();
                wire.EndTerminal.ConnectWire();
                connectedTerminals.Add(wire.StartTerminal);
                connectedTerminals.Add(wire.EndTerminal);
            }
        }

        foreach (var term in allTerminals)
        {
            if (term != null)
            {
                if (!connectedTerminals.Contains(term) || !term.IsConnected)
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Đánh giá chất lượng sửa chữa (RepairQuality):
    /// - Nếu chưa nối thông hết tất cả các cọc -> Broken (Hỏng).
    /// - Nếu nối hết (không chéo dây) -> Perfect (Hoàn hảo - Nối có tâm).
    /// - Nếu nối hết nhưng có >= 1 điểm chéo nhau (trường hợp vẽ tự do đặc biệt) -> Passable (Nối ẩu).
    /// </summary>
    public RepairQuality EvaluateRewiringQuality()
    {
        if (!AreAllTerminalsConnected() || activeWires.Count == 0)
        {
            return RepairQuality.Broken;
        }

        int intersectionCount = CountTotalIntersections();

        if (intersectionCount == 0)
        {
            return RepairQuality.Perfect;
        }
        else
        {
            return RepairQuality.Passable;
        }
    }

    /// <summary>
    /// Hoàn tất minigame: Gọi hàm đánh giá chất lượng và kích hoạt sự kiện kết thúc.
    /// </summary>
    public RepairQuality EndMinigame()
    {
        if (!_isActive) return RepairQuality.Broken;

        RepairQuality quality = EvaluateRewiringQuality();
        _isActive = false;
        gameObject.SetActive(false);

        OnMinigameCompleted?.Invoke(quality);
        return quality;
    }
}
