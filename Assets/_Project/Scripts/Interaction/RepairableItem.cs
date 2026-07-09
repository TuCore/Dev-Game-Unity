using UnityEngine;
using System.Collections.Generic;

public class RepairableItem : MonoBehaviour, IInteractable
{
    [Header("Cấu hình sửa chữa")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private List<string> possibleFaults = new List<string> { "Đứt dây", "Cháy tụ", "Hỏng IC", "Lỏng giắc" };
    
    private MinigameManager _minigameManager;
    private IMinigame _solderMinigame;

    private void Start()
    {
        // Tự động tìm hệ thống Minigame trong Scene
        _minigameManager = FindAnyObjectByType<MinigameManager>();
        _solderMinigame = FindAnyObjectByType<SolderingMinigame>();
    }

    public string GetInteractionPrompt()
    {
        return $"Nhấn [F] để Sửa {gameObject.name.Replace("Interactable_", "")}";
    }

    public void Interact()
    {
        // Interface yêu cầu có hàm này, nhưng nút F sẽ do RaycastInteract gọi thẳng vào StartRepair()
    }

    public void StartRepair()
    {
        if (_minigameManager != null && _solderMinigame != null)
        {
            if (!_minigameManager.IsMinigameActive)
            {
                _minigameManager.StartMinigame(_solderMinigame, possibleFaults, difficultyLevel);
                
                // Đăng ký nhận kết quả khi sửa xong
                _minigameManager.OnMinigameCompleted += OnRepairDone;
            }
            else
            {
                Debug.Log("Đang có một minigame khác chạy rồi!");
            }
        }
        else
        {
            Debug.LogWarning("Không tìm thấy MinigameManager hoặc SolderingMinigame trong Scene! Bạn đã kéo UI vào scene chưa?");
        }
    }

    private void OnRepairDone(RepairQuality quality)
    {
        // Gỡ đăng ký để không bị gọi lặp lại vào lần sau
        _minigameManager.OnMinigameCompleted -= OnRepairDone;
        
        Debug.Log($"Đã sửa xong {gameObject.name} với chất lượng: {quality}");
        
        // Gợi ý: Có thể thêm hiệu ứng (Particle System), hoặc đổi material sang mới cứng tại đây
    }
}
