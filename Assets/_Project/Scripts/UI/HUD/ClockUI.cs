using UnityEngine;
using TMPro;

public class ClockUI : MonoBehaviour
{
    private TextMeshProUGUI _timeText;

    private void Start()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VietnamStreet")
        {
            CreateClockUI();
        }
        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged += UpdateTimeDisplay;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "VietnamStreet")
        {
            if (_timeText == null) CreateClockUI();
            if (_timeText != null) _timeText.gameObject.SetActive(true);
        }
        else
        {
            if (_timeText != null) _timeText.gameObject.SetActive(false);
        }
    }

    private void CreateClockUI()
    {
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null) canvas = existingCanvas.GetComponent<Canvas>();
        else canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null) return;

        GameObject textObj = new GameObject("ClockText");
        textObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); // Góc trên trái
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -30); // Cách lề trái 30px
        rect.sizeDelta = new Vector2(400, 60);

        _timeText = textObj.AddComponent<TextMeshProUGUI>();
        _timeText.fontSize = 44;
        _timeText.alignment = TextAlignmentOptions.Left;
        _timeText.fontStyle = FontStyles.Bold;
        _timeText.color = Color.white;
        
        UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        
        if (DayClock.Instance != null)
        {
            UpdateTimeDisplay(DayClock.Instance.CurrentHour);
        }
    }

    private void UpdateTimeDisplay(float currentHour)
    {
        if (_timeText == null) return;
        
        int day = DayClock.Instance != null ? DayClock.Instance.CurrentDay : 1;
        
        int hours = Mathf.FloorToInt(currentHour);
        int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);
        
        string amPm = hours >= 12 ? "PM" : "AM";
        int displayHours = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);
        
        _timeText.text = $"Ngày {day} - {displayHours:00}:{minutes:00} {amPm}";
        
        // Cảnh báo nếu sắp hết giờ (sau 19:00)
        if (hours >= 19)
        {
            _timeText.color = Color.red;
        }
        else
        {
            _timeText.color = Color.white;
        }
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged -= UpdateTimeDisplay;
        }
    }
}
