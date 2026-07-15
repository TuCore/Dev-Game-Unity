using UnityEngine;
using System.Collections.Generic;
using Minigames.Diagnosis;

public enum MinigameType { Soldering, Diagnosis, Rewiring }

public class RepairableItem : MonoBehaviour
{
    [Header("Minigame Settings")]
    [Tooltip("Chọn loại minigame để chơi khi sửa món đồ này")]
    [SerializeField] private MinigameType minigameToPlay = MinigameType.Soldering;
    [Tooltip("Danh sách lỗi có thể xuất hiện trên đồ vật này")]
    [SerializeField] private List<string> faultPool = new List<string> { "Đứt dây", "Cháy tụ", "Hỏng IC" };
    
    [Tooltip("Độ khó của minigame (1 = Dễ, 3 = Khó)")]
    [Range(1, 5)]
    [SerializeField] private int difficultyLevel = 1;

    [Header("Requirements")]
    [Tooltip("Danh sách linh kiện cần có (Ghi đúng tên, VD: Tụ điện, Dây đồng)")]
    public List<string> requiredParts = new List<string>();

    [Header("Random Requirements")]
    [SerializeField] private bool randomizeRequiredPartsOnOrder = true;
    [SerializeField, Min(1)] private int minRandomRequiredParts = 1;
    [SerializeField, Min(1)] private int maxRandomRequiredParts = 1;
    [SerializeField] private List<string> randomRequiredPartPool = new List<string>
    {
        "T\u1ee5 \u0111i\u1ec7n",
        "D\u00e2y \u0111\u1ed3ng",
        "B\u0103ng keo \u0111en"
    };

    [Header("Economy")]
    [Tooltip("Tiền công cơ bản khi sửa xong")]
    [SerializeField] private float baseReward = 50000f;

    [Header("Repair Limits")]
    [Tooltip("Số lần tối đa có thể sửa món đồ này. Đặt lớn hơn 1 nếu muốn cho phép sửa nhiều lần.")]
    [SerializeField] private int maxRepairs = 1;
    
    private int _currentRepairs = 0;
    
    // Đơn hàng liên kết với vật phẩm này
    public CustomerOrder linkedOrder;

    public bool CanBeRepaired()
    {
        return _currentRepairs < maxRepairs;
    }

    public void SetRandomizedProperties(MinigameType minigame, int diff, float reward, bool randomizeParts = true)
    {
        minigameToPlay = minigame;
        difficultyLevel = Mathf.Clamp(diff, 1, 5);
        baseReward = reward;

        if (randomizeParts)
        {
            RandomizeRequiredParts();
        }
    }

    public void StartRepair()
    {
        if (!CanBeRepaired())
        {
            Debug.Log("Món đồ này đã được sửa xong, không thể sửa thêm!");
            if (ToastNotificationManager.Instance != null)
                ToastNotificationManager.Instance.ShowToast("Món đồ này đã được sửa xong!", 2f);
            return;
        }

        if (!HasRequiredParts())
        {
            Debug.Log("Không đủ linh kiện để sửa chữa!");
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.ShowSubtitle("Anh Thợ Điện (Thiếu đồ)", $"Chưa thể sửa được! {GetMissingPartsText()}", 3.5f);
            }
            return;
        }

        MinigameManager manager = FindObjectOfType<MinigameManager>();
        
