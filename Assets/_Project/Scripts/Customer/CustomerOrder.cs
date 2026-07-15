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
    public CustomerPersonality personality;
    public int difficultyLevel = 1;   // Độ khó
    public float basePay;             // Tiền công cơ bản
    public float negotiatedPrice;     // Giá đã chốt sau thương lượng
    
    [Header("Lịch Hẹn Lấy")]
    public int appointmentDay;
    public float appointmentHour;

    [Header("Trạng thái")]
    public bool isPickedUp;
    public bool isCompleted;
    public bool isFailed;
    public bool hasSpawnedReturning = false;

    /// <summary>
    /// Đơn hàng đã tới giờ hẹn hay chưa.
    /// </summary>
    public bool IsAppointmentDue(int currentDay, float currentHour)
    {
        if (isPickedUp) return false;
        if (currentDay > appointmentDay) return true;
        if (currentDay == appointmentDay && currentHour >= appointmentHour) return true;
        return false;
    }

    /// <summary>
    /// Tạo đơn hàng mới.
    /// </summary>
    public CustomerOrder(string customerName, string itemName, CustomerPersonality personality, int difficulty, float pay, int apptDay, float apptHour)
    {
        this.customerName = customerName;
        this.itemName = itemName;
        this.personality = personality;
        this.difficultyLevel = difficulty;
        this.basePay = pay;
        this.negotiatedPrice = pay;
        this.appointmentDay = apptDay;
        this.appointmentHour = apptHour;
        this.isCompleted = false;
        this.isPickedUp = false;
        this.isFailed = false;
        this.hasSpawnedReturning = false;
    }

    public void MarkCompleted()
    {
        if (!isPickedUp && !isFailed)
        {
            isCompleted = true;
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
            case RepairQuality.Passable: return negotiatedPrice * 0.7f;
            case RepairQuality.Good:     return negotiatedPrice;
            case RepairQuality.Perfect:  return negotiatedPrice * 1.3f; // + tip
            default: return 0f;
        }
    }
}
