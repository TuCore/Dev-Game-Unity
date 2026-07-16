using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    private GameObject _panel;

    private void Start()
    {
        CreateUI();
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnBankrupt += ShowGameOver;
        }
    }

    private void CreateUI()
    {
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null) canvas = existingCanvas.GetComponent<Canvas>();
        else canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null) return;

        _panel = new GameObject("GameOverPanel");
        _panel.transform.SetParent(canvas.transform, false);
        _panel.SetActive(false); // Hide by default

        RectTransform panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.8f, 0f, 0f, 0.95f); // Đỏ thẫm

        // Text
        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(_panel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(800, 200);
        textRect.anchoredPosition = new Vector2(0, 100);

        TextMeshProUGUI goText = textObj.AddComponent<TextMeshProUGUI>();
        goText.fontSize = 80;
        goText.fontStyle = FontStyles.Bold;
        goText.alignment = TextAlignmentOptions.Center;
        goText.color = Color.white;
        goText.text = "PHÁ SẢN!";

        GameObject subTextObj = new GameObject("SubText");
        subTextObj.transform.SetParent(_panel.transform, false);
        RectTransform subTextRect = subTextObj.AddComponent<RectTransform>();
        subTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        subTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        subTextRect.sizeDelta = new Vector2(800, 100);
        subTextRect.anchoredPosition = new Vector2(0, 0);

        TextMeshProUGUI subText = subTextObj.AddComponent<TextMeshProUGUI>();
        subText.fontSize = 30;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = Color.white;
        subText.text = "Bạn đã không thể trả nợ và bị đuổi ra khỏi nhà trọ.";

        // Button
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(_panel.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(300, 80);
        btnRect.anchoredPosition = new Vector2(0, -150);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.2f);
        
        UnityEngine.UI.Outline btnOutline = btnObj.AddComponent<UnityEngine.UI.Outline>();
        btnOutline.effectColor = Color.white;
        btnOutline.effectDistance = new Vector2(2, -2);

        Button restartBtn = btnObj.AddComponent<Button>();
        restartBtn.onClick.AddListener(OnRestartClicked);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        btnTextRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "CHƠI LẠI TỪ ĐẦU";
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
    }

    private void ShowGameOver()
    {
        if (_panel == null) CreateUI();
        if (_panel == null) return;
        
        // Hide DaySummary if it's open
        DaySummaryUI daySummary = FindFirstObjectByType<DaySummaryUI>();
        if (daySummary != null)
        {
            daySummary.gameObject.SetActive(false); 
        }

        Time.timeScale = 0f;
        _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;

        // Xóa lưu trữ để bắt đầu lại từ ngày 1
        PlayerPrefs.DeleteKey("Money");
        PlayerPrefs.DeleteKey("CurrentDay");
        PlayerPrefs.DeleteKey("TutorialShown");
        PlayerPrefs.SetInt("IsNewGame", 1);
        PlayerPrefs.SetInt("HasSaveGame", 1);
        PlayerPrefs.Save();

        // Xoá các Singleton đang giữ trạng thái cũ
        if (EconomyManager.Instance != null) Destroy(EconomyManager.Instance.gameObject);
        if (DayClock.Instance != null) Destroy(DayClock.Instance.gameObject);
        if (CustomerQueue.Instance != null) Destroy(CustomerQueue.Instance.gameObject);
        if (ToastNotificationManager.Instance != null) Destroy(ToastNotificationManager.Instance.gameObject);

        // Reset cờ kiểm tra lần đầu ra phố
        BedInteraction.hasVisitedStreetFirstTime = false;

        // Về phòng ngủ (Shop_Main)
        LoadingScreenManager.LoadScene("Shop_Main");
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnBankrupt -= ShowGameOver;
        }
    }
}
