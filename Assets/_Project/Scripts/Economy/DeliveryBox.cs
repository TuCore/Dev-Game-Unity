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
        InventoryManager.Instance.AddItem(itemName, 1);
        
        Debug.Log($"[Shopee] Đã nhận được {itemName} từ kiện hàng!");
        
        // Sinh ra một chút hạt (particles) nổ hoặc âm thanh nếu có
        
        Destroy(gameObject);
    }
}
