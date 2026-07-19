using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DaySummaryUI : MonoBehaviour
{
    private GameObject _panel;
    private TextMeshProUGUI _headerText;
    
    // Cached Row Values
    private GameObject _incomeRow;
    private TextMeshProUGUI _incomeValue;
    
    private GameObject _expensesRow;
    private TextMeshProUGUI _expensesValue;
    
    private GameObject _installmentRow;
    private TextMeshProUGUI _installmentValue;
    
    private GameObject _debtRow;
    private TextMeshProUGUI _debtValue;
    
    private GameObject _netRow;
    private TextMeshProUGUI _netValue;
    
    // Buttons
    private Button _nextDayBtn;
    private TextMeshProUGUI _nextDayBtnText;
    private Button _vayNhanhBtn;
    private PlayerController _summaryPlayer;
    private PlayerCamera _summaryCamera;
    private bool _playerWasEnabled;
    private bool _cameraWasEnabled;

    private void LateUpdate()
    {
        if (_panel == null || !_panel.activeInHierarchy) return;

        // Mot so UI/gameplay persistent co the khoa lai chuot sau khi bang tong ket
        // da mo. Giu con tro mo trong moi frame ke ca khi Time.timeScale = 0.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        CreateUI();
        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnDayEnded += ShowSummary;
        }
    }

    private void CreateUI()
    {
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null) canvas = existingCanvas.GetComponent<Canvas>();
        
        if (canvas == null)
        {
            GameObject newCanvasObj = new GameObject("DaySummary_Canvas");
            canvas = newCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            newCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            newCanvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 1. Fullscreen Background
        _panel = new GameObject("DaySummaryPanel");
        _panel.transform.SetParent(canvas.transform, false);
        _panel.SetActive(false);

        RectTransform panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f); 

        // 2. Card Container
        GameObject cardObj = new GameObject("Card");
        cardObj.transform.SetParent(_panel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(800, 0); // Tự chỉnh chiều cao
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.15f, 1f); // Darker blue-grey
        UnityEngine.UI.Shadow cardShadow = cardObj.AddComponent<UnityEngine.UI.Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        cardShadow.effectDistance = new Vector2(5, -5);

        ContentSizeFitter cardCSF = cardObj.AddComponent<ContentSizeFitter>();
        cardCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup cardVLG = cardObj.AddComponent<VerticalLayoutGroup>();
        cardVLG.padding = new RectOffset(60, 60, 50, 50);
        cardVLG.spacing = 15;
        cardVLG.childAlignment = TextAnchor.UpperCenter;
        cardVLG.childControlHeight = true;
        cardVLG.childControlWidth = true;
        cardVLG.childForceExpandHeight = false;
        cardVLG.childForceExpandWidth = false;

        // 3. Header
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(cardObj.transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0, 80);

        _headerText = headerObj.AddComponent<TextMeshProUGUI>();
        _headerText.fontSize = 54;
        _headerText.fontStyle = FontStyles.Bold;
        _headerText.alignment = TextAlignmentOptions.Center;
        _headerText.color = new Color(1f, 0.95f, 0.8f); // Hơi vàng nhẹ sang trọng
        _headerText.text = "TỔNG KẾT NGÀY";

        // Space
        GameObject space1 = new GameObject("Space");
        space1.transform.SetParent(cardObj.transform, false);
        space1.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 15);

        // 4. Rows
        CreateRow(cardObj.transform, "Thu nhập:", out _incomeRow, out _incomeValue, 32);
        CreateRow(cardObj.transform, "Chi phí sinh hoạt:", out _expensesRow, out _expensesValue, 32);
        CreateRow(cardObj.transform, "Trả góp ngân hàng:", out _installmentRow, out _installmentValue, 32);
        CreateRow(cardObj.transform, "Dư nợ hiện tại:", out _debtRow, out _debtValue, 32);

        // Space
        GameObject space2 = new GameObject("Space");
        space2.transform.SetParent(cardObj.transform, false);
        space2.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 5);

        // Separator
        GameObject separator = new GameObject("Separator");
        separator.transform.SetParent(cardObj.transform, false);
        separator.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 2);
        separator.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.45f);

        // Space
        GameObject space3 = new GameObject("Space");
        space3.transform.SetParent(cardObj.transform, false);
        space3.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 5);

        // Net Profit Row
        CreateRow(cardObj.transform, "LỢI NHUẬN RÒNG:", out _netRow, out _netValue, 42, true);

        // Space
        GameObject space4 = new GameObject("Space");
        space4.transform.SetParent(cardObj.transform, false);
        space4.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 10);

        // 5. Buttons Container
        GameObject btnContainer = new GameObject("ButtonsContainer");
        btnContainer.transform.SetParent(cardObj.transform, false);
        RectTransform btnContRect = btnContainer.AddComponent<RectTransform>();

        VerticalLayoutGroup btnVLG = btnContainer.AddComponent<VerticalLayoutGroup>();
        btnVLG.spacing = 15;
        btnVLG.childAlignment = TextAnchor.LowerCenter;
        btnVLG.childControlHeight = true;
        btnVLG.childControlWidth = true;
        btnVLG.childForceExpandHeight = false;
        btnVLG.childForceExpandWidth = true;

        // Button 1 (Next Day)
        GameObject btnObj = new GameObject("NextDayButton");
        btnObj.transform.SetParent(btnContainer.transform, false);
        LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
        btnLE.minHeight = 85;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.6f, 0.2f);
        UnityEngine.UI.Shadow btnShadow = btnObj.AddComponent<UnityEngine.UI.Shadow>();
        btnShadow.effectColor = new Color(0, 0, 0, 0.3f);
        btnShadow.effectDistance = new Vector2(2, -2);

        _nextDayBtn = btnObj.AddComponent<Button>();
        _nextDayBtn.onClick.AddListener(OnNextDayClicked);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        btnTextRect.anchoredPosition = Vector2.zero;

        _nextDayBtnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        _nextDayBtnText.fontSize = 28;
        _nextDayBtnText.fontStyle = FontStyles.Bold;
        _nextDayBtnText.alignment = TextAlignmentOptions.Center;
        _nextDayBtnText.color = Color.white;

        // Button 2 (Vay Nhanh)
        GameObject vayBtnObj = new GameObject("VayNhanhButton");
        vayBtnObj.transform.SetParent(btnContainer.transform, false);
        LayoutElement vayBtnLE = vayBtnObj.AddComponent<LayoutElement>();
        vayBtnLE.minHeight = 85;

        Image vayBtnImg = vayBtnObj.AddComponent<Image>();
        vayBtnImg.color = new Color(0.85f, 0.45f, 0.1f);
        UnityEngine.UI.Shadow vayBtnShadow = vayBtnObj.AddComponent<UnityEngine.UI.Shadow>();
        vayBtnShadow.effectColor = new Color(0, 0, 0, 0.3f);
        vayBtnShadow.effectDistance = new Vector2(2, -2);

        _vayNhanhBtn = vayBtnObj.AddComponent<Button>();
        _vayNhanhBtn.onClick.AddListener(() => {
            if (EconomyManager.Instance != null && !EconomyManager.Instance.HasActiveLoan)
            {
                EconomyManager.Instance.TakeLoan();
                UpdateUIState();
            }
        });

        GameObject vayTextObj = new GameObject("Text");
        vayTextObj.transform.SetParent(vayBtnObj.transform, false);
        RectTransform vayTextRect = vayTextObj.AddComponent<RectTransform>();
        vayTextRect.anchorMin = Vector2.zero;
        vayTextRect.anchorMax = Vector2.one;
        vayTextRect.sizeDelta = Vector2.zero;
        vayTextRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI vayText = vayTextObj.AddComponent<TextMeshProUGUI>();
        vayText.text = "VAY NÓNG 100,000Đ";
        vayText.fontSize = 28;
        vayText.fontStyle = FontStyles.Bold;
        vayText.alignment = TextAlignmentOptions.Center;
        vayText.color = Color.white;
    }

    private void CreateRow(Transform parent, string titleText, out GameObject rowObj, out TextMeshProUGUI valueText, int fontSize = 30, bool isBold = false)
    {
        rowObj = new GameObject("Row_" + titleText);
        rowObj.transform.SetParent(parent, false);
        RectTransform rect = rowObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, fontSize + 10);

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = true;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rowObj.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = titleText;
        label.fontSize = fontSize;
        label.color = new Color(0.9f, 0.9f, 0.9f);
        if (isBold) label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Left;

        GameObject valObj = new GameObject("Value");
        valObj.transform.SetParent(rowObj.transform, false);
        valueText = valObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "0đ";
        valueText.fontSize = fontSize;
        if (isBold) valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Right;
    }

    private void ShowSummary()
    {
        if (_panel == null) CreateUI();
        if (_panel == null) return;
        
        Time.timeScale = 0f;
        
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LockGameplayForSummary();
        if (_nextDayBtn != null)
        {
            _nextDayBtn.Select();
        }
        
        UpdateUIState();
    }

    private void LockGameplayForSummary()
    {
        _summaryPlayer = FindAnyObjectByType<PlayerController>();
        _summaryCamera = FindAnyObjectByType<PlayerCamera>();

        if (_summaryPlayer != null)
        {
            _playerWasEnabled = _summaryPlayer.enabled;
            _summaryPlayer.enabled = false;
        }

        if (_summaryCamera != null)
        {
            _cameraWasEnabled = _summaryCamera.enabled;
            _summaryCamera.enabled = false;
        }
    }

    private void RestoreGameplayAfterSummary()
    {
        if (_summaryPlayer != null) _summaryPlayer.enabled = _playerWasEnabled;
        if (_summaryCamera != null) _summaryCamera.enabled = _cameraWasEnabled;
        _summaryPlayer = null;
        _summaryCamera = null;
    }

    private void UpdateUIState()
    {
        if (EconomyManager.Instance == null) return;
        
        float income = EconomyManager.Instance.DailyIncome;
        float expenses = EconomyManager.Instance.DailyExpenses;
        float dailyInstallment = EconomyManager.Instance.DailyInstallment;
        float totalDeduction = EconomyManager.Instance.TotalDailyDeduction;
        float cash = EconomyManager.Instance.CurrentCash;
        float debt = EconomyManager.Instance.CurrentDebt;
        bool hasActiveLoan = EconomyManager.Instance.HasActiveLoan;
        float net = income - totalDeduction;

        int day = DayClock.Instance != null ? DayClock.Instance.CurrentDay : 1;
        
        _headerText.text = $"TỔNG KẾT NGÀY {day}";

        _incomeValue.text = $"+{income:N0}đ";
        _incomeValue.color = new Color(0.29f, 0.87f, 0.50f);

        _expensesValue.text = $"-{expenses:N0}đ";
        _expensesValue.color = new Color(0.97f, 0.44f, 0.44f);

        if (dailyInstallment > 0)
        {
            _installmentRow.SetActive(true);
            _installmentValue.text = $"-{dailyInstallment:N0}đ";
            _installmentValue.color = new Color(0.97f, 0.44f, 0.44f);
        }
        else
        {
            _installmentRow.SetActive(false);
        }

        if (debt > 0)
        {
            _debtRow.SetActive(true);
            _debtValue.text = $"{debt:N0}đ";
            _debtValue.color = new Color(0.98f, 0.75f, 0.14f); // Cam
        }
        else
        {
            _debtRow.SetActive(false);
        }

        _netValue.text = (net >= 0 ? "+" : "") + $"{net:N0}đ";
        _netValue.color = net >= 0 ? new Color(0.29f, 0.87f, 0.50f) : new Color(0.97f, 0.44f, 0.44f);

        if (cash < expenses)
        {
            // Không đủ tiền đóng trọ -> Chắc chắn Game Over
            _nextDayBtn.interactable = true;
            _nextDayBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
            _nextDayBtnText.text = "CHẤP NHẬN BỊ ĐUỔI";
            
            if (_vayNhanhBtn != null) _vayNhanhBtn.gameObject.SetActive(!hasActiveLoan);
        }
        else if (cash < totalDeduction && hasActiveLoan)
        {
            // Đủ đóng trọ nhưng KHÔNG đủ đóng tiền góp -> Chịu phạt trễ hạn
            _nextDayBtn.interactable = true;
            _nextDayBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.1f);
            _nextDayBtnText.text = "NGỦ & CHỊU PHẠT TRỄ HẠN";
            
            if (_vayNhanhBtn != null) _vayNhanhBtn.gameObject.SetActive(false);
        }
        else if (cash < totalDeduction && !hasActiveLoan)
        {
             // Dành cho trường hợp logic lỗi (không có nợ nhưng vẫn bị trừ), hoặc để fallback
            _nextDayBtn.interactable = true;
            _nextDayBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);
            _nextDayBtnText.text = "CHẤP NHẬN BỊ ĐUỔI";
            
            if (_vayNhanhBtn != null) _vayNhanhBtn.gameObject.SetActive(true);
        }
        else
        {
            _nextDayBtn.interactable = true;
            _nextDayBtn.GetComponent<Image>().color = new Color(0.15f, 0.6f, 0.2f);
            _nextDayBtnText.text = "NGỦ & QUA NGÀY MỚI";
            
            if (_vayNhanhBtn != null) _vayNhanhBtn.gameObject.SetActive(false);
        }
    }

    private void OnNextDayClicked()
    {
        if (EconomyManager.Instance != null)
        {
            bool survived = EconomyManager.Instance.DeductDailyExpenses();
            if (!survived) return; // Đã Game Over

            EconomyManager.Instance.ResetDailyIncome();
        }

        if (_panel != null)
        {
            _panel.SetActive(false);
        }

        if (DayClock.Instance != null)
        {
            DayClock.Instance.StartNextDay();
        }

        Time.timeScale = 1f;

        RestoreGameplayAfterSummary();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (ToastNotificationManager.Instance != null)
        {
            int nextDay = DayClock.Instance != null ? DayClock.Instance.CurrentDay : 1;
            ToastNotificationManager.Instance.ShowToast($"[!] Đã bắt đầu ngày mới (Ngày {nextDay}). Chúc bạn một ngày làm việc hiệu quả!", 5f);
        }

        // Đưa người chơi quay về phòng ngủ để bắt đầu ngày mới
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Shop_Main")
        {
            LoadingScreenManager.LoadScene("Shop_Main");
        }
    }

    private void OnDestroy()
    {
        DayClock clock = DayClock.ExistingInstance;
        if (clock != null)
        {
            clock.OnDayEnded -= ShowSummary;
        }
    }
}
