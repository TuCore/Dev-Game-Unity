using UnityEngine;

/// <summary>
/// Hệ thống tính điểm danh tiếng từ RepairQuality.
/// Mở khóa tệp khách (NPCArchetype) và nhánh Skill Tree.
/// Nên bắn event qua EventBus khi đạt mốc, để UI/Skill Tree tự lắng nghe (GDD mục 13).
/// </summary>
public class ReputationSystem : MonoBehaviour
{
    [Header("Cấu hình Danh tiếng")]
    [SerializeField] private int startingReputation = 0;
    [SerializeField] private int maxReputation = 1000;

    [Header("Điểm thưởng/phạt theo chất lượng sửa")]
    [SerializeField] private int brokenPenalty = -10;
    [SerializeField] private int passableReward = 0;
    [SerializeField] private int goodReward = 5;
    [SerializeField] private int perfectReward = 15;

    private int _currentReputation;

    public int CurrentReputation => _currentReputation;
    public int MaxReputation => maxReputation;

    // Events
    public System.Action<int> OnReputationChanged;        // Truyền giá trị mới
    public System.Action<int> OnReputationMilestone;      // Khi đạt mốc quan trọng

    // Các mốc danh tiếng (có thể đưa vào config/ScriptableObject)
    private readonly int[] _milestones = { 50, 100, 200, 350, 500, 750, 1000 };

    private void Awake()
    {
        _currentReputation = startingReputation;
    }

    /// <summary>
    /// Cập nhật danh tiếng dựa trên chất lượng sửa chữa.
    /// </summary>
    public void ApplyRepairResult(RepairQuality quality)
    {
        int delta = quality switch
        {
            RepairQuality.Broken   => brokenPenalty,
            RepairQuality.Passable => passableReward,
            RepairQuality.Good     => goodReward,
            RepairQuality.Perfect  => perfectReward,
            _ => 0
        };

        ChangeReputation(delta);
    }

    /// <summary>
    /// Thay đổi danh tiếng trực tiếp (VD: sự kiện đặc biệt).
    /// </summary>
    public void ChangeReputation(int amount)
    {
        int oldReputation = _currentReputation;
        _currentReputation = Mathf.Clamp(_currentReputation + amount, 0, maxReputation);

        OnReputationChanged?.Invoke(_currentReputation);

        // Kiểm tra mốc
        foreach (int milestone in _milestones)
        {
            if (oldReputation < milestone && _currentReputation >= milestone)
            {
                OnReputationMilestone?.Invoke(milestone);
            }
        }
    }

    /// <summary>
    /// Kiểm tra đã đạt mốc danh tiếng tối thiểu chưa (cho mở khóa khách/skill).
    /// </summary>
    public bool HasReputation(int required) => _currentReputation >= required;
}
