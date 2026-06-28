using UnityEngine;

/// <summary>
/// Tổng hợp quản lý Cash, chi phí sinh hoạt, giá nâng cấp.
/// Tập trung mọi thay đổi Cash qua đây để dễ balance & log (GDD mục 13).
/// </summary>
public class EconomyManager : MonoBehaviour
{
    [Header("Cấu hình Kinh tế")]
    [SerializeField] private float startingCash = 50000f;   // VNĐ khởi đầu

    [Header("Chi phí sinh hoạt hàng ngày")]
    [SerializeField] private float dailyRent = 15000f;       // Tiền trọ
    [SerializeField] private float dailyElectricity = 5000f;  // Tiền điện
    [SerializeField] private float dailyWater = 3000f;        // Tiền nước

    private float _currentCash;

    public float CurrentCash => _currentCash;
    public float DailyExpenses => dailyRent + dailyElectricity + dailyWater;
    public bool IsBankrupt => _currentCash < 0f;

    // Events
    public System.Action<float> OnCashChanged;        // Truyền số tiền hiện tại
    public System.Action<float> OnCashEarned;         // Truyền số tiền kiếm được
    public System.Action<float> OnCashSpent;          // Truyền số tiền chi ra
    public System.Action OnBankrupt;                   // Hết tiền!

    private void Awake()
    {
        _currentCash = startingCash;
    }

    /// <summary>
    /// Thêm tiền (thu nhập từ sửa chữa, bán đồ ve chai).
    /// </summary>
    public void AddCash(float amount)
    {
        if (amount <= 0) return;
        _currentCash += amount;
        OnCashEarned?.Invoke(amount);
        OnCashChanged?.Invoke(_currentCash);
    }

    /// <summary>
    /// Trừ tiền (mua dụng cụ, chi phí sinh hoạt, nhập đồ ve chai).
    /// </summary>
    public bool SpendCash(float amount)
    {
        if (amount <= 0) return false;
        if (_currentCash < amount) return false; // Không đủ tiền

        _currentCash -= amount;
        OnCashSpent?.Invoke(amount);
        OnCashChanged?.Invoke(_currentCash);

        if (IsBankrupt) OnBankrupt?.Invoke();

        return true;
    }

    /// <summary>
    /// Trừ chi phí sinh hoạt cuối ngày (tiền trọ + điện + nước).
    /// </summary>
    public void DeductDailyExpenses()
    {
        SpendCash(DailyExpenses);
        Debug.Log($"[EconomyManager] Chi phí ngày: {DailyExpenses:N0}đ | Còn lại: {_currentCash:N0}đ");
    }

    /// <summary>
    /// Kiểm tra đủ tiền mua không.
    /// </summary>
    public bool CanAfford(float price) => _currentCash >= price;
}
