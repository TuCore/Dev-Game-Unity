using UnityEngine;

public class DeliveryBox : MonoBehaviour, IInteractable
{
    public string itemName;

    public string GetInteractionPrompt()
    {
        return $"Nhấn [E] để mở hộp giao hàng ({itemName})";
    }

    public void Interact()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Tiếng mở hộp");
        }

        InventoryManager.Instance.AddItem(itemName, 1);
        
        Debug.Log($"[Shopee] Đã nhận được {itemName} từ kiện hàng!");
        if (ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast($"Đã cất {itemName} vào kho linh kiện!", 3f);
        }
        
        Destroy(gameObject);
    }
}
