using UnityEngine;

public class BedInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Ngủ (Qua ngày mới)";

    public string GetInteractionPrompt()
    {
        return prompt;
    }

    public static bool hasVisitedStreetFirstTime = false;

    public void Interact()
    {
        if (!hasVisitedStreetFirstTime)
        {
            if (ToastNotificationManager.Instance != null)
            {
                ToastNotificationManager.Instance.ShowToast("Ngày mới vừa bắt đầu, mình nên ra mở cửa tiệm đã!", 3f);
            }
            return;
        }

        if (DayClock.Instance != null)
        {
            if (AudioManager.Instance != null)
            {
                // Thay thế bằng âm thanh ngáy hoặc ngủ nếu có
                AudioManager.Instance.PlaySFX("Tiếng ngáy"); 
            }

            PlayerNeeds.EnsureInstance().RestOvernight();
            
            // Gọi bảng tổng kết ngày
            DayClock.Instance.EndDay();
            Debug.Log("[BedInteraction] Nhân vật đã ngủ. Hiện bảng tổng kết ngày.");
        }
        else
        {
            Debug.LogWarning("[BedInteraction] Không tìm thấy DayClock trong scene!");
        }
    }
}
