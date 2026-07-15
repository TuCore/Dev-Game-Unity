using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    private TextMeshProUGUI _moneyText;
    private EconomyManager _economy;

    private void Start()
    {
        _economy = GetComponent<EconomyManager>();
        if (_economy == null)
        {
            _economy = FindFirstObjectByType<EconomyManager>();
        }

        if (_economy != null)
        {
            _economy.OnCashChanged += UpdateMoneyText;
        }

        // Tự động tìm HUD_Canvas hoặc tạo mới để hiển thị UI Tiền
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null)
        {
            canvas = existingCanvas.GetComponent<Canvas>();
        }
        else
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas != null)
        {
            // Kiểm tra xem đã có chữ tiền chưa để tránh tạo đúp
            Transform existingText = canvas.transform.Find("MoneyText");
            if (existingText == null)
            {
                GameObject textObj = new GameObject("MoneyText");
                textObj.transform.SetParent(canvas.transform, false);
                
                RectTransform rect = textObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 1f); // Neo góc trên cùng bên phải
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-30, -30); // Cách góc phải
                rect.sizeDelta = new Vector2(400, 60);

                _moneyText = textObj.AddComponent<TextMeshProUGUI>();
                _moneyText.fontSize = 42;
                _moneyText.alignment = TextAlignmentOptions.Right;
                _moneyText.color = new Color(0.2f, 0.9f, 0.2f); // Màu xanh lá mạ
                _moneyText.fontStyle = FontStyles.Bold;
                
                UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(2, -2);
            }
            else
            {
                _moneyText = existingText.GetComponent<TextMeshProUGUI>();
            }
        }

        if (_economy != null)
        {
            UpdateMoneyText(_economy.CurrentCash);
        }

        MinigameManager.OnMinigameStartedGlobal += HandleMinigameStarted;
        MinigameManager.OnMinigameCompletedGlobal += HandleMinigameCompleted;
        SubscribeToMinigameManagerInstance();

        if (IsMinigameCurrentlyActive())
        {
            if (_moneyText != null) _moneyText.gameObject.SetActive(false);
        }
    }

    private float _lastCash = -1f;

    private void UpdateMoneyText(float currentCash)
    {
        if (_lastCash >= 0 && currentCash != _lastCash)
        {
            float diff = currentCash - _lastCash;
            SpawnFloatingText(diff);
        }

        _lastCash = currentCash;

        if (_moneyText != null)
        {
            _moneyText.text = $"Tiền: {currentCash:N0} VNĐ";
        }
    }

    private void SpawnFloatingText(float amount)
    {
        if (_moneyText == null || !_moneyText.gameObject.activeInHierarchy) return;

        GameObject floatObj = new GameObject("FloatingText");
        floatObj.transform.SetParent(_moneyText.transform.parent, false);

        RectTransform rect = floatObj.AddComponent<RectTransform>();
        rect.anchorMin = _moneyText.rectTransform.anchorMin;
        rect.anchorMax = _moneyText.rectTransform.anchorMax;
        rect.pivot = _moneyText.rectTransform.pivot;
        
        // Vị trí xuất phát thấp hơn tiền hiện tại một chút
        rect.anchoredPosition = _moneyText.rectTransform.anchoredPosition + new Vector2(0, -50);
        rect.sizeDelta = new Vector2(400, 60);

        TextMeshProUGUI floatText = floatObj.AddComponent<TextMeshProUGUI>();
        floatText.fontSize = 36;
        floatText.alignment = TextAlignmentOptions.Right;
        floatText.fontStyle = FontStyles.Bold;

        if (amount > 0)
        {
            floatText.text = $"+{amount:N0} đ";
            floatText.color = new Color(0.2f, 1f, 0.2f); // Xanh lá
        }
        else
        {
            floatText.text = $"{amount:N0} đ";
            floatText.color = new Color(1f, 0.2f, 0.2f); // Đỏ
        }

        UnityEngine.UI.Outline outline = floatObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        floatObj.AddComponent<FloatingTextAnim>();
    }

    private void HandleMinigameStarted(IMinigame minigame)
    {
        if (_moneyText != null)
        {
            _moneyText.gameObject.SetActive(false);
        }
    }

    private void HandleMinigameCompleted(RepairQuality quality)
    {
        if (_moneyText != null)
        {
            _moneyText.gameObject.SetActive(true);
        }
    }

    private bool IsMinigameCurrentlyActive()
    {
        if (MinigameManager.Instance != null && MinigameManager.Instance.IsMinigameActive)
        {
            return true;
        }
        MinigameManager mm = FindFirstObjectByType<MinigameManager>();
        return mm != null && mm.IsMinigameActive;
    }

    private void SubscribeToMinigameManagerInstance()
    {
        MinigameManager mm = MinigameManager.Instance != null ? MinigameManager.Instance : FindFirstObjectByType<MinigameManager>();
        if (mm != null)
        {
            mm.OnMinigameStarted -= HandleMinigameStarted;
            mm.OnMinigameStarted += HandleMinigameStarted;
            mm.OnMinigameCompleted -= HandleMinigameCompleted;
            mm.OnMinigameCompleted += HandleMinigameCompleted;
        }
    }

    private void UnsubscribeFromMinigameManagerInstance()
    {
        MinigameManager mm = MinigameManager.Instance != null ? MinigameManager.Instance : FindFirstObjectByType<MinigameManager>();
        if (mm != null)
        {
            mm.OnMinigameStarted -= HandleMinigameStarted;
            mm.OnMinigameCompleted -= HandleMinigameCompleted;
        }
    }

    private void OnDestroy()
    {
        if (_economy != null)
        {
            _economy.OnCashChanged -= UpdateMoneyText;
        }
        MinigameManager.OnMinigameStartedGlobal -= HandleMinigameStarted;
        MinigameManager.OnMinigameCompletedGlobal -= HandleMinigameCompleted;
        UnsubscribeFromMinigameManagerInstance();
    }
}

// Trigger recompile

