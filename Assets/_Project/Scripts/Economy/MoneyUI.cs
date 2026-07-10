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
            _economy = FindObjectOfType<EconomyManager>();
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
            canvas = FindObjectOfType<Canvas>();
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
                rect.anchoredPosition = new Vector2(-30, -30); // Cách góc một chút
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
    }

    private void UpdateMoneyText(float currentCash)
    {
        if (_moneyText != null)
        {
            _moneyText.text = $"Tiền: {currentCash:N0} VNĐ";
        }
    }

    private void OnDestroy()
    {
        if (_economy != null)
        {
            _economy.OnCashChanged -= UpdateMoneyText;
        }
    }
}
