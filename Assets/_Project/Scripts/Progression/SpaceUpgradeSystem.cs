using UnityEngine;

/// <summary>
/// Hệ thống nâng cấp không gian sống/tiệm.
/// Sơn tường, máy lạnh, loa xịn → Stamina hồi nhanh hơn / buff tinh thần.
/// Tiệm lớn → chuỗi cửa hàng (giai đoạn endgame).
/// </summary>
public class SpaceUpgradeSystem : MonoBehaviour
{
    [Header("Cấp độ không gian")]
    [SerializeField] private int shopLevel = 1;           // Cấp tiệm (1: phòng trọ, 2: tiệm nhỏ, 3: tiệm lớn)
    [SerializeField] private int comfortLevel = 0;        // Tiện nghi (máy lạnh, loa, đèn...)

    public int ShopLevel => shopLevel;
    public int ComfortLevel => comfortLevel;

    /// <summary>Bonus hồi phục Stamina từ tiện nghi (% tăng thêm).</summary>
    public float StaminaRecoveryBonus => comfortLevel * 0.1f; // +10% mỗi comfort level

    // Events
    public System.Action<int> OnShopUpgraded;             // Truyền level mới
    public System.Action<int> OnComfortUpgraded;           // Truyền comfort level mới

    /// <summary>
    /// Nâng cấp tiệm (phòng trọ → tiệm nhỏ → tiệm lớn).
    /// </summary>
    public bool UpgradeShop(EconomyManager economy, float cost)
    {
        if (!economy.CanAfford(cost)) return false;
        economy.SpendCash(cost);
        shopLevel++;
        OnShopUpgraded?.Invoke(shopLevel);
        return true;
    }

    /// <summary>
    /// Thêm tiện nghi (sơn tường, máy lạnh, loa xịn...).
    /// </summary>
    public bool AddComfort(EconomyManager economy, float cost)
    {
        if (!economy.CanAfford(cost)) return false;
        economy.SpendCash(cost);
        comfortLevel++;
        OnComfortUpgraded?.Invoke(comfortLevel);
        return true;
    }

    /// <summary>
    /// Kiểm tra tiệm đã đủ lớn để mở chi nhánh chưa (yêu cầu giai đoạn 3).
    /// </summary>
    public bool CanExpandBranch() => shopLevel >= 3;
}
