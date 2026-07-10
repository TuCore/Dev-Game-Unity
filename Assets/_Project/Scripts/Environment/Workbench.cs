using UnityEngine;

public class Workbench : MonoBehaviour, IInteractable
{
    public string GetInteractionPrompt()
    {
        return "Bấm E hoặc Click để Sửa Đồ";
    }

    public void Interact()
    {
        Debug.Log("[Workbench] Bạn đã click vào Bàn Làm Việc! Sẵn sàng mở Minigame...");
        // TODO: Chuyển đổi Camera sang góc nhìn sửa đồ, tắt PlayerController
    }
}
