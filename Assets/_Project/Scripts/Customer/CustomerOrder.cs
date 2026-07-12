using UnityEngine;

/// <summary>
/// Đại diện cho một đơn hàng từ khách — gồm thông tin đồ cần sửa, deadline, và trạng thái.
/// </summary>
[System.Serializable]
public class CustomerOrder
{
    [Header("Thông tin đơn hàng")]
    public string customerName;
    public string itemName;           // Tên đồ cần sửa (VD: "Quạt bàn", "Nồi cơm điện")
    public int difficultyLevel = 1;   // Độ khó
    public float basePay;             // Tiền công cơ bản
    public float deadline;            // Thời gian tối đa (seconds) trước khi khách bỏ đi

    [Header("Trạng thái")]
    public float timeRemaining;
    public bool isCompleted;

    /// <summary>Đơn hàng đã hết hạn chưa.</summary>
    public bool IsExpired => !isCompleted && timeRemaining <= 0f;

    /// <summary>
    /// Tạo đơn hàng mới.
    /// </summary>
    public CustomerOrder(string customerName, string itemName, int difficulty, float pay, float deadline)
    {
        this.customerName = customerName;
        this.itemName = itemName;
        this.difficultyLevel = difficulty;
        this.basePay = pay;
        this.deadline = deadline;
        this.timeRemaining = deadline;
        this.isCompleted = false;
    }

    /// <summary>
    /// Cập nhật thời gian còn lại.
    /// </summary>
    public void UpdateTimer(float deltaTime)
    {
        if (!isCompleted)
        {
            timeRemaining -= deltaTime;
        }
    }

    /// <summary>
    /// Tính tiền công dựa trên chất lượng sửa chữa.
    /// </summary>
    public float CalculatePay(RepairQuality quality)
    {
        switch (quality)
        {
            case RepairQuality.Broken:   return 0f;
            case RepairQuality.Passable: return basePay * 0.7f;
            case RepairQuality.Good:     return basePay;
            case RepairQuality.Perfect:  return basePay * 1.3f; // + tip
            default: return 0f;
        }
    }
}
