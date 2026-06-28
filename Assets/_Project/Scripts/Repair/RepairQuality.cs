/// <summary>
/// Enum chất lượng sửa chữa — kết quả trả về từ mỗi minigame.
/// Quyết định tiền công, tip, và điểm danh tiếng nhận được.
/// </summary>
public enum RepairQuality
{
    /// <summary>Sửa hỏng — mất danh tiếng, không nhận tiền.</summary>
    Broken,     // Hỏng

    /// <summary>Sửa tạm — nhận tiền cơ bản, không tip, danh tiếng không đổi.</summary>
    Passable,   // Tạm

    /// <summary>Sửa tốt — nhận tiền đầy đủ, có thể có tip nhỏ, cộng danh tiếng.</summary>
    Good,       // Tốt

    /// <summary>Sửa hoàn hảo — tiền cao + tip lớn + danh tiếng tăng mạnh.</summary>
    Perfect     // Hoàn hảo
}
