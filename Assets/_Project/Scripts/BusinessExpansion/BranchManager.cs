using UnityEngine;

/// <summary>
/// Quản lý mở chi nhánh thứ 2 (endgame).
/// Yêu cầu vốn lớn, mở ra khu vực bản đồ mới (hẻm khác/quận khác),
/// nhân đôi nguồn khách nhưng cũng nhân đôi chi phí vận hành (GDD mục 10).
/// </summary>
public class BranchManager : MonoBehaviour
{
    [Header("Trạng thái chi nhánh")]
    [SerializeField] private bool isBranch02Unlocked = false;

    [Header("Yêu cầu mở chi nhánh")]
    [SerializeField] private float branch02Cost = 5000000f;      // 5 triệu VNĐ
    [SerializeField] private int branch02ReputationRequired = 500;

    [Header("Chi phí vận hành chi nhánh")]
    [SerializeField] private float branch02DailyExpense = 30000f;

    public bool IsBranch02Unlocked => isBranch02Unlocked;
    public float Branch02Cost => branch02Cost;
    public float Branch02DailyExpense => branch02DailyExpense;

    // Events
    public System.Action OnBranchUnlocked;

    /// <summary>
    /// Kiểm tra đủ điều kiện mở chi nhánh chưa.
    /// </summary>
    public bool CanUnlockBranch(EconomyManager economy, ReputationSystem reputation)
    {
        return !isBranch02Unlocked
            && economy.CanAfford(branch02Cost)
            && reputation.HasReputation(branch02ReputationRequired);
    }

    /// <summary>
    /// Mở chi nhánh thứ 2.
    /// </summary>
    public bool UnlockBranch02(EconomyManager economy, ReputationSystem reputation)
    {
        if (!CanUnlockBranch(economy, reputation)) return false;

        economy.SpendCash(branch02Cost);
        isBranch02Unlocked = true;
        OnBranchUnlocked?.Invoke();

        Debug.Log("[BranchManager] Chi nhánh 2 đã mở!");
        return true;
    }

    /// <summary>
    /// Trừ chi phí vận hành chi nhánh cuối ngày.
    /// </summary>
    public void DeductBranchExpenses(EconomyManager economy)
    {
        if (!isBranch02Unlocked) return;
        economy.SpendCash(branch02DailyExpense);
    }
}
