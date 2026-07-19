using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NeedsHUDUI : MonoBehaviour
{
    private static NeedsHUDUI instance;

    private sealed class NeedRow
    {
        public TextMeshProUGUI Label;
        public TextMeshProUGUI Value;
        public Image Track;
        public Image Fill;
        public Color BaseColor;
    }

    private RectTransform _panelRect;
    private Image _panelImage;
    private NeedRow _fatigueRow;
    private NeedRow _hungerRow;
    private NeedRow _thirstRow;
    private Sprite _panelSprite;
    private Sprite _barSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        EnsureInstance();
    }

    public static NeedsHUDUI EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<NeedsHUDUI>();
        if (instance != null)
        {
            return instance;
        }

        GameObject hudObject = new GameObject("NeedsHUDUI");
        instance = hudObject.AddComponent<NeedsHUDUI>();
        DontDestroyOnLoad(hudObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        _panelSprite = CreateRoundedSprite(96, 18);
        _barSprite = CreateRoundedSprite(64, 18);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerNeeds needs = PlayerNeeds.EnsureInstance();
        needs.OnNeedsChanged += Refresh;

        MinigameManager.OnMinigameStartedGlobal += HandleMinigameStarted;
        MinigameManager.OnMinigameCompletedGlobal += HandleMinigameCompleted;
        SubscribeToMinigameManagerInstance();
    }

    private void Start()
    {
        if (IsValidScene(SceneManager.GetActiveScene().name))
        {
            BuildOrBindUI();
            Refresh(PlayerNeeds.EnsureInstance());
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (PlayerNeeds.Instance != null)
        {
            PlayerNeeds.Instance.OnNeedsChanged -= Refresh;
        }

        MinigameManager.OnMinigameStartedGlobal -= HandleMinigameStarted;
        MinigameManager.OnMinigameCompletedGlobal -= HandleMinigameCompleted;
        UnsubscribeFromMinigameManagerInstance();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _panelRect = null;
        _panelImage = null;
        _fatigueRow = null;
        _hungerRow = null;
        _thirstRow = null;

        if (IsValidScene(scene.name))
        {
            BuildOrBindUI();
            Refresh(PlayerNeeds.EnsureInstance());

            if (_panelRect != null && IsMinigameCurrentlyActive())
            {
                _panelRect.gameObject.SetActive(false);
            }
        }
        else if (_panelRect != null)
        {
            _panelRect.gameObject.SetActive(false);
        }

        SubscribeToMinigameManagerInstance();
    }

    private bool IsValidScene(string sceneName)
    {
        return sceneName == "Shop_Main" || sceneName == "VietnamStreet";
    }

    private void BuildOrBindUI()
    {
        if (!IsValidScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        Canvas canvas = FindOrCreateHudCanvas();
        if (canvas == null)
        {
            return;
        }

        Transform existingPanel = canvas.transform.Find("NeedsHUD_Panel");
        GameObject panelObject = existingPanel != null ? existingPanel.gameObject : new GameObject("NeedsHUD_Panel", typeof(RectTransform));
        panelObject.transform.SetParent(canvas.transform, false);

        _panelRect = panelObject.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0f, 1f);
        _panelRect.anchorMax = new Vector2(0f, 1f);
        _panelRect.pivot = new Vector2(0f, 1f);
        _panelRect.anchoredPosition = new Vector2(18f, -108f);
        _panelRect.sizeDelta = new Vector2(360f, 124f);
        _panelRect.localScale = Vector3.one;

        _panelImage = GetOrAdd<Image>(panelObject);
        _panelImage.sprite = _panelSprite;
        _panelImage.type = Image.Type.Sliced;
        _panelImage.color = new Color(0.018f, 0.026f, 0.036f, 0.72f);
        _panelImage.raycastTarget = false;

        DisableGraphicEffects(panelObject);

        Image accent = FindOrCreateImage("NeedsHUD_Accent", panelObject.transform);
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.offsetMin = new Vector2(18f, -5f);
        accentRect.offsetMax = new Vector2(-18f, -2f);
        accent.sprite = _barSprite;
        accent.type = Image.Type.Sliced;
        accent.color = new Color(0.42f, 0.86f, 1f, 0.26f);
        accent.raycastTarget = false;

        _fatigueRow = BuildRow(panelObject.transform, "Fatigue", "Năng lượng", "NL", new Vector2(16f, -16f), new Color(0.36f, 0.76f, 1f, 1f));
        _hungerRow = BuildRow(panelObject.transform, "Hunger", "Đói", "ĐÓI", new Vector2(16f, -52f), new Color(1f, 0.64f, 0.24f, 1f));
        _thirstRow = BuildRow(panelObject.transform, "Thirst", "Khát", "KHÁT", new Vector2(16f, -88f), new Color(0.28f, 0.94f, 1f, 1f));

        panelObject.SetActive(true);
    }

    private NeedRow BuildRow(Transform parent, string id, string label, string chip, Vector2 position, Color fillColor)
    {
        Transform existing = parent.Find(id + "_Row");
        GameObject rowObject = existing != null ? existing.gameObject : new GameObject(id + "_Row", typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = position;
        rowRect.sizeDelta = new Vector2(-32f, 30f);

        Image rowBg = FindOrCreateImage(id + "_RowBg", rowObject.transform);
        RectTransform rowBgRect = rowBg.rectTransform;
        rowBgRect.anchorMin = Vector2.zero;
        rowBgRect.anchorMax = Vector2.one;
        rowBgRect.offsetMin = Vector2.zero;
        rowBgRect.offsetMax = Vector2.zero;
        rowBg.sprite = _panelSprite;
        rowBg.type = Image.Type.Sliced;
        rowBg.color = new Color(fillColor.r, fillColor.g, fillColor.b, 0.055f);
        rowBg.raycastTarget = false;
        rowBg.transform.SetAsFirstSibling();

        Image chipBg = FindOrCreateImage(id + "_Chip", rowObject.transform);
        RectTransform chipRect = chipBg.rectTransform;
        chipRect.anchorMin = new Vector2(0f, 0.5f);
        chipRect.anchorMax = new Vector2(0f, 0.5f);
        chipRect.pivot = new Vector2(0f, 0.5f);
        chipRect.anchoredPosition = new Vector2(8f, 0f);
        chipRect.sizeDelta = new Vector2(44f, 22f);
        chipBg.sprite = _barSprite;
        chipBg.type = Image.Type.Sliced;
        chipBg.color = new Color(fillColor.r, fillColor.g, fillColor.b, 0.26f);
        chipBg.raycastTarget = false;

        TextMeshProUGUI chipText = FindOrCreateText(id + "_ChipText", chipBg.transform);
        RectTransform chipTextRect = chipText.rectTransform;
        chipTextRect.anchorMin = Vector2.zero;
        chipTextRect.anchorMax = Vector2.one;
        chipTextRect.offsetMin = Vector2.zero;
        chipTextRect.offsetMax = Vector2.zero;
        chipText.text = chip;
        chipText.fontSize = 11.5f;
        chipText.fontStyle = FontStyles.Bold;
        chipText.alignment = TextAlignmentOptions.Center;
        chipText.textWrappingMode = TextWrappingModes.NoWrap;
        chipText.color = Color.Lerp(fillColor, Color.white, 0.28f);
        chipText.raycastTarget = false;

        TextMeshProUGUI labelText = FindOrCreateText(id + "_Label", rowObject.transform);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(62f, 0f);
        labelRect.sizeDelta = new Vector2(92f, 24f);
        labelText.text = label;
        labelText.fontSize = 13.2f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.color = new Color(0.88f, 0.93f, 0.98f, 0.96f);
        labelText.raycastTarget = false;

        Image barBg = FindOrCreateImage(id + "_BarBg", rowObject.transform);
        RectTransform bgRect = barBg.rectTransform;
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(1f, 0.5f);
        bgRect.pivot = new Vector2(0f, 0.5f);
        bgRect.offsetMin = new Vector2(156f, -7f);
        bgRect.offsetMax = new Vector2(-54f, 7f);
        barBg.sprite = _barSprite;
        barBg.type = Image.Type.Sliced;
        barBg.color = new Color(0.055f, 0.065f, 0.078f, 0.72f);
        barBg.raycastTarget = false;

        Image fill = FindOrCreateImage(id + "_Fill", barBg.transform);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.sprite = _barSprite;
        fill.type = Image.Type.Sliced;
        fill.color = Color.Lerp(fillColor, Color.white, 0.06f);
        fill.raycastTarget = false;

        Image sheen = FindOrCreateImage(id + "_FillSheen", fill.transform);
        RectTransform sheenRect = sheen.rectTransform;
        sheenRect.anchorMin = new Vector2(0f, 0.52f);
        sheenRect.anchorMax = Vector2.one;
        sheenRect.offsetMin = Vector2.zero;
        sheenRect.offsetMax = Vector2.zero;
        sheen.sprite = _barSprite;
        sheen.type = Image.Type.Sliced;
        sheen.color = new Color(1f, 1f, 1f, 0.16f);
        sheen.raycastTarget = false;

        TextMeshProUGUI valueText = FindOrCreateText(id + "_Value", rowObject.transform);
        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(-8f, 0f);
        valueRect.sizeDelta = new Vector2(44f, 24f);
        valueText.fontSize = 12.8f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.color = new Color(0.92f, 0.95f, 1f, 0.94f);
        valueText.raycastTarget = false;

        return new NeedRow
        {
            Label = labelText,
            Value = valueText,
            Track = barBg,
            Fill = fill,
            BaseColor = fillColor
        };
    }

    private void Refresh(PlayerNeeds needs)
    {
        if (needs == null)
        {
            return;
        }

        if (_panelRect == null)
        {
            BuildOrBindUI();
        }

        if (_panelRect == null)
        {
            return;
        }

        _panelRect.gameObject.SetActive(IsValidScene(SceneManager.GetActiveScene().name) && !IsMinigameCurrentlyActive());
        UpdateRow(_fatigueRow, needs.FatiguePercent);
        UpdateRow(_hungerRow, needs.HungerPercent);
        UpdateRow(_thirstRow, needs.ThirstPercent);
    }

    private void UpdateRow(NeedRow row, float percent)
    {
        if (row == null || row.Fill == null)
        {
            return;
        }

        float clampedPercent = Mathf.Clamp01(percent);
        RectTransform fillRect = row.Fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(clampedPercent, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Color healthyColor = Color.Lerp(row.BaseColor, Color.white, 0.08f);
        Color warningColor = new Color(1f, 0.38f, 0.28f, 1f);
        Color targetColor = clampedPercent < 0.24f ? warningColor : healthyColor;
        row.Fill.color = Color.Lerp(row.Fill.color, targetColor, 0.35f);

        if (row.Track != null)
        {
            row.Track.color = clampedPercent < 0.24f
                ? new Color(0.22f, 0.08f, 0.07f, 0.72f)
                : new Color(0.055f, 0.065f, 0.078f, 0.72f);
        }

        if (row.Value != null)
        {
            row.Value.text = Mathf.RoundToInt(clampedPercent * 100f) + "%";
            row.Value.color = clampedPercent < 0.24f ? new Color(1f, 0.7f, 0.58f, 1f) : new Color(0.92f, 0.95f, 1f, 0.94f);
        }
    }

    private Canvas FindOrCreateHudCanvas()
    {
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        Canvas canvas = existingCanvas != null ? existingCanvas.GetComponent<Canvas>() : null;
        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasObject = new GameObject("HUD_Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private TextMeshProUGUI FindOrCreateText(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
        {
            return existingText;
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    private Image FindOrCreateImage(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out Image existingImage))
        {
            return existingImage;
        }

        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private void HandleMinigameStarted(IMinigame minigame)
    {
        if (_panelRect != null)
        {
            _panelRect.gameObject.SetActive(false);
        }
    }

    private void HandleMinigameCompleted(RepairQuality quality)
    {
        Refresh(PlayerNeeds.Instance);
    }

    private bool IsMinigameCurrentlyActive()
    {
        if (MinigameManager.Instance != null && MinigameManager.Instance.IsMinigameActive)
        {
            return true;
        }

        MinigameManager manager = FindFirstObjectByType<MinigameManager>();
        return manager != null && manager.IsMinigameActive;
    }

    private void SubscribeToMinigameManagerInstance()
    {
        MinigameManager manager = MinigameManager.Instance != null ? MinigameManager.Instance : FindFirstObjectByType<MinigameManager>();
        if (manager == null)
        {
            return;
        }

        manager.OnMinigameStarted -= HandleMinigameStarted;
        manager.OnMinigameStarted += HandleMinigameStarted;
        manager.OnMinigameCompleted -= HandleMinigameCompleted;
        manager.OnMinigameCompleted += HandleMinigameCompleted;
    }

    private void UnsubscribeFromMinigameManagerInstance()
    {
        MinigameManager manager = MinigameManager.Instance != null ? MinigameManager.Instance : FindFirstObjectByType<MinigameManager>();
        if (manager == null)
        {
            return;
        }

        manager.OnMinigameStarted -= HandleMinigameStarted;
        manager.OnMinigameCompleted -= HandleMinigameCompleted;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private void DisableGraphicEffects(GameObject target)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.enabled = false;
        }

        UnityEngine.UI.Outline outline = target.GetComponent<UnityEngine.UI.Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    private Sprite CreateRoundedSprite(int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, size, radius);
                texture.SetPixel(x, y, inside ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    private bool IsInsideRoundedRect(int x, int y, int size, int radius)
    {
        int left = radius;
        int right = size - radius - 1;
        int bottom = radius;
        int top = size - radius - 1;

        int nearestX = Mathf.Clamp(x, left, right);
        int nearestY = Mathf.Clamp(y, bottom, top);
        int dx = x - nearestX;
        int dy = y - nearestY;
        return (dx * dx) + (dy * dy) <= radius * radius;
    }
}
