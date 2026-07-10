using System.Collections.Generic;
using UnityEngine;

public class SolderWorkbench : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MinigameManager minigameManager;
    [SerializeField] private SolderingMinigame solderingMinigame;

    [Header("Cấu hình test Minigame")]
    [SerializeField] private int difficultyLevel = 1;
    public string GetInteractionPrompt()
    {
        return "Nhấn [E] để bắt đầu Hàn mạch";
    }
    public void Interact()
    {
        if (minigameManager == null || solderingMinigame == null)
        {
            Debug.LogError("[SolderWorkbench] Thiếu reference tới MinigameManager hoặc SolderingMinigame!");
            return;
        }
        if (minigameManager.IsMinigameActive) return;
        // Giả lập danh sách lỗi để test
        List<string> mockFaults = new List<string> { "Đứt dây nguồn", "Cháy tụ điện C1", "Hỏng mối hàn chân IC" };
        Debug.Log("[SolderWorkbench] Bắt đầu minigame hàn...");
        minigameManager.StartMinigame(solderingMinigame, mockFaults, difficultyLevel);
    }

    private void Update()
    {
        // Kiểm tra nếu người chơi nhấn phím số 2 (ở hàng phím số phía trên chữ W)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("[SolderWorkbench] Nhấn phím 2 để kích hoạt nhanh Minigame!");
            Interact();
        }
    }

}
