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
        public float TargetPercent = 1f;
        public float DisplayedPercent = 1f;
        public bool Initialized;
    }

    private RectTransform _panelRect;
    private Image _panelImage;
    private NeedRow _fatigueRow;
    private NeedRow _hungerRow;
    private NeedRow _thirstRow;
    private Sprite _panelSprite;
    private Sprite _barSprite;
    private Sprite _circleSprite;
    private Sprite _ringSprite;


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
        _circleSprite = CreateCircleSprite(96);
        _ringSprite = CreateRingSprite(128, 16);
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
        _panelRect.anchorMin = new Vector2(1f, 0f);
        _panelRect.anchorMax = new Vector2(1f, 0f);
        _panelRect.pivot = new Vector2(1f, 0f);
        _panelRect.anchoredPosition = new Vector2(-42f, 42f);
        _panelRect.sizeDelta = new Vector2(204f, 72f);
        _panelRect.localScale = Vector3.one;

        _panelImage = GetOrAdd<Image>(panelObject);
        _panelImage.enabled = false;
        _panelImage.raycastTarget = false;

        DisableGraphicEffects(panelObject);

        Image accent = FindOrCreateImage("NeedsHUD_Accent", panelObject.transform);
        accent.gameObject.SetActive(false);

        _fatigueRow = BuildRow(panelObject.transform, "Fatigue", "NL", "NL", new Vector2(0f, 4f), new Color(0.40f, 0.77f, 1f, 1f));
        _hungerRow = BuildRow(panelObject.transform, "Hunger", "\u0110\u00F3i", "\u0110\u00D3I", new Vector2(70f, 4f), new Color(1f, 0.66f, 0.24f, 1f));
        _thirstRow = BuildRow(panelObject.transform, "Thirst", "Kh\u00E1t", "KH\u00C1T", new Vector2(140f, 4f), new Color(0.25f, 0.92f, 1f, 1f));

        panelObject.SetActive(true);
    }

