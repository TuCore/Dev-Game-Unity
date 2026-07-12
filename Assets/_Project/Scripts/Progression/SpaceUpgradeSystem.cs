using UnityEngine;

/// <summary>
/// Hệ thống nâng cấp không gian sống/tiệm.
/// Sơn tường, máy lạnh, loa xịn → Stamina hồi nhanh hơn / buff tinh thần.
/// Tiệm lớn → chuỗi cửa hàng (giai đoạn endgame).
/// </summary>
public class SpaceUpgradeSystem : MonoBehaviour
{
    private static SpaceUpgradeSystem _instance;
    public static SpaceUpgradeSystem Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<SpaceUpgradeSystem>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    [Header("Cấp độ không gian")]
    [SerializeField] private int shopLevel = 1;           // Cấp tiệm (1: phòng trọ, 2: tiệm nhỏ, 3: tiệm lớn)
    [SerializeField] private int comfortLevel = 0;        // Tiện nghi (máy lạnh, loa, đèn...)

    [Header("Các nâng cấp cụ thể")]
    public bool hasAirConditioner = false;
    public bool hasSpeaker = false;

    [Header("GameObject 3D (Kéo thả từ Scene vào)")]
    public GameObject airConditionerModel;
    public GameObject speakerModel;

    public int ShopLevel => shopLevel;
    public int ComfortLevel => comfortLevel;

    /// <summary>Bonus hồi phục Stamina từ tiện nghi (% tăng thêm).</summary>
    public float StaminaRecoveryBonus => comfortLevel * 0.1f; // +10% mỗi comfort level

    // Events
    public System.Action<int> OnShopUpgraded;             // Truyền level mới
    public System.Action<int> OnComfortUpgraded;           // Truyền comfort level mới

    private void Start()
    {
        // Ẩn/hiện model theo trạng thái đã lưu
        if (airConditionerModel != null) airConditionerModel.SetActive(hasAirConditioner);
        if (speakerModel != null) speakerModel.SetActive(hasSpeaker);
    }

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
    /// Thêm tiện nghi chung chung (Sơn tường, dọn dẹp...).
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
    /// Mua Máy Lạnh
    /// </summary>
    public bool BuyAirConditioner(EconomyManager economy, float cost)
    {
        if (hasAirConditioner || !economy.CanAfford(cost)) return false;
        
        economy.SpendCash(cost);
        hasAirConditioner = true;
        comfortLevel += 2; // Tăng độ thoải mái nhiều hơn
        
        if (airConditionerModel != null) airConditionerModel.SetActive(true);
        OnComfortUpgraded?.Invoke(comfortLevel);
        return true;
    }

    /// <summary>
    /// Mua Loa xịn
    /// </summary>
    public bool BuySpeaker(EconomyManager economy, float cost)
    {
        if (hasSpeaker || !economy.CanAfford(cost)) return false;
        
        economy.SpendCash(cost);
        hasSpeaker = true;
        comfortLevel += 1;
        
        if (speakerModel != null) speakerModel.SetActive(true);
        OnComfortUpgraded?.Invoke(comfortLevel);
        return true;
    }

    /// <summary>
    /// Kiểm tra tiệm đã đủ lớn để mở chi nhánh chưa (yêu cầu giai đoạn 3).
    /// </summary>
    public bool CanExpandBranch() => shopLevel >= 3;
}
