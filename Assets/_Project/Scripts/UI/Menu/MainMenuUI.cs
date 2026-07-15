using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
namespace AnhThoDien.UI.Menu
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Buttons")]
        public Button btnNewGame;
        public Button btnContinue;
        public Button btnSettings;
        public Button btnCollection;
        public Button btnInfo;
        public Button btnQuit;

        [Header("Panels")]
        public GameObject settingsPanel;

        [Header("Intro Sequence")]
        public GameObject introPanel;
        public Text introText;
        [TextArea(3, 5)]
        public string introMessage = "Giữa lòng Sài Gòn nhộn nhịp, trong một con hẻm nhỏ...\nNơi những món đồ điện cũ kỹ được trao thêm một cơ hội sống.\nBạn là Anh Thợ Điện.\nHành trình của bạn bắt đầu...";
        public float typingSpeed = 0.05f;
        
        private bool isPlayingIntro = false;
        private Coroutine introCoroutine;

        private void Start()
        {
            // Bind buttons
            if (btnNewGame != null) btnNewGame.onClick.AddListener(OnNewGameClicked);
            if (btnContinue != null) btnContinue.onClick.AddListener(OnContinueClicked);
            if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
            if (btnCollection != null) btnCollection.onClick.AddListener(OnCollectionClicked);
            if (btnInfo != null) btnInfo.onClick.AddListener(OnInfoClicked);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);

            // Ensure settings panel is hidden at start
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (introPanel != null) introPanel.SetActive(false);

            // Kiểm tra xem đã có dữ liệu lưu chưa để bật/tắt nút Tiếp tục
            if (btnContinue != null)
            {
                bool hasSave = PlayerPrefs.GetInt("HasSaveGame", 0) == 1;
                btnContinue.interactable = hasSave;
            }

            // Automatically add hover and click sounds to all buttons in this Canvas
            AddSoundToButtons();

            // Play Main Menu background music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic("🎵Nhạc lofi chill chơi game, học bài,... hay nhất, lukrembo royal music🎵  Thiện SUS - Thiện SUS");
            }
        }

        private void Update()
        {
            if (isPlayingIntro && Input.anyKeyDown)
            {
                SkipIntro();
            }
        }

        private void OnNewGameClicked()
        {
            Debug.Log("Starting New Game...");
            
            // Xóa các dữ liệu cũ (reset quá trình chơi)
            PlayerPrefs.DeleteKey("Money");
            PlayerPrefs.DeleteKey("CurrentDay");
            PlayerPrefs.DeleteKey("TutorialShown");
            
            // Báo cho game biết đây là lượt chơi mới
            PlayerPrefs.SetInt("IsNewGame", 1);
            
            // Đánh dấu là đã bắt đầu chơi để lần sau có thể Bấm Tiếp Tục
            PlayerPrefs.SetInt("HasSaveGame", 1);
            PlayerPrefs.Save();
            
            PlayerController.ResetLoadState();

            if (introPanel == null || introText == null)
            {
                CreateIntroUI();
            }

            if (introPanel != null && introText != null)
            {
                introCoroutine = StartCoroutine(PlayIntroRoutine());
            }
            else
            {
                LoadingScreenManager.LoadScene("Shop_Main");
            }
        }

        private void CreateIntroUI()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            introPanel = new GameObject("IntroPanel");
            introPanel.transform.SetParent(canvas.transform, false);
            introPanel.transform.SetAsLastSibling();
            Image bg = introPanel.AddComponent<Image>();
            bg.color = Color.black;
            RectTransform bgRect = introPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            GameObject textObj = new GameObject("IntroText");
            textObj.transform.SetParent(introPanel.transform, false);
            introText = textObj.AddComponent<Text>();
            introText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            introText.fontSize = 32;
            introText.color = Color.white;
            introText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.1f);
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.sizeDelta = Vector2.zero;

            introPanel.SetActive(false);
        }

        private IEnumerator PlayIntroRoutine()
        {
            isPlayingIntro = true;
            introPanel.SetActive(true);
            introText.text = "";

            // Typewriter effect
            foreach (char c in introMessage.ToCharArray())
            {
                introText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            // Wait a few seconds after finishing
            yield return new WaitForSeconds(2.5f);
            
            LoadGameplayScene();
        }

        private void SkipIntro()
        {
            if (introCoroutine != null)
            {
                StopCoroutine(introCoroutine);
            }
            LoadGameplayScene();
        }

        private void LoadGameplayScene()
        {
            isPlayingIntro = false;
            if (introPanel != null)
            {
                Destroy(introPanel);
            }
            LoadingScreenManager.LoadScene("Shop_Main");
        }

        private void OnContinueClicked()
        {
            Debug.Log("Continuing Game...");
            
            // Báo cho game biết đây là lượt chơi tiếp tục
            PlayerPrefs.SetInt("IsNewGame", 0);
            PlayerPrefs.Save();
            
            PlayerController.ResetLoadState();

            // Khi Load Scene, các script trong game sẽ tự động đọc PlayerPrefs (nếu có) để phục hồi
            LoadingScreenManager.LoadScene("VietnamStreet");
        }

        private void OnSettingsClicked()
        {
            Debug.Log("Opening Settings...");
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void OnCollectionClicked()
        {
            Debug.Log("Opening Collection...");
        }

        private void OnInfoClicked()
        {
            Debug.Log("Opening Info...");
        }

        private void OnQuitClicked()
        {
            Debug.Log("Quitting Game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void AddSoundToButtons()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn.gameObject.GetComponent<UIButtonSound>() == null)
                {
                    btn.gameObject.AddComponent<UIButtonSound>();
                }
            }
        }
    }
}
