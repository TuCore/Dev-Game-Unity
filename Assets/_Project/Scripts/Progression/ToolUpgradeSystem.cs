using UnityEngine;

/// <summary>
/// Hệ thống nâng cấp đồ nghề sửa chữa.
/// Mỏ hàn xịn → vùng xanh minigame Hàn mạch rộng hơn.
/// Kính lúp/đèn LED → minigame Khám bệnh hiển thị rõ hơn, dễ phát hiện lỗi ẩn.
/// </summary>
public class ToolUpgradeSystem : MonoBehaviour
{
    private static ToolUpgradeSystem _instance;
    public static ToolUpgradeSystem Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<ToolUpgradeSystem>();
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

    [Header("Cấp độ đồ nghề hiện tại")]
    [SerializeField] private int solderingIronLevel = 1;  // Mỏ hàn
    [SerializeField] private int multimeterLevel = 1;     // Đồng hồ vạn năng
    [SerializeField] private int magnifierLevel = 1;      // Kính lúp

    public float GetRadarRadiusBonus()
    {
        // Mỗi cấp kính lúp tăng bán kính radar dò lỗi thêm 60 pixel
        return (magnifierLevel - 1) * 60f;
    }

    public int SolderingIronLevel => solderingIronLevel;
    public int MultimeterLevel => multimeterLevel;
    public int MagnifierLevel => magnifierLevel;

    // Events
    public System.Action<string, int> OnToolUpgraded; // (tên tool, level mới)

    /// <summary>
    /// Nâng cấp mỏ hàn — mở rộng vùng xanh trong minigame Soldering.
    /// </summary>
    public bool UpgradeSolderingIron(EconomyManager economy, float cost)
    {
        if (!economy.CanAfford(cost)) return false;
        economy.SpendCash(cost);
        solderingIronLevel++;
        OnToolUpgraded?.Invoke("SolderingIron", solderingIronLevel);
        return true;
    }

    /// <summary>
    /// Nâng cấp đồng hồ vạn năng — dò mạch nhanh hơn trong minigame Diagnosis.
    /// </summary>
    public bool UpgradeMultimeter(EconomyManager economy, float cost)
    {
        if (!economy.CanAfford(cost)) return false;
        economy.SpendCash(cost);
        multimeterLevel++;
        OnToolUpgraded?.Invoke("Multimeter", multimeterLevel);
        return true;
    }

    /// <summary>
    /// Nâng cấp kính lúp/đèn LED — hiển thị rõ hơn, dễ phát hiện lỗi ẩn.
    /// </summary>
    public bool UpgradeMagnifier(EconomyManager economy, float cost)
    {
        if (!economy.CanAfford(cost)) return false;
        economy.SpendCash(cost);
        magnifierLevel++;
        OnToolUpgraded?.Invoke("Magnifier", magnifierLevel);
        return true;
    }

    /// <summary>
    /// Lấy bonus accuracy dựa trên level đồ nghề (dùng cho minigame).
    /// </summary>
    public float GetAccuracyBonus(string toolType)
    {
        return toolType switch
        {
            "SolderingIron" => 0.05f * (solderingIronLevel - 1), // +5% mỗi level
            "Multimeter"    => 0.05f * (multimeterLevel - 1),
            "Magnifier"     => 0.05f * (magnifierLevel - 1),
            _ => 0f
        };
    }
}