private NeedRow BuildRow(Transform parent, string id, string label, string chip, Vector2 position, Color fillColor)
    {
        Transform existing = parent.Find(id + "_Row");
        GameObject rowObject = existing != null ? existing.gameObject : new GameObject(id + "_Row", typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(0f, 0f);
        rowRect.pivot = new Vector2(0f, 0f);
        rowRect.anchoredPosition = position;
        rowRect.sizeDelta = new Vector2(64f, 66f);

        Image rowBg = FindOrCreateImage(id + "_RowBg", rowObject.transform);
        RectTransform rowBgRect = rowBg.rectTransform;
        rowBgRect.anchorMin = new Vector2(0.5f, 0f);
        rowBgRect.anchorMax = new Vector2(0.5f, 0f);
        rowBgRect.pivot = new Vector2(0.5f, 0.5f);
        rowBgRect.anchoredPosition = new Vector2(32f, 40f);
        rowBgRect.sizeDelta = new Vector2(54f, 54f);
        rowBg.sprite = _circleSprite;
        rowBg.type = Image.Type.Simple;
        rowBg.color = new Color(0.012f, 0.018f, 0.026f, 0.62f);
        rowBg.raycastTarget = false;
        rowBg.transform.SetAsFirstSibling();

        Image chipBg = FindOrCreateImage(id + "_Chip", rowObject.transform);
        chipBg.gameObject.SetActive(false);

        TextMeshProUGUI chipText = FindOrCreateText(id + "_ChipText", chipBg.transform);
        chipText.text = string.Empty;
        chipText.gameObject.SetActive(false);

        Image barBg = FindOrCreateImage(id + "_BarBg", rowObject.transform);
        RectTransform bgRect = barBg.rectTransform;
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(32f, 40f);
        bgRect.sizeDelta = new Vector2(54f, 54f);
        barBg.sprite = _ringSprite;
        barBg.type = Image.Type.Simple;
        barBg.color = new Color(1f, 1f, 1f, 0.15f);
        barBg.raycastTarget = false;

        Image fill = FindOrCreateImage(id + "_Fill", barBg.transform);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.sprite = _ringSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 1f;
        fill.color = Color.Lerp(fillColor, Color.white, 0.08f);
        fill.raycastTarget = false;

        Image sheen = FindOrCreateImage(id + "_FillSheen", fill.transform);
        sheen.gameObject.SetActive(false);

        TextMeshProUGUI valueText = FindOrCreateText(id + "_Value", rowObject.transform);
        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(0.5f, 0f);
        valueRect.anchorMax = new Vector2(0.5f, 0f);
        valueRect.pivot = new Vector2(0.5f, 0.5f);
        valueRect.anchoredPosition = new Vector2(32f, 42f);
        valueRect.sizeDelta = new Vector2(54f, 20f);
        valueText.fontSize = 13.2f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.color = new Color(0.95f, 0.98f, 1f, 0.96f);
        valueText.raycastTarget = false;

        TextMeshProUGUI labelText = FindOrCreateText(id + "_Label", rowObject.transform);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(32f, 0f);
        labelRect.sizeDelta = new Vector2(62f, 15f);
        labelText.text = label;
        labelText.fontSize = 10f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.color = Color.Lerp(fillColor, Color.white, 0.24f);
        labelText.raycastTarget = false;

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
        row.TargetPercent = clampedPercent;

        if (!row.Initialized)
        {
            row.DisplayedPercent = clampedPercent;
            row.Initialized = true;
            PaintRow(row, clampedPercent);
        }
    }

    private void Update()
    {
        if (_panelRect == null || !_panelRect.gameObject.activeInHierarchy)
        {
            return;
        }

        AnimateRow(_fatigueRow);
        AnimateRow(_hungerRow);
        AnimateRow(_thirstRow);
    }

    private void AnimateRow(NeedRow row)
    {
        if (row == null || row.Fill == null || !row.Initialized)
        {
            return;
        }

        float t = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
        float nextPercent = Mathf.Lerp(row.DisplayedPercent, row.TargetPercent, t);
        if (Mathf.Abs(nextPercent - row.TargetPercent) < 0.001f)
        {
            nextPercent = row.TargetPercent;
        }

        if (Mathf.Approximately(nextPercent, row.DisplayedPercent))
        {
            return;
        }

        row.DisplayedPercent = nextPercent;
        PaintRow(row, nextPercent);
    }

private void PaintRow(NeedRow row, float percent)
    {
        float clampedPercent = Mathf.Clamp01(percent);
        row.Fill.type = Image.Type.Filled;
        row.Fill.fillMethod = Image.FillMethod.Radial360;
        row.Fill.fillOrigin = (int)Image.Origin360.Top;
        row.Fill.fillClockwise = true;
        row.Fill.fillAmount = clampedPercent;

        bool isLow = clampedPercent < 0.24f;
        Color healthyColor = Color.Lerp(row.BaseColor, Color.white, 0.08f);
        Color warningColor = new Color(1f, 0.34f, 0.24f, 1f);
        Color targetColor = isLow ? warningColor : healthyColor;
        row.Fill.color = Color.Lerp(row.Fill.color, targetColor, 0.45f);

        if (row.Track != null)
        {
            row.Track.color = isLow
                ? new Color(1f, 0.34f, 0.24f, 0.18f)
                : new Color(1f, 1f, 1f, 0.15f);
        }

        if (row.Label != null)
        {
            row.Label.color = isLow ? new Color(1f, 0.72f, 0.58f, 1f) : Color.Lerp(row.BaseColor, Color.white, 0.24f);
        }

        if (row.Value != null)
        {
            row.Value.text = Mathf.RoundToInt(clampedPercent * 100f) + "%";
            row.Value.color = isLow ? new Color(1f, 0.72f, 0.58f, 1f) : new Color(0.95f, 0.98f, 1f, 0.96f);
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

private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                texture.SetPixel(x, y, (dx * dx) + (dy * dy) <= radius * radius ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

private Sprite CreateRingSprite(int size, int thickness)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        float center = (size - 1) * 0.5f;
        float outer = center - 1f;
        float inner = Mathf.Max(0f, outer - thickness);
        float outerSqr = outer * outer;
        float innerSqr = inner * inner;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distanceSqr = (dx * dx) + (dy * dy);
                texture.SetPixel(x, y, distanceSqr <= outerSqr && distanceSqr >= innerSqr ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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
