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

    [Header("Economy")]
    [Tooltip("Tiền công cơ bản khi sửa xong")]
    [SerializeField] private float baseReward = 50000f;

    public void StartRepair()
    {
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

    private void OnRepairDone(RepairQuality quality)
    {
        // Gỡ event ngay để tránh bị gọi lặp lại vào lần sau
        MinigameManager manager = FindObjectOfType<MinigameManager>();
        if (manager != null) manager.OnMinigameCompleted -= OnRepairDone;

        // Tính tiền thưởng dựa trên chất lượng sửa (Pass hết = Perfect)
        float reward = 0f;
        switch (quality)
        {
            case RepairQuality.Perfect: 
                reward = baseReward * 1.5f; // Thưởng thêm 50% nếu hoàn hảo
                break;
            case RepairQuality.Good: 
                reward = baseReward; 
                break;
            case RepairQuality.Passable: 
                reward = baseReward * 0.5f; // Bị trừ nửa tiền nếu làm ẩu
                break;
            case RepairQuality.Broken: 
                reward = 0f; // Sửa hỏng thì không có tiền
                break;
        }

        if (reward > 0)
        {
            // Tìm hoặc tự tạo EconomyManager nếu chưa có
            EconomyManager economy = FindObjectOfType<EconomyManager>();
            if (economy == null)
            {
                GameObject ecoObj = new GameObject("EconomyManager");
                economy = ecoObj.AddComponent<EconomyManager>();
            }

            // Tìm hoặc tự tạo MoneyUI để hiển thị tiền lên màn hình
            MoneyUI moneyUI = FindObjectOfType<MoneyUI>();
            if (moneyUI == null)
            {
                economy.gameObject.AddComponent<MoneyUI>();
            }

            // Cộng tiền vào tài khoản
            economy.AddCash(reward);
            Debug.Log($"[Tiền công] Sửa thành công ({quality})! Nhận được: {reward} VNĐ");
        }
        else
        {
            Debug.Log("[Tiền công] Bạn đã làm hỏng món đồ, không nhận được đồng nào!");
        }
    }
}
