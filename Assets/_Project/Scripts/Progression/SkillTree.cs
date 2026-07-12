using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý việc mở khóa các loại đồ điện dựa trên điểm danh tiếng.
/// Gọi GetUnlockedItemTypes() để lấy danh sách các món đồ khách có thể mang tới.
/// </summary>
public class SkillTree : MonoBehaviour
{
    private static SkillTree _instance;
    public static SkillTree Instance
    {
        get
        {
            if (_instance == null) _instance = FindFirstObjectByType<SkillTree>();
            return _instance;
        }
    }

    [System.Serializable]
    public class RepairTier
    {
        public string tierName;
        public int requiredReputation;
        public List<string> unlockableItems;
    }

    [Header("Cấu hình các bậc đồ nghề")]
    public List<RepairTier> repairTiers = new List<RepairTier>
    {
        new RepairTier { tierName = "Tier 1 - Khởi đầu", requiredReputation = 0, unlockableItems = new List<string> { "Quạt bàn", "Nồi cơm điện" } },
        new RepairTier { tierName = "Tier 2 - Cơ bản", requiredReputation = 100, unlockableItems = new List<string> { "Lò vi sóng", "Tivi" } },
        new RepairTier { tierName = "Tier 3 - Nâng cao", requiredReputation = 350, unlockableItems = new List<string> { "Laptop" } },
        new RepairTier { tierName = "Tier 4 - Chuyên gia", requiredReputation = 750, unlockableItems = new List<string> { "PC cao cấp", "Bàn phím cơ" } }
    };

    private ReputationSystem _reputationSystem;
    private List<string> _currentUnlockedItems = new List<string>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        _reputationSystem = FindFirstObjectByType<ReputationSystem>();
        UpdateUnlockedItems();
        
        if (_reputationSystem != null)
        {
            _reputationSystem.OnReputationMilestone += (milestone) => UpdateUnlockedItems();
        }
    }

    /// <summary>
    /// Tính toán lại danh sách đồ vật đã được mở khóa dựa trên danh tiếng hiện tại.
    /// </summary>
    private void UpdateUnlockedItems()
    {
        _currentUnlockedItems.Clear();
        int currentRep = _reputationSystem != null ? _reputationSystem.CurrentReputation : 0;

        foreach (var tier in repairTiers)
        {
            if (currentRep >= tier.requiredReputation)
            {
                _currentUnlockedItems.AddRange(tier.unlockableItems);
            }
        }
    }

    /// <summary>
    /// Trả về danh sách tên các món đồ mà thợ có thể sửa ở hiện tại.
    /// </summary>
    public List<string> GetUnlockedItemTypes()
    {
        if (_currentUnlockedItems.Count == 0)
        {
            UpdateUnlockedItems();
        }
        return new List<string>(_currentUnlockedItems);
    }
}