        if (manager != null)
        {
            if (manager.IsMinigameActive)
            {
                Debug.Log("Minigame đang chạy rồi, không thể mở thêm!");
                return;
            }

            IMinigame targetMinigame = null;
            if (minigameToPlay == MinigameType.Soldering)
            {
                targetMinigame = FindObjectOfType<SolderingMinigame>(true);
            }
            else if (minigameToPlay == MinigameType.Diagnosis)
            {
                targetMinigame = FindObjectOfType<DiagnosisMinigame>(true);
            }
            else if (minigameToPlay == MinigameType.Rewiring)
            {
                targetMinigame = FindObjectOfType<RewiringController>(true);
                if (targetMinigame == null)
                {
                    GameObject rewiringRoot = new GameObject("RewiringMinigame_Root");
                    RewiringDemoUI demoUI = rewiringRoot.AddComponent<RewiringDemoUI>();
                    targetMinigame = rewiringRoot.GetComponentInChildren<RewiringController>(true);
                    if (targetMinigame != null && targetMinigame is MonoBehaviour mb)
                    {
                        mb.gameObject.SetActive(false);
                    }
                    demoUI.ShowUI(false);
                }
            }
            
            if (targetMinigame != null)
            {
                // Đăng ký sự kiện hoàn thành minigame
                manager.OnMinigameCompleted += OnRepairDone;
                manager.StartMinigame(targetMinigame, faultPool, difficultyLevel);
            }
            else
            {
                Debug.LogError($"Không tìm thấy {minigameToPlay} trong Scene! Hãy kéo Prefab vào Scene.");
            }
        }
        else
        {
            Debug.LogError("Không tìm thấy MinigameManager trong Scene! Hãy kéo Prefab vào Scene.");
        }
    }

    public bool HasRequiredParts()
    {
        if (requiredParts == null || requiredParts.Count == 0) return true;
        
        Dictionary<string, int> requirements = new Dictionary<string, int>();
        foreach(var part in requiredParts) 
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            
            if (requirements.ContainsKey(part)) requirements[part]++;
            else requirements[part] = 1;
        }
        
        if (InventoryManager.Instance == null) return false;

        foreach(var kvp in requirements) 
        {
            if (!InventoryManager.Instance.HasItem(kvp.Key, kvp.Value)) return false;
        }
        return true;
    }

    public string GetMissingPartsText()
    {
        if (requiredParts == null || requiredParts.Count == 0) return "";
        
        Dictionary<string, int> requirements = new Dictionary<string, int>();
        foreach(var part in requiredParts) 
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            if (requirements.ContainsKey(part)) requirements[part]++;
            else requirements[part] = 1;
        }

        List<string> missing = new List<string>();
        foreach(var kvp in requirements) 
        {
            if (InventoryManager.Instance == null || !InventoryManager.Instance.HasItem(kvp.Key, kvp.Value))
            {
                missing.Add($"{kvp.Value}x {kvp.Key}");
            }
        }
        
        if (missing.Count > 0) return "Thiếu: " + string.Join(", ", missing);
        return "";
    }

    public string GetRequiredPartsText()
    {
        if (requiredParts == null || requiredParts.Count == 0) return "Kh\u00f4ng c\u1ea7n linh ki\u1ec7n";

        Dictionary<string, int> requirements = new Dictionary<string, int>();
        foreach(var part in requiredParts) 
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            if (requirements.ContainsKey(part)) requirements[part]++;
            else requirements[part] = 1;
        }

        List<string> parts = new List<string>();
        foreach(var kvp in requirements) 
        {
            parts.Add($"{kvp.Value}x {kvp.Key}");
        }

        return string.Join(", ", parts);
    }

    private void RandomizeRequiredParts()
    {
        if (!randomizeRequiredPartsOnOrder)
        {
            return;
        }

        List<string> pool = new List<string>();
        if (randomRequiredPartPool != null)
        {
            for (int i = 0; i < randomRequiredPartPool.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(randomRequiredPartPool[i]))
                {
                    pool.Add(randomRequiredPartPool[i]);
                }
            }
        }

        requiredParts.Clear();
        if (pool.Count == 0)
        {
            return;
        }

        int safeMin = Mathf.Clamp(minRandomRequiredParts, 1, pool.Count);
        int safeMax = Mathf.Clamp(maxRandomRequiredParts, safeMin, pool.Count);
        int difficultyMax = Mathf.Clamp(difficultyLevel, safeMin, safeMax);
        int count = Random.Range(safeMin, difficultyMax + 1);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            requiredParts.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    private void ConsumeRequiredParts()
    {
        if (requiredParts == null || requiredParts.Count == 0) return;
        
        Dictionary<string, int> requirements = new Dictionary<string, int>();
        foreach(var part in requiredParts) 
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            if (requirements.ContainsKey(part)) requirements[part]++;
            else requirements[part] = 1;
        }

        foreach(var kvp in requirements) 
        {
            InventoryManager.Instance.ConsumeItem(kvp.Key, kvp.Value);
        }
    }

    private void OnRepairDone(RepairQuality quality)
    {
        _currentRepairs++;

        MinigameManager manager = FindObjectOfType<MinigameManager>();
        if (manager != null) manager.OnMinigameCompleted -= OnRepairDone;

        ConsumeRequiredParts();

        float reward = 0f;
        string ratingText = "";
        switch (quality)
        {
            case RepairQuality.Perfect: 
                reward = baseReward * 1.5f; 
                ratingText = "Tuyệt vời! [S+]";
                break;
            case RepairQuality.Good: 
                reward = baseReward; 
                ratingText = "Khá tốt! [A]";
                break;
            case RepairQuality.Passable: 
                reward = baseReward * 0.5f; 
                ratingText = "Tạm được! [C]";
                break;
            case RepairQuality.Broken: 
                reward = 0f; 
                ratingText = "Làm hỏng đồ rồi! [F]";
                break;
        }

        // Cập nhật CustomerOrder nếu có liên kết
        TaskManager.EnsureInstance().NotifyRepairCompleted(quality);

        if (linkedOrder != null)
        {
            linkedOrder.basePay = reward; // Override base pay with the calculated reward based on quality
            linkedOrder.isCompleted = true;
            
            // NOTE: Do not call CustomerQueue.Instance.CompleteOrder(linkedOrder) here.
            // It will be called in CustomerController.ProcessPickup() when the customer actually returns.
        }
        else
        {
            // Trả thưởng trực tiếp (Fall-back nếu không có Order)
            if (ToastNotificationManager.Instance != null)
            {
                if (reward > 0)
                    ToastNotificationManager.Instance.ShowToast($"Đánh giá: {ratingText}\nNhận {reward:N0} đ!", 4f);
                else
                    ToastNotificationManager.Instance.ShowToast($"Đánh giá: {ratingText}\nKhông nhận được tiền!", 4f);
            }

            if (reward > 0)
            {
                EconomyManager economy = FindObjectOfType<EconomyManager>();
                if (economy != null) economy.AddCash(reward);
                
                Debug.Log($"[Tiền công] Sửa thành công ({quality})! Nhận được: {reward} VNĐ");

                string commentText = quality switch
                {
                    RepairQuality.Perfect => $"Sửa hoàn hảo không tì vết! Khách hài lòng trả công {reward:N0} đ [S+].",
                    RepairQuality.Good => $"Sửa xong rồi! Khách kiểm tra thấy hoạt động tốt và trả đủ tiền công {reward:N0} đ [A].",
                    _ => $"Món đồ chạy tạm được, nhưng mối hàn hơi ẩu. Khách bớt tiền công còn {reward:N0} đ [C]."
                };

                if (SubtitleManager.Instance != null)
                {
                    SubtitleManager.Instance.ShowSubtitle("Khách Hàng & Thợ Điện", commentText, 4.5f, "Tiếng thanh toán");
                }
                else if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("Tiếng thanh toán");
                }
            }
            else
            {
                Debug.Log("[Tiền công] Bạn đã làm hỏng món đồ, không nhận được đồng nào!");
                string commentText = "Tiếng xẹt điện nổ bùm! Món đồ đã hỏng hẳn, khách hàng giận dữ bỏ đi không trả đồng nào [F].";
                if (SubtitleManager.Instance != null)
                {
                    SubtitleManager.Instance.ShowSubtitle("Khách Hàng & Thợ Điện", commentText, 4.5f, "Tiếng vô điện");
                }
                else if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("Tiếng vô điện");
                }
            }
        }
    }
}

// Trigger recompile
