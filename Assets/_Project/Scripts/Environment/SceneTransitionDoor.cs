using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionDoor : MonoBehaviour, IInteractable
{
    private bool _isTransitioning;

    [Header("Cấu hình Chuyển Cảnh")]
    [Tooltip("Tên của Scene muốn chuyển tới (ví dụ: VietnamStreet, Shop_Main)")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Dòng chữ hiện lên khi chỉ chuột vào cửa")]
    [SerializeField] private string interactionPrompt = "Mở cửa ra phố";

    [Tooltip("Dòng chữ hiện lên khi chuyển cảnh")]
    [SerializeField] private string loadingMessage = "Đang đi tới chỗ làm...";

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (_isTransitioning || LoadingScreenManager.IsLoading)
        {
            return;
        }

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogError("[SceneTransitionDoor] Scene không có trong Build Settings: " + targetSceneName);
                return;
            }

            _isTransitioning = true;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Tiếng mở cửa");
            }
            Debug.Log("[SceneTransitionDoor] Đang chuyển tới scene: " + targetSceneName);
            LoadingScreenManager.LoadScene(targetSceneName, loadingMessage);
        }
        else
        {
            Debug.LogWarning("[SceneTransitionDoor] Chưa thiết lập tên Scene đích trong Inspector!");
        }
    }
}
