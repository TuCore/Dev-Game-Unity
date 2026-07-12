using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionDoor : MonoBehaviour, IInteractable
{
    [Header("Cấu hình Chuyển Cảnh")]
    [Tooltip("Tên của Scene muốn chuyển tới (ví dụ: VietnamStreet, Shop_Main)")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Dòng chữ hiện lên khi chỉ chuột vào cửa")]
    [SerializeField] private string interactionPrompt = "Mở cửa ra phố";

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log("[SceneTransitionDoor] Đang chuyển tới scene: " + targetSceneName);
            LoadingScreenManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("[SceneTransitionDoor] Chưa thiết lập tên Scene đích trong Inspector!");
        }
    }
}
