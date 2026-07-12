using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class ToastNotificationManager : MonoBehaviour
{
    private static ToastNotificationManager _instance;
    public static ToastNotificationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ToastNotificationManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ToastNotificationManager");
                    _instance = go.AddComponent<ToastNotificationManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private GameObject _toastPanel;
    private TextMeshProUGUI _toastText;
    private RectTransform _toastRect;
    private Coroutine _hideCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        CreateToastUI();
    }

    private void CreateToastUI()
    {
        // Tự động tìm HUD_Canvas
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null) canvas = existingCanvas.GetComponent<Canvas>();
        else canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null) return;

        // Tạo Panel
        _toastPanel = new GameObject("ToastPanel");
        _toastPanel.transform.SetParent(canvas.transform, false);
        _toastRect = _toastPanel.AddComponent<RectTransform>();
        _toastRect.anchorMin = new Vector2(0.5f, 0f); // Neo ở giữa dưới
        _toastRect.anchorMax = new Vector2(0.5f, 0f);
        _toastRect.pivot = new Vector2(0.5f, 0f);
        _toastRect.anchoredPosition = new Vector2(0, -200); // Ẩn xuống dưới ban đầu
        _toastRect.sizeDelta = new Vector2(600, 80);

        Image bg = _toastPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        // Tạo Text
        GameObject textObj = new GameObject("ToastText");
        textObj.transform.SetParent(_toastPanel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(20, 0);
        textRect.offsetMax = new Vector2(-20, 0);

        _toastText = textObj.AddComponent<TextMeshProUGUI>();
        _toastText.fontSize = 32;
        _toastText.alignment = TextAlignmentOptions.Center;
        _toastText.color = Color.white;
        _toastText.fontStyle = FontStyles.Bold;

        _toastPanel.SetActive(false);
    }

    public void ShowToast(string message, float duration = 3f)
    {
        if (_toastPanel == null) return;

        _toastText.text = message;
        _toastPanel.SetActive(true);

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(AnimateToast(duration));
    }

    private IEnumerator AnimateToast(float duration)
    {
        float animSpeed = 10f;
        
        // Trượt lên
        while (_toastRect.anchoredPosition.y < 100)
        {
            _toastRect.anchoredPosition = Vector2.Lerp(_toastRect.anchoredPosition, new Vector2(0, 100), Time.deltaTime * animSpeed);
            yield return null;
        }

        // Chờ
        yield return new WaitForSeconds(duration);

        // Trượt xuống
        while (_toastRect.anchoredPosition.y > -200)
        {
            _toastRect.anchoredPosition = Vector2.Lerp(_toastRect.anchoredPosition, new Vector2(0, -200), Time.deltaTime * animSpeed);
            if (_toastRect.anchoredPosition.y < -150) break;
            yield return null;
        }

        _toastPanel.SetActive(false);
    }
}
