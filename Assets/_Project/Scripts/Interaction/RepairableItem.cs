using UnityEngine;
using System.Collections.Generic;
using Minigames.Diagnosis;

public enum MinigameType { Soldering, Diagnosis }

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
        if (linkedOrder != null)
        {
            linkedOrder.basePay = reward; // Override base pay with the calculated reward based on quality
            linkedOrder.isCompleted = true;
            
            if (CustomerQueue.Instance != null)
            {
                CustomerQueue.Instance.CompleteOrder(linkedOrder);
            }
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
            }
        }
    }
}

// Trigger recompile
