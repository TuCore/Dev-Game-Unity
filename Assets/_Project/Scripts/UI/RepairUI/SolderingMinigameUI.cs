using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SolderingMinigameUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Skill Check Dial References")]
    [SerializeField] private RectTransform needle;
    [SerializeField] private Image successZoneFill;
    [SerializeField] private Image greatZoneFill;
    [SerializeField] private Image perfectZoneFill;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Skill Check Tuning")]
    [SerializeField] private float baseRotationSpeed = 185f;
    [SerializeField] private float speedIncreasePerJoint = 0.18f;
    [SerializeField] private float speedIncreasePerLoop = 0.08f;
    [SerializeField] private float maxSpeedMultiplier = 2.4f;
    [SerializeField] private float dialSize = 280f;
    [SerializeField] private float baseSuccessZoneDegrees = 54f;
    [SerializeField] private float minSuccessZoneDegrees = 28f;
    [SerializeField] private float baseGreatZoneDegrees = 26f;
    [SerializeField] private float minGreatZoneDegrees = 14f;
    [SerializeField] private float basePerfectZoneDegrees = 8f;
    [SerializeField] private float minPerfectZoneDegrees = 5f;

    [Header("Visual Style")]
    [SerializeField] private bool buildStyledDialOnAwake = true;
    [SerializeField] private Color overlayColor = new Color(0.015f, 0.018f, 0.022f, 0f);
    [SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.88f);
    [SerializeField] private Color faintRingColor = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color goodResultColor = new Color(1f, 0.12f, 0.22f, 1f);
    [SerializeField] private Color greatResultColor = new Color(1f, 0.82f, 0.16f, 1f);
    [SerializeField] private Color perfectResultColor = new Color(0.22f, 1f, 0.28f, 1f);
    [SerializeField] private Color needleColor = new Color(0.08f, 0.62f, 1f, 1f);

    private const int RingSpriteSize = 256;

    private readonly List<Image> _progressPips = new List<Image>();

    private SolderingMinigame _minigame;
    private RectTransform _dialRoot;
    private RectTransform _pipsRoot;
    private Image _outerRing;
    private Image _innerRing;
    private Image _trackRing;
    private Image _needleImage;
    private Image _centerBox;
    private TextMeshProUGUI _spaceText;
    private Sprite _ringSprite;
    private Sprite _solidSprite;
    private Coroutine _feedbackRoutine;

    private int _difficultyLevel;
    private bool _isChecking;
    private bool _styleReady;
    private float _needleAngle;
    private float _speedMultiplier;
    private float _currentCheckSpeedMultiplier;
    private int _currentCheckLoopCount;
    private float _successStartAngle;
    private float _successSize;
    private float _greatStartAngle;
    private float _greatSize;
    private float _perfectStartAngle;
    private float _perfectSize;

    private void Awake()
    {
        if (buildStyledDialOnAwake)
        {
            EnsureStyledDial();
        }

        HideUIOnSceneStart();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && mainPanel != null && mainPanel != gameObject)
        {
            mainPanel.SetActive(false);
            UnityEditor.EditorUtility.SetDirty(mainPanel);
        }
    }
