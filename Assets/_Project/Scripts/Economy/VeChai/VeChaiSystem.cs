using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Ve chai — sinh đơn hàng ve chai theo khung giờ ngẫu nhiên.
/// Tái sử dụng MinigameManager + FaultRandomizer, chỉ khác nguồn gốc đơn và không deadline khách (GDD mục 2.5).
/// Cho người chơi việc để làm khi không có khách, biến thời gian rảnh thành cơ hội thu nhập chủ động.
/// </summary>
public class VeChaiSystem : MonoBehaviour
{
    [Header("Cấu hình Ve chai")]
    [SerializeField] private float minAppearInterval = 60f;   // Giây tối thiểu giữa 2 lần rao
    [SerializeField] private float maxAppearInterval = 180f;   // Giây tối đa giữa 2 lần rao
    [SerializeField] private float buyPriceMultiplier = 0.3f;  // Giá mua = 30% giá bán lại
    [SerializeField] private float sellPriceMultiplier = 0.8f; // Giá bán = 80% giá gốc

    [Header("Kho đồ ve chai")]
    [SerializeField] private List<string> veChaiItemPool;      // Danh sách đồ có thể mua từ ve chai

    private float _nextAppearTimer;
    private bool _veChaiAvailable;
    private List<string> _currentOfferings = new List<string>();

    public bool IsVeChaiAvailable => _veChaiAvailable;
    public List<string> CurrentOfferings => new List<string>(_currentOfferings);

    // Events
    public System.Action OnVeChaiArrived;               // Tiếng rao ve chai vang lên
    public System.Action OnVeChaiLeft;                   // Ve chai đi khỏi
    public System.Action<string> OnItemPurchased;        // Mua đồ từ ve chai
    public System.Action<string, float> OnItemSold;      // Bán đồ đã sửa (item, giá)

    private void Start()
    {
        ScheduleNextAppearance();
    }

    private void Update()
    {
        if (!_veChaiAvailable)
        {
            _nextAppearTimer -= Time.deltaTime;
            if (_nextAppearTimer <= 0f)
            {
                AppearVeChai();
            }
        }
    }

    private void ScheduleNextAppearance()
    {
        _nextAppearTimer = Random.Range(minAppearInterval, maxAppearInterval);
    }

    private void AppearVeChai()
    {
        _veChaiAvailable = true;

        // Random chọn 1-3 đồ từ pool
        _currentOfferings.Clear();
        int count = Random.Range(1, Mathf.Min(4, veChaiItemPool.Count + 1));
        List<string> shuffled = new List<string>(veChaiItemPool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        _currentOfferings = shuffled.GetRange(0, count);

        OnVeChaiArrived?.Invoke();
    }

    /// <summary>
    /// Mua đồ hỏng từ ve chai (giá rẻ).
    /// </summary>
    public bool PurchaseItem(string itemName, EconomyManager economy, float basePrice)
    {
        float buyPrice = basePrice * buyPriceMultiplier;
        if (!economy.CanAfford(buyPrice)) return false;

        economy.SpendCash(buyPrice);
        _currentOfferings.Remove(itemName);
        OnItemPurchased?.Invoke(itemName);
        return true;
    }

    /// <summary>
    /// Bán đồ đã sửa xong (sau khi qua minigame).
    /// </summary>
    public void SellRepairedItem(string itemName, float basePrice, RepairQuality quality, EconomyManager economy)
    {
        float qualityMultiplier = quality switch
        {
            RepairQuality.Broken   => 0f,
            RepairQuality.Passable => 0.5f,
            RepairQuality.Good     => 1f,
            RepairQuality.Perfect  => 1.2f,
            _ => 0f
        };

        float sellPrice = basePrice * sellPriceMultiplier * qualityMultiplier;
        economy.AddCash(sellPrice);
        OnItemSold?.Invoke(itemName, sellPrice);
    }

    /// <summary>
    /// Ve chai đi khỏi (hết giờ hoặc người chơi bỏ qua).
    /// </summary>
    public void DismissVeChai()
    {
        _veChaiAvailable = false;
        _currentOfferings.Clear();
        OnVeChaiLeft?.Invoke();
        ScheduleNextAppearance();
    }
}
