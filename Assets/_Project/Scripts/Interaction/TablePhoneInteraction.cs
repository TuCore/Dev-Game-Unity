using UnityEngine;

public class TablePhoneInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Sử dụng điện thoại (Mua đồ)";

    public string GetInteractionPrompt()
    {
        return prompt;
    }

    public void Interact()
    {
        if (PhoneManager.Instance != null)
        {
            // Bật điện thoại lên
            PhoneManager.Instance.TogglePhone(true);
            Debug.Log("[TablePhoneInteraction] Mở điện thoại để mua đồ.");
        }
        else
        {
            Debug.LogWarning("[TablePhoneInteraction] Không tìm thấy PhoneManager trong scene!");
        }
    }
}