#endif

    public void SetupMinigame(SolderingMinigame minigame, int difficulty)
    {
        EnsureStyledDial();

        _minigame = minigame;
        _difficultyLevel = Mathf.Max(1, difficulty);
        _speedMultiplier = 1f + ((_difficultyLevel - 1) * 0.22f);

        SetFeedback("", Color.white);
        UpdateProgressPips(0, 0);
    }

    public void ShowUI(bool show)
    {
        EnsureStyledDial();

        if (mainPanel != null)
        {
            mainPanel.SetActive(show);
        }

        _isChecking = false;

        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }

    public void TriggerSkillCheck(int current, int total)
    {
        EnsureStyledDial();

        if (progressText != null)
        {
            progressText.text = $"MỐI HÀN {current}/{total}";
        }

        SetFeedback("", Color.white);
        UpdateProgressPips(current, total);

        _needleAngle = 0f;
        _currentCheckLoopCount = 0;
        _currentCheckSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, _speedMultiplier + ((current - 1) * speedIncreasePerJoint));
        ApplyNeedleRotation();

        float difficultyOffset = Mathf.Max(0, _difficultyLevel - 1);
        
        // Nhận bonus từ nâng cấp mỏ hàn (ToolUpgradeSystem)
        float toolBonusPercent = 0f;
        if (ToolUpgradeSystem.Instance != null)
        {
            toolBonusPercent = ToolUpgradeSystem.Instance.GetAccuracyBonus("SolderingIron");
        }
        
        // Tính toán kích thước (độ) cơ bản
        float baseSuccess = Mathf.Clamp(baseSuccessZoneDegrees - (difficultyOffset * 8f), minSuccessZoneDegrees, baseSuccessZoneDegrees);
        float baseGreat = Mathf.Clamp(baseGreatZoneDegrees - (difficultyOffset * 4f), minGreatZoneDegrees, Mathf.Min(baseGreatZoneDegrees, baseSuccess));
        float basePerfect = Mathf.Clamp(basePerfectZoneDegrees - (difficultyOffset * 1.4f), minPerfectZoneDegrees, basePerfectZoneDegrees);

        // Cộng thêm góc mở rộng từ ToolBonus (VD: 5% bonus = mở rộng thêm 5% của 360 độ = 18 độ cho success)
        float bonusAngle = toolBonusPercent * 360f;
        
        _successSize = Mathf.Clamp(baseSuccess + bonusAngle, minSuccessZoneDegrees, 360f);
        _greatSize = Mathf.Clamp(baseGreat + (bonusAngle * 0.6f), minGreatZoneDegrees, _successSize);
        _perfectSize = Mathf.Clamp(basePerfect + (bonusAngle * 0.3f), minPerfectZoneDegrees, _greatSize);

        _successStartAngle = Random.Range(25f, 335f - _successSize);
        _greatStartAngle = _successStartAngle + ((_successSize - _greatSize) * 0.5f);
        _perfectStartAngle = _successStartAngle + ((_successSize - _perfectSize) * 0.5f);

        UpdateZoneVisuals();
        _isChecking = true;
    }

    private void Update()
    {
        if (!_isChecking)
        {
            return;
        }

        float previousAngle = _needleAngle;
        float activeSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, _currentCheckSpeedMultiplier + (_currentCheckLoopCount * speedIncreasePerLoop));
        _needleAngle = Mathf.Repeat(_needleAngle + (baseRotationSpeed * activeSpeedMultiplier * Time.deltaTime), 360f);

        if (_needleAngle < previousAngle)
        {
            _currentCheckLoopCount++;
        }

        ApplyNeedleRotation();

        if (CustomInputManager.GetKeyDown("MinigameAction") || Input.GetMouseButtonDown(0))
        {
            EvaluateHit();
        }
    }

    private void OnDisable()
    {
        _isChecking = false;
    }

    private void HideUIOnSceneStart()
    {
        _isChecking = false;

        if (mainPanel != null && mainPanel != gameObject)
        {
            mainPanel.SetActive(false);
        }
    }

    private void EvaluateHit()
    {
        if (IsAngleInsideWindow(_needleAngle, _perfectStartAngle, _perfectSize))
        {
            HandleSkillCheckHit("Perfect");
        }
        else if (IsAngleInsideWindow(_needleAngle, _greatStartAngle, _greatSize))
        {
            HandleSkillCheckHit("Great");
        }
        else if (IsAngleInsideWindow(_needleAngle, _successStartAngle, _successSize))
        {
            HandleSkillCheckHit("Good");
        }
        else
        {
            HandleSkillCheckHit("Miss");
        }
    }

    private void HandleSkillCheckHit(string hitType)
    {
        _isChecking = false;

        Color feedbackColor = Color.white;
        if (hitType == "Perfect")
        {
            feedbackColor = perfectResultColor;
        }
        else if (hitType == "Great")
        {
            feedbackColor = greatResultColor;
        }
        else if (hitType == "Good")
        {
            feedbackColor = goodResultColor;
        }

        SetFeedback(hitType.ToUpperInvariant(), feedbackColor);

        if (_minigame != null)
        {
            _minigame.ReportSkillCheckResult(hitType);
        }
    }

    private void EnsureStyledDial()
    {
        if (_styleReady)
        {
            return;
        }

        if (mainPanel == null)
        {
            mainPanel = gameObject;
        }

        RectTransform panelRect = mainPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            panelRect = mainPanel.AddComponent<RectTransform>();
        }

        StretchToParent(panelRect);

        Image panelImage = GetOrAdd<Image>(mainPanel);
        panelImage.color = overlayColor;
        panelImage.enabled = false;
        panelImage.raycastTarget = false;
        HideLegacyDialArtwork(panelRect);

        _solidSprite = CreateSolidSprite();
        _ringSprite = CreateRingSprite(RingSpriteSize, 0.79f, 0.93f, 0.035f);

        _dialRoot = FindOrCreateRect("SkillCheckDial_Root", panelRect);
        SetCenteredRect(_dialRoot, panelRect, new Vector2(dialSize, dialSize), Vector2.zero, new Vector2(0.5f, 0.5f));
        HideObsoleteZoneArtwork();

        Image dialBackdrop = FindOrCreateImage("Dial_Backdrop", _dialRoot);
        SetCenteredRect(dialBackdrop.rectTransform, _dialRoot, new Vector2(dialSize * 1.08f, dialSize * 1.08f), Vector2.zero, new Vector2(0.5f, 0.5f));
        dialBackdrop.sprite = _solidSprite;
        dialBackdrop.type = Image.Type.Simple;
        dialBackdrop.color = Color.clear;
        dialBackdrop.enabled = false;
        dialBackdrop.raycastTarget = false;

        _trackRing = FindOrCreateImage("Dial_Track", _dialRoot);
        ConfigureRing(_trackRing, dialSize, faintRingColor, 1f);

        _outerRing = FindOrCreateImage("Dial_OuterRing", _dialRoot);
        ConfigureSimpleRing(_outerRing, dialSize * 1.03f, ringColor);

        _innerRing = FindOrCreateImage("Dial_InnerRing", _dialRoot);
        ConfigureSimpleRing(_innerRing, dialSize * 0.86f, new Color(1f, 1f, 1f, 0.58f));

        if (successZoneFill == null)
        {
            successZoneFill = FindOrCreateImage("Dial_SuccessZone", _dialRoot);
        }
        ConfigureRing(successZoneFill, dialSize * 1.03f, goodResultColor, 0f);

        if (greatZoneFill == null)
        {
            greatZoneFill = FindOrCreateImage("Dial_GreatZone", _dialRoot);
        }
        ConfigureRing(greatZoneFill, dialSize * 1.03f, greatResultColor, 0f);

        if (perfectZoneFill == null)
        {
            perfectZoneFill = FindOrCreateImage("Dial_PerfectZone", _dialRoot);
        }
        ConfigureRing(perfectZoneFill, dialSize * 1.03f, perfectResultColor, 0f);

        if (needle == null)
        {
            _needleImage = FindOrCreateImage("Dial_Needle", _dialRoot);
            needle = _needleImage.rectTransform;
        }
        else
        {
            needle.SetParent(_dialRoot, false);
            _needleImage = GetOrAdd<Image>(needle.gameObject);
        }

        ConfigureNeedle();
        CreateCenterPrompt();
        ConfigureProgressText(panelRect);
        ConfigureFeedbackText(panelRect);
        ConfigureProgressPips(panelRect);

        _styleReady = true;
    }

    private void ConfigureRing(Image image, float size, Color color, float fillAmount)
    {
        image.transform.SetParent(_dialRoot, false);
        SetCenteredRect(image.rectTransform, _dialRoot, new Vector2(size, size), Vector2.zero, new Vector2(0.5f, 0.5f));
        image.sprite = _ringSprite;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = true;
        image.fillAmount = fillAmount;
        image.color = color;
        image.raycastTarget = false;
    }

    private void ConfigureSimpleRing(Image image, float size, Color color)
    {
        image.transform.SetParent(_dialRoot, false);
        SetCenteredRect(image.rectTransform, _dialRoot, new Vector2(size, size), Vector2.zero, new Vector2(0.5f, 0.5f));
        image.sprite = _ringSprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
    }

    private void ConfigureNeedle()
    {
        SetCenteredRect(needle, _dialRoot, new Vector2(7f, dialSize * 0.45f), Vector2.zero, new Vector2(0.5f, 0.04f));
        needle.SetAsLastSibling();

        _needleImage.sprite = _solidSprite;
        _needleImage.type = Image.Type.Simple;
        _needleImage.color = needleColor;
        _needleImage.raycastTarget = false;

        UnityEngine.UI.Shadow shadow = GetOrAdd<UnityEngine.UI.Shadow>(needle.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        ApplyNeedleRotation();
    }

    private void CreateCenterPrompt()
    {
        _centerBox = FindOrCreateImage("Center_SPACE_Box", _dialRoot);
        SetCenteredRect(_centerBox.rectTransform, _dialRoot, new Vector2(150f, 52f), Vector2.zero, new Vector2(0.5f, 0.5f));
        _centerBox.sprite = _solidSprite;
        _centerBox.type = Image.Type.Simple;
        _centerBox.color = new Color(0.04f, 0.045f, 0.055f, 0.82f);
        _centerBox.raycastTarget = false;
        _centerBox.transform.SetAsLastSibling();

        UnityEngine.UI.Outline outline = GetOrAdd<UnityEngine.UI.Outline>(_centerBox.gameObject);
        outline.enabled = false;

        _spaceText = FindOrCreateText("Center_SPACE_Text", _centerBox.rectTransform);
        SetCenteredRect(_spaceText.rectTransform, _centerBox.rectTransform, new Vector2(150f, 52f), Vector2.zero, new Vector2(0.5f, 0.5f));
        _spaceText.text = "SPACE";
        _spaceText.alignment = TextAlignmentOptions.Center;
        _spaceText.fontSize = 26f;
        _spaceText.fontStyle = FontStyles.Bold;
        _spaceText.color = Color.white;
        _spaceText.raycastTarget = false;
    }

    private void ConfigureProgressText(RectTransform panelRect)
    {
        if (progressText == null)
        {
            progressText = FindOrCreateText("ProgressText", panelRect);
        }

        progressText.transform.SetParent(panelRect, false);
        SetCenteredRect(progressText.rectTransform, panelRect, new Vector2(440f, 44f), new Vector2(0f, (dialSize * 0.5f) + 76f), new Vector2(0.5f, 0.5f));
        progressText.text = "MỐI HÀN 0/0";
        progressText.alignment = TextAlignmentOptions.Center;
        progressText.fontSize = 27f;
        progressText.fontStyle = FontStyles.Bold;
        progressText.color = new Color(1f, 1f, 1f, 0.92f);
        progressText.raycastTarget = false;
    }

    private void ConfigureFeedbackText(RectTransform panelRect)
    {
        if (feedbackText == null)
        {
            feedbackText = FindOrCreateText("FeedbackText", panelRect);
        }

        feedbackText.transform.SetParent(panelRect, false);
        SetCenteredRect(feedbackText.rectTransform, panelRect, new Vector2(440f, 58f), new Vector2(0f, -(dialSize * 0.5f) - 72f), new Vector2(0.5f, 0.5f));
        feedbackText.text = "";
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.fontSize = 34f;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.color = Color.white;
        feedbackText.raycastTarget = false;

        UnityEngine.UI.Shadow shadow = GetOrAdd<UnityEngine.UI.Shadow>(feedbackText.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private void ConfigureProgressPips(RectTransform panelRect)
    {
        _pipsRoot = FindOrCreateRect("SkillCheck_ProgressPips", panelRect);
        SetCenteredRect(_pipsRoot, panelRect, new Vector2(260f, 18f), new Vector2(0f, -(dialSize * 0.5f) - 26f), new Vector2(0.5f, 0.5f));
    }

    private void UpdateProgressPips(int current, int total)
    {
        if (_pipsRoot == null)
        {
            return;
        }

        while (_progressPips.Count < total)
        {
            Image pip = FindOrCreateImage("Pip_" + _progressPips.Count, _pipsRoot);
            pip.sprite = _solidSprite;
            pip.type = Image.Type.Simple;
            pip.raycastTarget = false;
            _progressPips.Add(pip);
        }

        float spacing = 24f;
        float startX = total > 0 ? -((total - 1) * spacing * 0.5f) : 0f;

        for (int i = 0; i < _progressPips.Count; i++)
        {
            Image pip = _progressPips[i];
            bool isVisible = i < total;
            pip.gameObject.SetActive(isVisible);

            if (!isVisible)
            {
                continue;
            }

            SetCenteredRect(pip.rectTransform, _pipsRoot, new Vector2(16f, 5f), new Vector2(startX + (i * spacing), 0f), new Vector2(0.5f, 0.5f));

            if (i < current - 1)
            {
                pip.color = new Color(0.08f, 0.62f, 1f, 0.9f);
            }
            else if (i == current - 1)
            {
                pip.color = Color.white;
            }
            else
            {
                pip.color = new Color(1f, 1f, 1f, 0.24f);
            }
        }
    }

    private void HideLegacyDialArtwork(RectTransform panelRect)
    {
        Transform legacyDial = panelRect.Find("Dial");
        if (legacyDial != null)
        {
            legacyDial.gameObject.SetActive(false);
        }
    }

    private void UpdateZoneVisuals()
    {
        HideObsoleteZoneArtwork();

        if (successZoneFill != null)
        {
            SetZoneArc(successZoneFill, _successStartAngle, _successSize, goodResultColor);
        }

        if (greatZoneFill != null)
        {
            SetZoneArc(greatZoneFill, _greatStartAngle, _greatSize, greatResultColor);
            greatZoneFill.transform.SetAsLastSibling();
        }

        if (perfectZoneFill != null)
        {
            SetZoneArc(perfectZoneFill, _perfectStartAngle, _perfectSize, perfectResultColor);
            perfectZoneFill.transform.SetAsLastSibling();
        }

        if (needle != null)
        {
            needle.SetAsLastSibling();
        }

        if (_centerBox != null)
        {
            _centerBox.transform.SetAsLastSibling();
        }
    }

    private void HideObsoleteZoneArtwork()
    {
        if (_dialRoot == null)
        {
            return;
        }

        Transform oldWarningZone = _dialRoot.Find("Dial_WarningZone");
        if (oldWarningZone != null)
        {
            oldWarningZone.gameObject.SetActive(false);
        }
    }

    private void SetZoneArc(Image zone, float startAngle, float angleSize, Color color)
    {
        zone.transform.SetParent(_dialRoot, false);
        SetCenteredRect(zone.rectTransform, _dialRoot, new Vector2(dialSize * 1.03f, dialSize * 1.03f), Vector2.zero, new Vector2(0.5f, 0.5f));
        zone.sprite = _ringSprite;
        zone.enabled = angleSize > 0.1f;
        zone.type = Image.Type.Filled;
        zone.fillMethod = Image.FillMethod.Radial360;
        zone.fillOrigin = (int)Image.Origin360.Top;
        zone.fillClockwise = true;
        zone.fillAmount = Mathf.Clamp01(angleSize / 360f);
        zone.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -startAngle);
        zone.color = color;
    }

    private void ApplyNeedleRotation()
    {
        if (needle != null)
        {
            needle.localRotation = Quaternion.Euler(0f, 0f, -_needleAngle);
        }
    }

    private bool IsAngleInsideWindow(float angle, float startAngle, float windowSize)
    {
        float delta = Mathf.Repeat(angle - startAngle, 360f);
        return delta <= windowSize;
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null)
        {
            return;
        }

        if (_feedbackRoutine != null)
        {
            StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = null;
        }

        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.rectTransform.localScale = Vector3.one;

        if (!string.IsNullOrEmpty(message) && gameObject.activeInHierarchy)
        {
            _feedbackRoutine = StartCoroutine(PulseFeedback());
        }
    }

    private IEnumerator PulseFeedback()
    {
        float elapsed = 0f;
        const float duration = 0.18f;
        Vector3 from = Vector3.one * 1.2f;
        Vector3 to = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            feedbackText.rectTransform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        feedbackText.rectTransform.localScale = Vector3.one;
        _feedbackRoutine = null;
    }

    private Image FindOrCreateImage(string objectName, Transform parent)
    {
        RectTransform rect = FindOrCreateRect(objectName, parent);
        return GetOrAdd<Image>(rect.gameObject);
    }

    private TextMeshProUGUI FindOrCreateText(string objectName, Transform parent)
    {
        RectTransform rect = FindOrCreateRect(objectName, parent);
        return GetOrAdd<TextMeshProUGUI>(rect.gameObject);
    }

    private RectTransform FindOrCreateRect(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            RectTransform existingRect = existing as RectTransform;
            if (existingRect != null)
            {
                return existingRect;
            }
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetCenteredRect(RectTransform rect, Transform parent, Vector2 size, Vector2 anchoredPosition, Vector2 pivot)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private Sprite CreateRingSprite(int size, float innerRadius, float outerRadius, float edgeSoftness)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy));

                float innerFade = Mathf.InverseLerp(innerRadius, innerRadius + edgeSoftness, distance);
                float outerFade = 1f - Mathf.InverseLerp(outerRadius - edgeSoftness, outerRadius, distance);
                float alpha = Mathf.Clamp01(Mathf.Min(innerFade, outerFade));

                texture.SetPixel(x, y, alpha > 0f ? new Color(white.r, white.g, white.b, alpha) : clear);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}

