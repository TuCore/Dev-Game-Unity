/// <summary>
/// Interface chung cho 4 loại minigame (Diagnosis, Soldering, Rewiring, Cleaning).
/// CẢ NHÓM phải thống nhất interface này từ Day 1 trước khi code minigame riêng.
/// </summary>
public interface IMinigame
{
    /// <summary>Tên minigame (VD: "Hàn mạch", "Dò mạch").</summary>
    string MinigameName { get; }

    /// <summary>Minigame đang chạy hay không.</summary>
    bool IsActive { get; }

    /// <summary>
    /// Khởi tạo minigame với danh sách lỗi đã được random hóa.
    /// </summary>
    /// <param name="faults">Danh sách lỗi cần xử lý trong lượt này.</param>
    /// <param name="difficultyLevel">Cấp độ khó (ảnh hưởng vùng xanh, tốc độ...).</param>
    void Initialize(System.Collections.Generic.List<string> faults, int difficultyLevel);

    /// <summary>Bắt đầu minigame.</summary>
    void StartMinigame();

    /// <summary>
    /// Kết thúc minigame và trả về chất lượng sửa chữa.
    /// </summary>
    RepairQuality EndMinigame();

    /// <summary>Hủy minigame giữa chừng (VD: hết giờ, cúp điện).</summary>
    void AbortMinigame();

    /// <summary>Event khi minigame hoàn thành, truyền kết quả RepairQuality.</summary>
    event System.Action<RepairQuality> OnMinigameCompleted;
}
