using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    private static LoadingScreenManager _instance;
    public static LoadingScreenManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("LoadingScreenManager");
                _instance = go.AddComponent<LoadingScreenManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private GameObject _loadingCanvas;
    private TextMeshProUGUI _progressText;
    private RectTransform _runnerIconRect;
    private RectTransform _barFillRect;
    private TextMeshProUGUI _messageText;
    
    // Tạo giao diện loading
    private void CreateLoadingUI()
    {
        if (_loadingCanvas != null) return;

        // Tạo Canvas
        GameObject canvasObj = new GameObject("LoadingCanvas");
        canvasObj.transform.SetParent(transform); // Gắn vào DontDestroyOnLoad
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Luôn nằm trên cùng
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        _loadingCanvas = canvasObj;

        // Màn hình đen
        GameObject bgObj = new GameObject("BlackBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = Color.black;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Thanh tiến trình (Background)
        GameObject barBgObj = new GameObject("BarBackground");
        barBgObj.transform.SetParent(canvasObj.transform, false);
        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.1f, 0.45f);
        barBgRect.anchorMax = new Vector2(0.9f, 0.5f); // Chiếm 80% chiều ngang
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;

        // Thanh Fill (Tiến độ)
        GameObject barFillObj = new GameObject("BarFill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        Image barFillImg = barFillObj.AddComponent<Image>();
        barFillImg.color = Color.green;
        _barFillRect = barFillObj.GetComponent<RectTransform>();
        _barFillRect.anchorMin = new Vector2(0, 0);
        _barFillRect.anchorMax = new Vector2(0, 1); // Khởi tạo 0%
        _barFillRect.offsetMin = Vector2.zero;
        _barFillRect.offsetMax = Vector2.zero;
        
        // Progress Text
        GameObject textObj = new GameObject("ProgressText");
        textObj.transform.SetParent(canvasObj.transform, false);
        _progressText = textObj.AddComponent<TextMeshProUGUI>();
        _progressText.text = "0%";
        _progressText.fontSize = 50;
        _progressText.alignment = TextAlignmentOptions.Center;
        _progressText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.2f);
        textRect.anchorMax = new Vector2(1, 0.4f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Message Text
        GameObject msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(canvasObj.transform, false);
        _messageText = msgObj.AddComponent<TextMeshProUGUI>();
        _messageText.text = "Đang tải...";
        _messageText.fontSize = 40;
        _messageText.alignment = TextAlignmentOptions.Center;
        _messageText.color = Color.white;
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0, 0.6f);
        msgRect.anchorMax = new Vector2(1, 0.8f);
        msgRect.offsetMin = Vector2.zero;
        msgRect.offsetMax = Vector2.zero;

        // Runner Icon
        GameObject runnerObj = new GameObject("RunnerIcon");
        runnerObj.transform.SetParent(barBgObj.transform, false);
        TextMeshProUGUI runnerText = runnerObj.AddComponent<TextMeshProUGUI>();
        runnerText.text = "🏃"; // Emoji người chạy
        runnerText.fontSize = 60;
        runnerText.alignment = TextAlignmentOptions.Center;
        runnerText.color = Color.white;
        
        _runnerIconRect = runnerObj.GetComponent<RectTransform>();
        _runnerIconRect.anchorMin = new Vector2(0, 1f); // Nằm trên thanh ngang
        _runnerIconRect.anchorMax = new Vector2(0, 1f);
        _runnerIconRect.pivot = new Vector2(0.5f, 0f); // Pivot dưới chân
        _runnerIconRect.sizeDelta = new Vector2(80, 80);
        _runnerIconRect.anchoredPosition = new Vector2(0, 0); 

        _loadingCanvas.SetActive(false);
    }

    public static void LoadScene(string sceneName, string message = "Đang di chuyển...")
    {
        Instance.StartCoroutine(Instance.LoadSceneAsyncRoutine(sceneName, message));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName, string message)
    {
        CreateLoadingUI();
        _loadingCanvas.SetActive(true);

        Time.timeScale = 1f; // Đảm bảo không bị pause
        _progressText.text = "0%";
        if (_messageText != null) _messageText.text = message;
        
        _barFillRect.anchorMax = new Vector2(0f, 1f);
        _runnerIconRect.anchorMin = new Vector2(0f, 1f);
        _runnerIconRect.anchorMax = new Vector2(0f, 1f);

        // Bắt đầu load scene ngầm
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false; 

        float simulatedProgress = 0f;
        float loadTime = 2.0f; // Thời gian tối thiểu giả định 2 giây
        float timer = 0f;

        while (timer < loadTime || op.progress < 0.9f)
        {
            timer += Time.deltaTime;
            
            simulatedProgress = Mathf.Clamp01(timer / loadTime);
            
            // Unity trả về op.progress từ 0 đến 0.9
            float sceneProgress = op.progress / 0.9f;
            float targetProgress = Mathf.Min(simulatedProgress, sceneProgress);

            // Cập nhật UI
            _barFillRect.anchorMax = new Vector2(targetProgress, 1f);
            
            _runnerIconRect.anchorMin = new Vector2(targetProgress, 1f);
            _runnerIconRect.anchorMax = new Vector2(targetProgress, 1f);

            _progressText.text = $"{(targetProgress * 100f):F0}%";

            yield return null;
        }

        // 100%
        _barFillRect.anchorMax = new Vector2(1f, 1f);
        _runnerIconRect.anchorMin = new Vector2(1f, 1f);
        _runnerIconRect.anchorMax = new Vector2(1f, 1f);
        _progressText.text = "100%";
        
        yield return new WaitForSeconds(0.5f); // Nghỉ 0.5s để người chơi thấy 100%

        // Hoàn tất tải cảnh
        op.allowSceneActivation = true;

        yield return new WaitForEndOfFrame();
        _loadingCanvas.SetActive(false);
    }
}
