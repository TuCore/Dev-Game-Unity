using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự động chạy phân cảnh Mở đầu (Thức dậy trong phòng ngủ với màn hình đen mờ dần + Vietsub bối cảnh)
/// khi nạp vào scene gameplay (Shop_Main / VietnamStreet).
/// </summary>
public class IntroSequenceStarter : MonoBehaviour
{
    [Tooltip("Nếu true, tự động phát Intro khi scene vừa khởi chạy")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Intro phong ngu chi duoc phep chay trong scene nay")]
    [SerializeField] private string introSceneName = "Shop_Main";

    private static bool _hasPlayedIntroThisSession = false;

    private void Start()
    {
        // Component nay tung ton tai trong ca scene pho, khien audio/phu de Intro
        // bat dau lai khi nguoi choi vao VietnamStreet. Intro chi thuoc phong ngu.
        if (SceneManager.GetActiveScene().name != introSceneName)
        {
            return;
        }

        if (playOnStart && !_hasPlayedIntroThisSession)
        {
            _hasPlayedIntroThisSession = true;
            PlayIntro();
        }
    }

    [ContextMenu("Phát thử Intro Phòng ngủ (Màn hình đen + Vietsub)")]
    public void PlayIntro()
    {
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.PlayIntroSequence(() =>
            {
                if (ToastNotificationManager.Instance != null)
                {
                    ToastNotificationManager.Instance.ShowToast("[!] Hãy nhấp chuột vào bàn sửa hoặc soi đồ để làm việc ngay thôi!", 4f);
                }
            });
        }
    }
}
