using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Enum màu sắc của dây và cọc nối trong Minigame Nối dây.
/// </summary>
public enum WireColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Orange,
    Purple,
    Brown,
    White,
    Black
}

/// <summary>
/// Đầu cọc đồng trên bo mạch điện trong Minigame Nối dây (Rewiring).
/// Quản lý trạng thái đã đấu dây hay chưa và màu sắc tương ứng.
/// </summary>
public class RewiringTerminal : MonoBehaviour
{
    [Header("Cấu hình Cọc điện")]
    [SerializeField] private WireColor wireColor = WireColor.Red;
    [SerializeField] private bool isConnected = false;

    [Header("Sự kiện thay đổi trạng thái")]
    public UnityEvent OnConnectedStateChanged;

    public WireColor Color => wireColor;
    public bool IsConnected => isConnected;
    public Vector2 Position => (Vector2)transform.position;
    public Vector2Int GridCell { get; private set; } = Vector2Int.zero;

    /// <summary>
    /// Khởi tạo màu sắc cho đầu cọc.
    /// </summary>
    public void Initialize(WireColor color)
    {
        wireColor = color;
        isConnected = false;
    }

    /// <summary>
    /// Khởi tạo cọc theo tọa độ ô lưới (Grid Board).
    /// </summary>
    public void InitializeGrid(WireColor color, Vector2Int cell)
    {
        wireColor = color;
        isConnected = false;
        GridCell = cell;
    }

    /// <summary>
    /// Đấu nối dây vào cọc này.
    /// </summary>
    public void ConnectWire()
    {
        if (!isConnected)
        {
            isConnected = true;
            OnConnectedStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Ngắt kết nối dây.
    /// </summary>
    public void DisconnectWire()
    {
        if (isConnected)
        {
            isConnected = false;
            OnConnectedStateChanged?.Invoke();
        }
    }
}
