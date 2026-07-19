using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContactCleaningMinigame : MonoBehaviour, IMinigame
{
    private enum CleaningTool
    {
        Ipa,
        Eraser,
        BrassBrush,
        Sandpaper
    }

    private enum ContactLayout
    {
        EdgePins,
        ButtonPads,
        PlugProngs,
        BatteryTerminals,
        FlatCable
    }

    private enum ContaminationType
    {
        OilFilm,
        LightOxide,
        HeavyOxide,
        BurntCarbon,
        TarnishedGold
    }

    private sealed class CleaningProfile
    {
        public string Title;
        public string BoardLabel;
        public string GuideText;
        public string ToolAdvice;
        public string ContactPrefix;
        public ContactLayout Layout;
        public int ContactCount;
        public int BasePatchCount;
        public Vector2 ContactSize;
        public float RequiredWorkScale;
        public float OxidationSizeScale;
        public float ScratchLimitScale;
        public float TimeBonus;
        public float IpaPower;
        public float EraserPower;
        public float BrushPower;
        public float SandpaperPower;
        public float IpaDamage;
        public float EraserDamage;
        public float BrushDamage;
        public float SandpaperDamage;
        public Color BoardColor;
        public Color BoardLightColor;
        public Color TraceA;
        public Color TraceB;
        public Color MetalMain;
        public Color MetalHighlight;
        public Color OxideA;
        public Color OxideB;
    }

    private sealed class PatchData
    {
        public string Id;
        public ContaminationType Type;
        public Vector2 Position;
        public Vector2 Size;
        public float RequiredWork;
        public float WorkDone;
        public bool WasLoosenedByIpa;
        public float ToolBlockedFeedbackAt;
        public Image Image;
        public TextMeshProUGUI Label;
        public bool IsClean => Progress >= 1f;
        public float Progress => RequiredWork <= 0f ? 1f : Mathf.Clamp01(WorkDone / RequiredWork);
    }

    private sealed class OxidationPatchView : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private ContactCleaningMinigame _owner;
        private PatchData _patch;

        public void Bind(ContactCleaningMinigame owner, PatchData patch)
        {
            _owner = owner;
            _patch = patch;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_owner != null && _patch != null)
            {
                _owner.ApplyScrub(_patch, 16f);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_owner != null && _patch != null)
            {
                float amount = Mathf.Clamp(eventData.delta.magnitude, 4f, 34f);
                _owner.ApplyScrub(_patch, amount);
            }
        }
    }

    public string MinigameName => "C\u1ea1o ti\u1ebfp \u0111i\u1ec3m oxy h\u00f3a";
    public bool IsActive { get; private set; }
    public event Action<RepairQuality> OnMinigameCompleted;

    [Header("Rules")]
    [SerializeField] private float baseTimeLimit = 80f;
    [SerializeField] private float maxScratchDamage = 100f;

    private readonly List<PatchData> _patches = new List<PatchData>();
    private readonly Dictionary<CleaningTool, Button> _toolButtons = new Dictionary<CleaningTool, Button>();

    private GameObject _uiRoot;
    private RectTransform _contactRect;
    private Transform _contactLayer;
    private Transform _patchLayer;
    private TextMeshProUGUI _timerText;
    private TextMeshProUGUI _progressText;
    private TextMeshProUGUI _feedbackText;
    private TextMeshProUGUI _toolHintText;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _boardLabelText;
    private TextMeshProUGUI _guideText;
    private Image _cleanBar;
    private Image _toolCursor;
    private Image _scratchBar;
    private Button _finishButton;

    private Sprite _solidSprite;
    private Sprite _panelSprite;
    private Sprite _patchSprite;
    private Sprite _contactBoardSprite;
    private Sprite _goldStripSprite;

    private CleaningTool _selectedTool = CleaningTool.Eraser;
    private CleaningProfile _profile;
    private string _contextItemName = "";
    private string _contextObjectName = "";
    private int _difficultyLevel = 1;
    private float _timeRemaining;
    private float _startedAt;
    private float _scratchDamage;
    private float _currentScratchLimit;
    private bool _isFinishing;
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    private void Awake()
    {
        EnsureUI();
        ShowUI(false);
    }

    private void Update()
    {
        if (!IsActive || _isFinishing)
        {
            if (_toolCursor != null)
            {
                _toolCursor.gameObject.SetActive(false);
            }
            return;
        }

        HandleToolShortcuts();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Complete(RepairQuality.Broken, true, "Đã hủy vệ sinh.");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) && _finishButton != null && _finishButton.interactable)
        {
            TryFinish();
            return;
        }

        UpdateToolCursor();

        // Repair minigames pause the world with timeScale = 0. The UI timer must
        // continue to run independently from the paused gameplay clock.
        _timeRemaining -= Time.unscaledDeltaTime;
        UpdateStatusText();

        if (_timeRemaining <= 0f)
        {
            Complete(RepairQuality.Broken, true, "Hết giờ, tiếp điểm vẫn còn oxy hóa.");
        }
    }

    private void HandleToolShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectTool(CleaningTool.Ipa);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectTool(CleaningTool.Eraser);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectTool(CleaningTool.BrassBrush);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectTool(CleaningTool.Sandpaper);

        if (Input.GetKeyDown(KeyCode.Q)) CycleTool(-1);
        if (Input.GetKeyDown(KeyCode.E)) CycleTool(1);

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.2f) CycleTool(-1);
        if (scroll < -0.2f) CycleTool(1);
    }

    private void CycleTool(int direction)
    {
        int toolCount = Enum.GetValues(typeof(CleaningTool)).Length;
        int next = ((int)_selectedTool + direction + toolCount) % toolCount;
        SelectTool((CleaningTool)next);
        ShowFeedback("Đổi sang " + GetToolDisplayName(_selectedTool) + ".", new Color(0.72f, 0.9f, 1f, 1f));
    }

    private void UpdateToolCursor()
    {
        if (_toolCursor == null || _uiRoot == null || _contactRect == null)
        {
            return;
        }

        bool pointerOverContact = RectTransformUtility.RectangleContainsScreenPoint(_contactRect, Input.mousePosition, null);
        _toolCursor.gameObject.SetActive(pointerOverContact);
        if (!pointerOverContact)
        {
            return;
        }

        RectTransform rootRect = _uiRoot.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, Input.mousePosition, null, out Vector2 localPoint))
        {
            _toolCursor.rectTransform.anchoredPosition = localPoint + new Vector2(30f, -30f);
            _toolCursor.transform.SetAsLastSibling();
        }
    }


    public void Initialize(List<string> faults, int difficultyLevel)
    {
        EnsureUI();

        _difficultyLevel = Mathf.Clamp(difficultyLevel, 1, 5);
        _profile = PickProfile(faults);
        _currentScratchLimit = Mathf.Max(30f, maxScratchDamage * _profile.ScratchLimitScale);
        _timeRemaining = Mathf.Max(42f, baseTimeLimit + _profile.TimeBonus - ((_difficultyLevel - 1) * 7f));
        _scratchDamage = 0f;
        _isFinishing = false;
        _selectedTool = CleaningTool.Eraser;

        BuildPatches();
        RebuildContactBoard();
        SelectTool(_selectedTool);
        ShowFeedback("Đọc nhãn vết bẩn rồi đổi dụng cụ: cồn không xử lý hết rỉ nặng hoặc mảng cháy.", new Color(0.78f, 0.9f, 1f, 1f));
        UpdateStatusText();
    }

    public void SetRepairContext(string itemName, string objectName)
    {
        _contextItemName = itemName ?? "";
        _contextObjectName = objectName ?? "";
    }

    public void StartMinigame()
    {
        EnsureUI();
        if (_patches.Count == 0)
        {
            Initialize(null, _difficultyLevel);
        }

        _isFinishing = false;
        IsActive = true;
        _startedAt = Time.unscaledTime;

        _previousLockMode = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowUI(true);
        MinigameSfxKit.Play(MinigameSfxCue.Open, 0.58f);
        UpdateStatusText();

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowSubtitle("Anh Thợ Điện", "Vệ sinh tiếp điểm bị oxy hóa. Dùng đúng dụng cụ để không làm trầy lớp mạ.", 3.5f, "Tiếng đặt đồ");
        }
    }

    public RepairQuality EndMinigame()
    {
        if (!IsActive)
        {
            return RepairQuality.Broken;
        }

        RepairQuality quality = EvaluateQuality();
        Complete(quality, false, GetResultText(quality));
        return quality;
    }

    public void AbortMinigame()
    {
        Complete(RepairQuality.Broken, false, "H\u1ee7y v\u1ec7 sinh ti\u1ebfp \u0111i\u1ec3m.");
    }

    private void ApplyScrub(PatchData patch, float rawAmount)
    {
        if (!IsActive || _isFinishing || patch == null)
        {
            return;
        }

        float effectiveness = GetToolEffectiveness(_selectedTool, patch);
        float progressCap = GetToolProgressCap(_selectedTool, patch);
        float damage = GetToolDamage(_selectedTool, patch);
        bool wasClean = patch.IsClean;

        if (!patch.IsClean)
        {
            if (_selectedTool == CleaningTool.Ipa && CanIpaLoosen(patch))
            {
                patch.WasLoosenedByIpa = true;
                patch.RequiredWork *= 0.9f;
                ShowFeedback(GetIpaLoosenFeedback(patch.Type), new Color(0.58f, 0.86f, 1f, 1f));
            }

            if (effectiveness > 0.03f && patch.Progress < progressCap - 0.002f)
            {
                float cappedWork = patch.RequiredWork * progressCap;
                patch.WorkDone = Mathf.Min(cappedWork, patch.WorkDone + (rawAmount * effectiveness));
            }
            else
            {
                ShowBlockedToolFeedback(_selectedTool, patch);
            }
        }

        if (_selectedTool == CleaningTool.Sandpaper || (wasClean && damage > 0f))
        {
            _scratchDamage += rawAmount * damage * 0.08f;
        }
        else if (_selectedTool == CleaningTool.BrassBrush && patch.Progress > 0.88f)
        {
            _scratchDamage += rawAmount * damage * 0.035f;
        }
        else if (effectiveness <= 0.05f && damage > 0.25f)
        {
            _scratchDamage += rawAmount * damage * 0.018f;
        }

        _scratchDamage = Mathf.Clamp(_scratchDamage, 0f, _currentScratchLimit + 20f);
        MinigameSfxKit.Play(_selectedTool == CleaningTool.Sandpaper || _selectedTool == CleaningTool.BrassBrush ? MinigameSfxCue.ScrubRough : MinigameSfxCue.ScrubSoft, Mathf.Clamp01(0.38f + (rawAmount * 0.012f)));
        UpdatePatchVisual(patch);
        UpdateStatusText();

        if (_scratchDamage >= _currentScratchLimit)
        {
            Complete(RepairQuality.Broken, true, "Cào quá tay, tiếp điểm bị trầy nặng.");
            return;
        }

        if (AllPatchesClean())
        {
            Complete(EvaluateQuality(), true, GetResultText(EvaluateQuality()));
        }
    }

    private void EnsureUI()
    {
        if (_uiRoot != null)
        {
            return;
        }

        _solidSprite = MinigameUiKit.CreateSolidSprite(Color.white);
        _panelSprite = MinigameUiKit.CreateRoundedRectSprite(128, 128, 18, Color.white, new Color(1f, 1f, 1f, 0.18f));
        _profile = CreateGenericProfile();
        _contactBoardSprite = CreateContactBoardSprite(_profile);
        _goldStripSprite = CreateGoldStripSprite(_profile);
        _patchSprite = CreateOxidationSprite(_profile);

        _uiRoot = MinigameUiKit.CreateCanvasRoot("ContactCleaningUI", transform, 515);
        MinigameWorkbenchVisuals.Install(_uiRoot, MinigameWorkbenchStyle.Cleaning, new Color(0.92f, 0.68f, 0.22f, 1f));


        Image overlay = MinigameUiKit.CreateImage(_uiRoot.transform, "BackgroundOverlay", _solidSprite, new Color(0.006f, 0.008f, 0.012f, 0.92f), false);
        MinigameUiKit.Stretch(overlay.rectTransform);
        overlay.transform.SetAsFirstSibling();

        Image header = MinigameUiKit.CreatePanel(_uiRoot.transform, "Header", _panelSprite, new Color(0.035f, 0.04f, 0.046f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(1680f, 72f));
        MinigameUiKit.AddChrome(header.transform, _solidSprite, new Color(0.92f, 0.68f, 0.22f, 0.95f));
        _titleText = MinigameUiKit.CreateText(header.transform, "Title", "VỆ SINH TIẾP ĐIỂM OXY HÓA", 28, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.95f, 0.98f, 1f, 1f));
        // SetAnchored uses a centered pivot. Offset by half the text width so
        // left/right anchored labels remain inside the header safe area.
        MinigameUiKit.SetAnchored(_titleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(412f, 0f), new Vector2(760f, 48f));

        _timerText = MinigameUiKit.CreateText(header.transform, "Timer", "", 23, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.72f, 0.92f, 1f, 1f));
        MinigameUiKit.SetAnchored(_timerText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-292f, 0f), new Vector2(520f, 48f));
        _progressText = MinigameUiKit.CreateText(header.transform, "Progress", "", 23, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.32f, 1f));
        MinigameUiKit.SetAnchored(_progressText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 48f));

        Image contactPanel = MinigameUiKit.CreatePanel(_uiRoot.transform, "ContactPanel", _panelSprite, new Color(0.02f, 0.025f, 0.029f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, -18f), new Vector2(1080f, 820f));
        MinigameUiKit.AddChrome(contactPanel.transform, _solidSprite, new Color(0.92f, 0.68f, 0.22f, 0.5f));
        _contactRect = contactPanel.rectTransform;
        _contactLayer = MinigameUiKit.CreateUIObject("ContactLayer", contactPanel.transform).transform;
        MinigameUiKit.Stretch(_contactLayer.GetComponent<RectTransform>());
        _patchLayer = MinigameUiKit.CreateUIObject("PatchLayer", contactPanel.transform).transform;
        MinigameUiKit.Stretch(_patchLayer.GetComponent<RectTransform>());

        _boardLabelText = MinigameUiKit.CreateText(contactPanel.transform, "BoardLabel", "CỤM CHÂN TIẾP XÚC", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.78f, 0.9f, 0.9f, 1f));
        MinigameUiKit.SetAnchored(_boardLabelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(620f, 42f));

        Image guidePanel = MinigameUiKit.CreatePanel(contactPanel.transform, "GuidePanel", _panelSprite, new Color(0.032f, 0.045f, 0.05f, 0.94f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(900f, 62f));
        MinigameUiKit.AddChrome(guidePanel.transform, _solidSprite, new Color(0.92f, 0.68f, 0.22f, 0.45f));
        _guideText = MinigameUiKit.CreateText(guidePanel.transform, "GuideText", "Hướng dẫn: chọn dụng cụ, giữ chuột và kéo trên vết oxy hóa. Dụng cụ mạnh sạch nhanh nhưng dễ làm trầy tiếp điểm.", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.84f, 0.94f, 0.92f, 1f));
        MinigameUiKit.Stretch(_guideText.rectTransform);

        Image toolPanel = MinigameUiKit.CreatePanel(_uiRoot.transform, "ToolPanel", _panelSprite, new Color(0.025f, 0.029f, 0.036f, 0.98f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-295f, -18f), new Vector2(500f, 820f));
        MinigameUiKit.AddChrome(toolPanel.transform, _solidSprite, new Color(0.92f, 0.68f, 0.22f, 0.5f));

        _toolCursor = MinigameUiKit.CreateImage(_uiRoot.transform, "CleaningToolCursor", CreateToolIconSprite(_selectedTool), Color.white, false);
        MinigameUiKit.SetAnchored(_toolCursor.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
        _toolCursor.raycastTarget = false;
        _toolCursor.gameObject.SetActive(false);
        BuildToolPanel(toolPanel.transform);

        _feedbackText = MinigameUiKit.CreateText(_uiRoot.transform, "Feedback", "", 25, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        MinigameUiKit.SetAnchored(_feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 42f), new Vector2(1240f, 48f));

        _finishButton = MinigameUiKit.CreateButton(_uiRoot.transform, "FinishButton", "KI\u1ec2M TRA", _panelSprite, new Color(0.12f, 0.42f, 0.28f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 58f), new Vector2(220f, 56f), TryFinish);
        MinigameUiKit.CreateButton(_uiRoot.transform, "CancelButton", "H\u1ee6Y", _panelSprite, new Color(0.35f, 0.09f, 0.08f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(130f, 58f), new Vector2(170f, 56f), () => Complete(RepairQuality.Broken, true, "\u0110\u00e3 h\u1ee7y v\u1ec7 sinh."));
    }

    private void BuildToolPanel(Transform parent)
    {
        TextMeshProUGUI title = MinigameUiKit.CreateText(parent, "ToolTitle", "D\u1ee4NG C\u1ee4", 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.98f, 1f, 1f));
        MinigameUiKit.SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(420f, 44f));

        AddToolButton(parent, CleaningTool.Ipa, "C\u1ed2N IPA", new Vector2(0f, -126f), new Color(0.08f, 0.34f, 0.5f, 1f));
        AddToolButton(parent, CleaningTool.Eraser, "G\u00d4M TI\u1ebeP \u0110I\u1ec2M", new Vector2(0f, -198f), new Color(0.34f, 0.29f, 0.16f, 1f));
        AddToolButton(parent, CleaningTool.BrassBrush, "B\u00c0N CH\u1ea2I \u0110\u1ed2NG", new Vector2(0f, -270f), new Color(0.42f, 0.28f, 0.08f, 1f));
        AddToolButton(parent, CleaningTool.Sandpaper, "GI\u1ea4Y NH\u00c1M", new Vector2(0f, -342f), new Color(0.42f, 0.14f, 0.08f, 1f));

        _toolHintText = MinigameUiKit.CreateText(parent, "ToolHint", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.8f, 0.9f, 0.92f, 1f));
        MinigameUiKit.SetAnchored(_toolHintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -445f), new Vector2(420f, 110f));

        TextMeshProUGUI cleanLabel = MinigameUiKit.CreateText(parent, "CleanLabel", "M\u1ee9c s\u1ea1ch", 18, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.92f, 0.96f, 1f, 1f));
        MinigameUiKit.SetAnchored(cleanLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -575f), new Vector2(420f, 30f));
        Image cleanBg = MinigameUiKit.CreatePanel(parent, "CleanBarBg", _panelSprite, new Color(0.06f, 0.07f, 0.08f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -620f), new Vector2(420f, 34f));
        _cleanBar = MinigameUiKit.CreateImage(cleanBg.transform, "Fill", _solidSprite, new Color(0.32f, 0.95f, 0.56f, 1f), false);
        MinigameUiKit.SetAnchored(_cleanBar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 34f));
        _cleanBar.rectTransform.pivot = new Vector2(0f, 0.5f);

        TextMeshProUGUI scratchLabel = MinigameUiKit.CreateText(parent, "ScratchLabel", "Tr\u1ea7y x\u01b0\u1edbc", 18, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.92f, 0.96f, 1f, 1f));
        MinigameUiKit.SetAnchored(scratchLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -675f), new Vector2(420f, 30f));
        Image scratchBg = MinigameUiKit.CreatePanel(parent, "ScratchBarBg", _panelSprite, new Color(0.06f, 0.07f, 0.08f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -720f), new Vector2(420f, 34f));
        _scratchBar = MinigameUiKit.CreateImage(scratchBg.transform, "Fill", _solidSprite, new Color(1f, 0.3f, 0.2f, 1f), false);
        MinigameUiKit.SetAnchored(_scratchBar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 34f));
        _scratchBar.rectTransform.pivot = new Vector2(0f, 0.5f);
    }

    private void AddToolButton(Transform parent, CleaningTool tool, string label, Vector2 position, Color color)
    {
        Button button = MinigameUiKit.CreateButton(parent, "Tool_" + tool, label, _panelSprite, color, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(420f, 54f), () => SelectTool(tool));
        Image icon = MinigameUiKit.CreateImage(button.transform, "Icon", CreateToolIconSprite(tool), Color.white, false);
        MinigameUiKit.SetAnchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(40f, 40f));

        Transform labelTransform = button.transform.Find("Label");
        if (labelTransform != null && labelTransform.TryGetComponent(out TextMeshProUGUI text))
        {
            MinigameUiKit.SetAnchored(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(36f, 0f), new Vector2(-70f, 48f));
            text.alignment = TextAlignmentOptions.Center;
        }

        _toolButtons[tool] = button;
    }

    private void BuildPatches()
    {
        _patches.Clear();
        if (_profile == null)
        {
            _profile = CreateGenericProfile();
        }

        List<Vector2> positions = GetPatchPositions(_profile.Layout);
        Shuffle(positions);
        int count = Mathf.Clamp(_profile.BasePatchCount + _difficultyLevel, 2, positions.Count);

        for (int i = 0; i < count; i++)
        {
            ContaminationType contamination = PickContaminationType(i);
            float size = (UnityEngine.Random.Range(78f, 126f) + (_difficultyLevel * 5f)) * _profile.OxidationSizeScale;
            _patches.Add(new PatchData
            {
                Id = "OX" + (i + 1),
                Type = contamination,
                Position = positions[i],
                Size = new Vector2(size, size * UnityEngine.Random.Range(0.72f, 1.08f)),
                RequiredWork = (UnityEngine.Random.Range(82f, 122f) + (_difficultyLevel * 18f)) * _profile.RequiredWorkScale * GetContaminationWorkScale(contamination),
                WorkDone = 0f
            });
        }
    }

    private void RebuildContactBoard()
    {
        ClearChildren(_contactLayer);
        ClearChildren(_patchLayer);
        if (_profile == null)
        {
            _profile = CreateGenericProfile();
        }

        _contactBoardSprite = CreateContactBoardSprite(_profile);
        _goldStripSprite = CreateGoldStripSprite(_profile);
        _patchSprite = CreateOxidationSprite(_profile);
        ApplyProfileText();

        Image board = MinigameUiKit.CreateImage(_contactLayer, "BoardBase", _contactBoardSprite, Color.white, false);
        MinigameUiKit.SetAnchored(board.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(900f, 590f));
        board.raycastTarget = false;

        BuildContactVisuals();

        for (int i = 0; i < _patches.Count; i++)
        {
            PatchData patch = _patches[i];
            Image image = MinigameUiKit.CreateImage(_patchLayer, "Patch_" + patch.Id, _patchSprite, GetContaminationColor(patch.Type));
            MinigameUiKit.SetAnchored(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), patch.Position, patch.Size);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-18f, 18f));
            patch.Image = image;

            OxidationPatchView view = image.gameObject.AddComponent<OxidationPatchView>();
            view.Bind(this, patch);

            patch.Label = MinigameUiKit.CreateText(_patchLayer, "Label_" + patch.Id, GetContaminationShortName(patch.Type), 13, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.98f, 0.9f, 0.66f, 1f));
            MinigameUiKit.SetAnchored(patch.Label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), patch.Position, new Vector2(118f, 28f));
            patch.Label.raycastTarget = false;
            UpdatePatchVisual(patch);
        }
    }

    private void ApplyProfileText()
    {
        if (_profile == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = _profile.Title;
        }

        if (_boardLabelText != null)
        {
            _boardLabelText.text = _profile.BoardLabel;
        }

        if (_guideText != null)
        {
            _guideText.text = _profile.GuideText;
        }
    }

    private void BuildContactVisuals()
    {
        switch (_profile.Layout)
        {
            case ContactLayout.ButtonPads:
                BuildButtonPadContacts();
                break;
            case ContactLayout.PlugProngs:
                BuildPlugProngContacts();
                break;
            case ContactLayout.BatteryTerminals:
                BuildBatteryTerminalContacts();
                break;
            case ContactLayout.FlatCable:
                BuildFlatCableContacts();
                break;
            default:
                BuildEdgePinContacts();
                break;
        }
    }

    private void BuildEdgePinContacts()
    {
        int count = Mathf.Max(2, _profile.ContactCount);
        float spacing = count <= 1 ? 0f : 700f / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float x = -350f + (i * spacing);
            CreateTracePair(i, x, 250f, -250f);
            CreateContact(i, new Vector2(x, 0f), _profile.ContactSize, $"{_profile.ContactPrefix} {i + 1}", new Vector2(x, -268f), 0f);
        }
    }

    private void BuildFlatCableContacts()
    {
        int count = Mathf.Max(6, _profile.ContactCount);
        float spacing = count <= 1 ? 0f : 640f / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float x = -320f + (i * spacing);
            CreateTracePair(i, x, 235f, -235f);
            CreateContact(i, new Vector2(x, -6f), _profile.ContactSize, $"{_profile.ContactPrefix} {i + 1}", new Vector2(x, -268f), 0f);
        }
    }

    private void BuildButtonPadContacts()
    {
        int count = Mathf.Max(4, _profile.ContactCount);
        int columns = Mathf.CeilToInt(count / 2f);
        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            float x = -((columns - 1) * 120f * 0.5f) + (column * 120f);
            float y = row == 0 ? 92f : -112f;
            CreateTracePair(i, x, y + 130f, y - 130f);
            CreateContact(i, new Vector2(x, y), _profile.ContactSize, $"{_profile.ContactPrefix} {i + 1}", new Vector2(x, y - 78f), UnityEngine.Random.Range(-4f, 5f));
        }
    }

    private void BuildPlugProngContacts()
    {
        string[] labels = { "L", "N", "E" };
        int count = Mathf.Clamp(_profile.ContactCount, 2, 3);
        float startX = count == 2 ? -120f : -220f;
        float spacing = count == 2 ? 240f : 220f;
        for (int i = 0; i < count; i++)
        {
            float x = startX + (i * spacing);
            CreateTracePair(i, x, 260f, -260f);
            CreateContact(i, new Vector2(x, -8f), _profile.ContactSize, labels[i], new Vector2(x, -282f), i == 2 ? 0f : UnityEngine.Random.Range(-2f, 3f));
        }
    }

    private void BuildBatteryTerminalContacts()
    {
        CreateLargeTerminal(0, new Vector2(-210f, 0f), "+", new Color(0.26f, 0.85f, 0.5f, 0.8f));
        CreateLargeTerminal(1, new Vector2(210f, 0f), "-", new Color(0.55f, 0.72f, 1f, 0.75f));
    }

    private void CreateLargeTerminal(int index, Vector2 position, string label, Color traceColor)
    {
        Image upperTrace = MinigameUiKit.CreateLine(_contactLayer, "TerminalTraceA_" + index, _solidSprite, traceColor, position + new Vector2(0f, 130f), position + new Vector2(index == 0 ? -210f : 210f, 240f), 14f);
        upperTrace.raycastTarget = false;
        Image lowerTrace = MinigameUiKit.CreateLine(_contactLayer, "TerminalTraceB_" + index, _solidSprite, traceColor * new Color(1f, 1f, 1f, 0.75f), position + new Vector2(0f, -130f), position + new Vector2(index == 0 ? -210f : 210f, -240f), 12f);
        lowerTrace.raycastTarget = false;
        CreateContact(index, position, _profile.ContactSize, label, position + new Vector2(0f, -146f), 0f);
    }

    private void CreateTracePair(int index, float x, float topY, float bottomY)
    {
        Color traceColor = index % 2 == 0 ? _profile.TraceA : _profile.TraceB;
        Image upperTrace = MinigameUiKit.CreateLine(_contactLayer, "UpperTrace_" + index, _solidSprite, traceColor, new Vector2(x, topY), new Vector2(x + (index % 2 == 0 ? -58f : 58f), Mathf.Lerp(topY, 0f, 0.42f)), 8f);
        upperTrace.raycastTarget = false;
        Image lowerTrace = MinigameUiKit.CreateLine(_contactLayer, "LowerTrace_" + index, _solidSprite, traceColor * new Color(1f, 1f, 1f, 0.82f), new Vector2(x, bottomY), new Vector2(x + (index % 2 == 0 ? 46f : -46f), Mathf.Lerp(bottomY, 0f, 0.42f)), 7f);
        lowerTrace.raycastTarget = false;
    }

    private void CreateContact(int index, Vector2 position, Vector2 size, string label, Vector2 labelPosition, float rotation)
    {
        Image strip = MinigameUiKit.CreateImage(_contactLayer, "Contact_" + index, _goldStripSprite, Color.white, false);
        MinigameUiKit.SetAnchored(strip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        strip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        strip.raycastTarget = false;

        TextMeshProUGUI pinLabel = MinigameUiKit.CreateText(_contactLayer, "ContactLabel_" + index, label, 12, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.7f, 0.92f, 0.84f, 0.85f));
        MinigameUiKit.SetAnchored(pinLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), labelPosition, new Vector2(92f, 24f));
        pinLabel.raycastTarget = false;
    }

    private void SelectTool(CleaningTool tool)
    {
        _selectedTool = tool;
        if (IsActive)
        {
            MinigameSfxKit.Play(MinigameSfxCue.Select, 0.42f);
        }

        foreach (KeyValuePair<CleaningTool, Button> pair in _toolButtons)
        {
            Image image = pair.Value.GetComponent<Image>();
            if (image != null)
            {
                Color buttonColor = pair.Key == tool ? Color.Lerp(GetToolColor(pair.Key), Color.white, 0.25f) : GetToolColor(pair.Key);
                image.color = buttonColor;
                MinigameUiKit.ConfigureButtonColors(pair.Value, buttonColor);
            }
        }


        if (_toolCursor != null)
        {
            _toolCursor.sprite = CreateToolIconSprite(tool);
            _toolCursor.color = Color.white;
        }
        if (_toolHintText != null)
        {
            _toolHintText.text = GetToolHint(tool);
        }
    }

    private void TryFinish()
    {
        if (!IsActive || _isFinishing)
        {
            return;
        }

        RepairQuality quality = EvaluateQuality();
        Complete(quality, true, GetResultText(quality));
    }

    private void UpdatePatchVisual(PatchData patch)
    {
        if (patch.Image != null)
        {
            float alpha = patch.Progress >= 1f ? 0.05f : Mathf.Lerp(0.98f, 0.16f, patch.Progress);
            Color tint = GetContaminationColor(patch.Type);
            patch.Image.color = new Color(tint.r, tint.g, tint.b, alpha);
            patch.Image.raycastTarget = true;
        }

        if (patch.Label != null)
        {
            patch.Label.text = patch.Progress >= 1f ? "S\u1ea0CH" : GetContaminationShortName(patch.Type) + " " + Mathf.RoundToInt(patch.Progress * 100f) + "%";
            patch.Label.color = patch.Progress >= 1f ? new Color(0.38f, 1f, 0.58f, 1f) : GetContaminationLabelColor(patch.Type);
        }
    }

    private void UpdateStatusText()
    {
        float cleanRatio = GetCleanRatio();
        if (_progressText != null)
        {
            _progressText.text = $"S\u1ea1ch {Mathf.RoundToInt(cleanRatio * 100f)}%";
        }

        if (_timerText != null)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(_timeRemaining));
            _timerText.text = $"Th\u1eddi gian {seconds / 60:00}:{seconds % 60:00}  |  Tr\u1ea7y {Mathf.RoundToInt(_scratchDamage)}%";
        }

        if (_cleanBar != null)
        {
            _cleanBar.rectTransform.sizeDelta = new Vector2(420f * cleanRatio, 34f);
        }

        if (_scratchBar != null)
        {
            _scratchBar.rectTransform.sizeDelta = new Vector2(420f * Mathf.Clamp01(_scratchDamage / Mathf.Max(1f, _currentScratchLimit)), 34f);
        }

        if (_finishButton != null)
        {
            _finishButton.interactable = cleanRatio >= 0.72f || AllPatchesClean();
        }
    }

    private float GetCleanRatio()
    {
        if (_patches.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < _patches.Count; i++)
        {
            total += _patches[i].Progress;
        }

        return Mathf.Clamp01(total / _patches.Count);
    }

    private bool AllPatchesClean()
    {
        for (int i = 0; i < _patches.Count; i++)
        {
            if (!_patches[i].IsClean)
            {
                return false;
            }
        }

        return _patches.Count > 0;
    }

    private RepairQuality EvaluateQuality()
    {
        float cleanRatio = GetCleanRatio();
        float elapsed = Mathf.Max(0f, Time.unscaledTime - _startedAt);
        float score = cleanRatio * 100f;
        score -= _scratchDamage * 0.82f;
        score -= Mathf.Max(0f, elapsed - 42f) * 0.22f;

        if (cleanRatio < 0.72f || _scratchDamage >= _currentScratchLimit)
        {
            return RepairQuality.Broken;
        }

        if (score >= 92f && cleanRatio >= 0.99f && _scratchDamage < 8f)
        {
            return RepairQuality.Perfect;
        }

        if (score >= 72f && cleanRatio >= 0.9f)
        {
            return RepairQuality.Good;
        }

        if (score >= 48f)
        {
            return RepairQuality.Passable;
        }

        return RepairQuality.Broken;
    }

    private void Complete(RepairQuality quality, bool delay, string feedback)
    {
        if (_isFinishing)
        {
            return;
        }

        _isFinishing = true;
        IsActive = false;
        MinigameSfxKit.Play(quality == RepairQuality.Broken ? MinigameSfxCue.Failure : MinigameSfxCue.Success, 0.78f);
        ShowFeedback(feedback, GetResultColor(quality));

        if (delay)
        {
            StartCoroutine(CompleteAfterDelay(quality));
        }
        else
        {
            FinishNow(quality);
        }
    }

    private IEnumerator CompleteAfterDelay(RepairQuality quality)
    {
        // Gameplay is deliberately paused while this UI is open, so a scaled
        // wait would never finish and would leave the minigame stuck onscreen.
        yield return new WaitForSecondsRealtime(0.75f);
        FinishNow(quality);
    }

    private void FinishNow(RepairQuality quality)
    {
        ShowUI(false);
        RestoreCursor();
        _isFinishing = false;
        OnMinigameCompleted?.Invoke(quality);
    }

    private void ShowFeedback(string message, Color color)
    {
        if (_feedbackText == null)
        {
            return;
        }

        _feedbackText.text = message;
        _feedbackText.color = color;
    }

    private float GetToolEffectiveness(CleaningTool tool, PatchData patch)
    {
        if (patch == null)
        {
            return GetToolPower(tool);
        }

        float multiplier = 1f;
        switch (patch.Type)
        {
            case ContaminationType.OilFilm:
                switch (tool)
                {
                    case CleaningTool.Ipa: multiplier = 1.75f; break;
                    case CleaningTool.Eraser: multiplier = 0.45f; break;
                    case CleaningTool.BrassBrush: multiplier = 0.22f; break;
                    case CleaningTool.Sandpaper: multiplier = 0.12f; break;
                }
                break;
            case ContaminationType.LightOxide:
                switch (tool)
                {
                    case CleaningTool.Ipa: multiplier = 0.52f; break;
                    case CleaningTool.Eraser: multiplier = 1.48f; break;
                    case CleaningTool.BrassBrush: multiplier = 1.08f; break;
                    case CleaningTool.Sandpaper: multiplier = 1.12f; break;
                }
                break;
            case ContaminationType.HeavyOxide:
                switch (tool)
                {
                    case CleaningTool.Ipa: multiplier = patch.WasLoosenedByIpa ? 0.26f : 0.18f; break;
                    case CleaningTool.Eraser: multiplier = 0.62f; break;
                    case CleaningTool.BrassBrush: multiplier = 1.68f; break;
                    case CleaningTool.Sandpaper: multiplier = 1.28f; break;
                }
                break;
            case ContaminationType.BurntCarbon:
                switch (tool)
                {
                    case CleaningTool.Ipa: multiplier = patch.WasLoosenedByIpa ? 0.16f : 0.1f; break;
                    case CleaningTool.Eraser: multiplier = 0.34f; break;
                    case CleaningTool.BrassBrush: multiplier = 1.08f; break;
                    case CleaningTool.Sandpaper: multiplier = 1.78f; break;
                }
                break;
            case ContaminationType.TarnishedGold:
                switch (tool)
                {
                    case CleaningTool.Ipa: multiplier = 0.88f; break;
                    case CleaningTool.Eraser: multiplier = 1.34f; break;
                    case CleaningTool.BrassBrush: multiplier = 0.66f; break;
                    case CleaningTool.Sandpaper: multiplier = 0.24f; break;
                }
                break;
        }

        return GetToolPower(tool) * multiplier;
    }

    private float GetToolProgressCap(CleaningTool tool, PatchData patch)
    {
        if (patch == null)
        {
            return 1f;
        }

        switch (patch.Type)
        {
            case ContaminationType.OilFilm:
                switch (tool)
                {
                    case CleaningTool.Ipa: return 1f;
                    case CleaningTool.Eraser: return 0.76f;
                    case CleaningTool.BrassBrush: return 0.45f;
                    case CleaningTool.Sandpaper: return 0.28f;
                }
                break;
            case ContaminationType.LightOxide:
                switch (tool)
                {
                    case CleaningTool.Ipa: return 0.72f;
                    case CleaningTool.Eraser: return 1f;
                    case CleaningTool.BrassBrush: return 1f;
                    case CleaningTool.Sandpaper: return 1f;
                }
                break;
            case ContaminationType.HeavyOxide:
                switch (tool)
                {
                    case CleaningTool.Ipa: return patch.WasLoosenedByIpa ? 0.56f : 0.42f;
                    case CleaningTool.Eraser: return 0.78f;
                    case CleaningTool.BrassBrush: return 1f;
                    case CleaningTool.Sandpaper: return 1f;
                }
                break;
            case ContaminationType.BurntCarbon:
                switch (tool)
                {
                    case CleaningTool.Ipa: return patch.WasLoosenedByIpa ? 0.34f : 0.26f;
                    case CleaningTool.Eraser: return 0.54f;
                    case CleaningTool.BrassBrush: return 0.9f;
                    case CleaningTool.Sandpaper: return 1f;
                }
                break;
            case ContaminationType.TarnishedGold:
                switch (tool)
                {
                    case CleaningTool.Ipa: return 0.94f;
                    case CleaningTool.Eraser: return 1f;
                    case CleaningTool.BrassBrush: return 0.82f;
                    case CleaningTool.Sandpaper: return 0.54f;
                }
                break;
        }

        return 1f;
    }

    private bool CanIpaLoosen(PatchData patch)
    {
        if (patch == null || patch.WasLoosenedByIpa)
        {
            return false;
        }

        return patch.Type == ContaminationType.HeavyOxide || patch.Type == ContaminationType.BurntCarbon;
    }

    private string GetIpaLoosenFeedback(ContaminationType type)
    {
        switch (type)
        {
            case ContaminationType.BurntCarbon:
                return "Cồn chỉ làm mềm bụi cháy. Cần giấy nhám nhẹ để phá mảng cháy đen.";
            case ContaminationType.HeavyOxide:
                return "Cồn đã làm ẩm lớp rỉ, nhưng phải dùng bàn chải đồng mới sạch hẳn.";
            default:
                return "Cồn chỉ hỗ trợ làm mềm, đổi dụng cụ để xử lý phần còn lại.";
        }
    }

    private void ShowBlockedToolFeedback(CleaningTool tool, PatchData patch)
    {
        if (patch == null || Time.time < patch.ToolBlockedFeedbackAt)
        {
            return;
        }

        patch.ToolBlockedFeedbackAt = Time.time + 0.75f;
        ShowFeedback(GetBlockedToolFeedback(tool, patch.Type), new Color(1f, 0.72f, 0.28f, 1f));
        MinigameSfxKit.Play(MinigameSfxCue.Error, 0.34f);
    }

    private string GetBlockedToolFeedback(CleaningTool tool, ContaminationType type)
    {
        if (tool == CleaningTool.Ipa)
        {
            switch (type)
            {
                case ContaminationType.LightOxide:
                    return "Cồn không bóc hết màng oxy hóa. Dùng gôm tiếp điểm để lấy lớp mờ.";
                case ContaminationType.HeavyOxide:
                    return "Lớp rỉ đã kẹt lại. Đổi sang bàn chải đồng, rồi chốt bằng cồn.";
                case ContaminationType.BurntCarbon:
                    return "Mảng cháy không tan bằng cồn. Dùng giấy nhám nhẹ trước.";
                case ContaminationType.TarnishedGold:
                    return "Cồn gần đủ rồi, nhưng lớp xỉ mạ cần gôm tiếp điểm để sáng hẳn.";
            }
        }

        switch (type)
        {
            case ContaminationType.OilFilm:
                return "Vết dầu bị kéo lan ra. Dùng cồn IPA để hòa tan lớp dầu.";
            case ContaminationType.LightOxide:
                return "Màng oxy hóa nhẹ hợp với gôm tiếp điểm hơn.";
            case ContaminationType.HeavyOxide:
                return "Vết rỉ dày cần bàn chải đồng để phá lớp xanh/nâu.";
            case ContaminationType.BurntCarbon:
                return "Mảng cháy đen cần giấy nhám nhẹ, thao tác quá tay sẽ trầy.";
            case ContaminationType.TarnishedGold:
                return "Lớp mạ vàng mỏng, dùng gôm nhẹ thay vì dụng cụ quá gắt.";
            default:
                return "Dụng cụ này không ăn thêm vết bẩn, thử đổi dụng cụ khác.";
        }
    }

    private float GetToolPower(CleaningTool tool)
    {
        if (_profile == null)
        {
            _profile = CreateGenericProfile();
        }

        switch (tool)
        {
            case CleaningTool.Ipa: return _profile.IpaPower;
            case CleaningTool.Eraser: return _profile.EraserPower;
            case CleaningTool.BrassBrush: return _profile.BrushPower;
            case CleaningTool.Sandpaper: return _profile.SandpaperPower;
            default: return 1f;
        }
    }

    private float GetToolDamage(CleaningTool tool, PatchData patch)
    {
        float damage = GetBaseToolDamage(tool);
        if (patch == null)
        {
            return damage;
        }

        switch (patch.Type)
        {
            case ContaminationType.OilFilm:
                if (tool == CleaningTool.BrassBrush) return damage * 1.5f;
                if (tool == CleaningTool.Sandpaper) return damage * 2.25f;
                return damage;
            case ContaminationType.LightOxide:
                if (tool == CleaningTool.Sandpaper) return damage * 1.18f;
                return damage;
            case ContaminationType.HeavyOxide:
                if (tool == CleaningTool.BrassBrush) return damage * 0.72f;
                if (tool == CleaningTool.Sandpaper) return damage * 0.92f;
                return damage * 0.85f;
            case ContaminationType.BurntCarbon:
                if (tool == CleaningTool.BrassBrush) return damage * 0.88f;
                if (tool == CleaningTool.Sandpaper) return damage * 0.94f;
                return damage;
            case ContaminationType.TarnishedGold:
                if (tool == CleaningTool.Ipa) return damage * 0.55f;
                if (tool == CleaningTool.Eraser) return damage * 0.75f;
                if (tool == CleaningTool.BrassBrush) return damage * 1.45f;
                if (tool == CleaningTool.Sandpaper) return damage * 2.35f;
                return damage;
            default:
                return damage;
        }
    }

    private float GetBaseToolDamage(CleaningTool tool)
    {
        if (_profile == null)
        {
            _profile = CreateGenericProfile();
        }

        switch (tool)
        {
            case CleaningTool.Ipa: return _profile.IpaDamage;
            case CleaningTool.Eraser: return _profile.EraserDamage;
            case CleaningTool.BrassBrush: return _profile.BrushDamage;
            case CleaningTool.Sandpaper: return _profile.SandpaperDamage;
            default: return 0.1f;
        }
    }

    private ContaminationType PickContaminationType(int patchIndex)
    {
        if (_profile == null)
        {
            _profile = CreateGenericProfile();
        }

        if (patchIndex == 0)
        {
            return PickStarterContamination();
        }

        if (patchIndex == 1)
        {
            return PickRequiredNonSolventContamination();
        }

        float roll = UnityEngine.Random.value;
        switch (_profile.Layout)
        {
            case ContactLayout.ButtonPads:
                if (roll < 0.38f) return ContaminationType.OilFilm;
                if (roll < 0.72f) return ContaminationType.TarnishedGold;
                if (roll < 0.94f) return ContaminationType.LightOxide;
                return _difficultyLevel >= 3 ? ContaminationType.BurntCarbon : ContaminationType.OilFilm;
            case ContactLayout.BatteryTerminals:
                if (roll < 0.46f) return ContaminationType.HeavyOxide;
                if (roll < 0.7f) return ContaminationType.LightOxide;
                if (roll < 0.9f) return ContaminationType.BurntCarbon;
                return ContaminationType.OilFilm;
            case ContactLayout.PlugProngs:
                if (roll < 0.38f) return ContaminationType.HeavyOxide;
                if (roll < 0.72f) return ContaminationType.LightOxide;
                if (roll < 0.88f) return ContaminationType.BurntCarbon;
                return ContaminationType.OilFilm;
            case ContactLayout.FlatCable:
                if (roll < 0.42f) return ContaminationType.TarnishedGold;
                if (roll < 0.72f) return ContaminationType.OilFilm;
                if (roll < 0.96f) return ContaminationType.LightOxide;
                return ContaminationType.HeavyOxide;
            default:
                if (roll < 0.3f) return ContaminationType.OilFilm;
                if (roll < 0.58f) return ContaminationType.LightOxide;
                if (roll < 0.78f) return ContaminationType.HeavyOxide;
                if (roll < 0.9f) return ContaminationType.TarnishedGold;
                return ContaminationType.BurntCarbon;
        }
    }

    private ContaminationType PickStarterContamination()
    {
        switch (_profile.Layout)
        {
            case ContactLayout.ButtonPads:
            case ContactLayout.FlatCable:
                return UnityEngine.Random.value < 0.62f ? ContaminationType.OilFilm : ContaminationType.TarnishedGold;
            case ContactLayout.BatteryTerminals:
                return ContaminationType.HeavyOxide;
            case ContactLayout.PlugProngs:
                return UnityEngine.Random.value < 0.58f ? ContaminationType.LightOxide : ContaminationType.HeavyOxide;
            default:
                return UnityEngine.Random.value < 0.55f ? ContaminationType.OilFilm : ContaminationType.LightOxide;
        }
    }

    private ContaminationType PickRequiredNonSolventContamination()
    {
        switch (_profile.Layout)
        {
            case ContactLayout.ButtonPads:
            case ContactLayout.FlatCable:
                return UnityEngine.Random.value < 0.55f ? ContaminationType.TarnishedGold : ContaminationType.LightOxide;
            case ContactLayout.BatteryTerminals:
                return UnityEngine.Random.value < 0.7f ? ContaminationType.HeavyOxide : ContaminationType.BurntCarbon;
            case ContactLayout.PlugProngs:
                return UnityEngine.Random.value < 0.58f ? ContaminationType.HeavyOxide : ContaminationType.BurntCarbon;
            default:
                return ContaminationType.LightOxide;
        }
    }

    private float GetContaminationWorkScale(ContaminationType type)
    {
        switch (type)
        {
            case ContaminationType.OilFilm: return 0.78f;
            case ContaminationType.LightOxide: return 1f;
            case ContaminationType.HeavyOxide: return 1.14f;
            case ContaminationType.BurntCarbon: return 1.22f;
            case ContaminationType.TarnishedGold: return 0.92f;
            default: return 1f;
        }
    }

    private Color GetContaminationColor(ContaminationType type)
    {
        switch (type)
        {
            case ContaminationType.OilFilm: return new Color(0.32f, 0.72f, 0.88f, 0.92f);
            case ContaminationType.LightOxide: return new Color(0.86f, 0.58f, 0.18f, 0.94f);
            case ContaminationType.HeavyOxide: return new Color(0.18f, 0.62f, 0.28f, 0.95f);
            case ContaminationType.BurntCarbon: return new Color(0.1f, 0.075f, 0.055f, 0.98f);
            case ContaminationType.TarnishedGold: return new Color(0.58f, 0.5f, 0.72f, 0.94f);
            default: return Color.white;
        }
    }

    private Color GetContaminationLabelColor(ContaminationType type)
    {
        switch (type)
        {
            case ContaminationType.OilFilm: return new Color(0.68f, 0.94f, 1f, 1f);
            case ContaminationType.LightOxide: return new Color(1f, 0.82f, 0.36f, 1f);
            case ContaminationType.HeavyOxide: return new Color(0.58f, 1f, 0.54f, 1f);
            case ContaminationType.BurntCarbon: return new Color(1f, 0.58f, 0.42f, 1f);
            case ContaminationType.TarnishedGold: return new Color(0.86f, 0.78f, 1f, 1f);
            default: return new Color(0.98f, 0.9f, 0.66f, 1f);
        }
    }

    private string GetContaminationShortName(ContaminationType type)
    {
        switch (type)
        {
            case ContaminationType.OilFilm: return "DẦU";
            case ContaminationType.LightOxide: return "OXI";
            case ContaminationType.HeavyOxide: return "RỈ";
            case ContaminationType.BurntCarbon: return "CHÁY";
            case ContaminationType.TarnishedGold: return "XỈ";
            default: return "BẨN";
        }
    }

    private Color GetToolColor(CleaningTool tool)
    {
        switch (tool)
        {
            case CleaningTool.Ipa: return new Color(0.08f, 0.34f, 0.5f, 1f);
            case CleaningTool.Eraser: return new Color(0.34f, 0.29f, 0.16f, 1f);
            case CleaningTool.BrassBrush: return new Color(0.42f, 0.28f, 0.08f, 1f);
            case CleaningTool.Sandpaper: return new Color(0.42f, 0.14f, 0.08f, 1f);
            default: return Color.gray;
        }
    }

    private string GetToolDisplayName(CleaningTool tool)
    {
        switch (tool)
        {
            case CleaningTool.Ipa: return "cồn IPA";
            case CleaningTool.Eraser: return "gôm tiếp điểm";
            case CleaningTool.BrassBrush: return "bàn chải đồng";
            case CleaningTool.Sandpaper: return "giấy nhám";
            default: return "dụng cụ";
        }
    }


    private string GetToolHint(CleaningTool tool)
    {
        switch (tool)
        {
            case CleaningTool.Ipa:
                return AppendProfileAdvice("Cồn IPA: tốt nhất cho DẦU/bụi ẩm. Với RỈ hoặc CHÁY, cồn chỉ làm mềm và sẽ dừng giữa chừng.");
            case CleaningTool.Eraser:
                return AppendProfileAdvice("Gôm tiếp điểm: tốt cho OXI nhẹ và XỈ trên lớp mạ vàng. Ít trầy, hợp để hoàn thiện.");
            case CleaningTool.BrassBrush:
                return AppendProfileAdvice("Bàn chải đồng: phá RỈ xanh/nâu rất tốt. Gần sạch thì đổi sang gôm hoặc cồn để tránh xước.");
            case CleaningTool.Sandpaper:
                return AppendProfileAdvice("Giấy nhám: chỉ dành cho CHÁY đen hoặc mảng cực dày. Dùng sai trên DẦU/XỈ sẽ trầy rất nhanh.");
            default:
                return "";
        }
    }

    private string AppendProfileAdvice(string toolText)
    {
        if (_profile == null || string.IsNullOrEmpty(_profile.ToolAdvice))
        {
            return toolText;
        }

        return toolText + "\n\n" + _profile.ToolAdvice;
    }

    private string GetResultText(RepairQuality quality)
    {
        switch (quality)
        {
            case RepairQuality.Perfect: return "Ti\u1ebfp \u0111i\u1ec3m s\u00e1ng \u0111\u1eb9p, kh\u00f4ng tr\u1ea7y l\u1edbp m\u1ea1.";
            case RepairQuality.Good: return "Ti\u1ebfp \u0111i\u1ec3m \u0111\u00e3 s\u1ea1ch, m\u00e1y nh\u1eadn \u0111i\u1ec7n \u1ed5n.";
            case RepairQuality.Passable: return "S\u1ea1ch v\u1eeba \u0111\u1ee7, nh\u01b0ng c\u00f3 v\u00e0i v\u1ebft c\u00e0o.";
            default: return "V\u1ec7 sinh ch\u01b0a \u0111\u1ea1t, ti\u1ebfp \u0111i\u1ec3m c\u00f2n b\u1ea9n ho\u1eb7c b\u1ecb tr\u1ea7y.";
        }
    }

    private Color GetResultColor(RepairQuality quality)
    {
        switch (quality)
        {
            case RepairQuality.Perfect: return new Color(0.42f, 1f, 0.72f, 1f);
            case RepairQuality.Good: return new Color(0.62f, 0.95f, 1f, 1f);
            case RepairQuality.Passable: return new Color(1f, 0.82f, 0.34f, 1f);
            default: return new Color(1f, 0.32f, 0.25f, 1f);
        }
    }

    private CleaningProfile PickProfile(List<string> faults)
    {
        string context = (_contextItemName + " " + _contextObjectName + " " + string.Join(" ", faults ?? new List<string>())).ToLowerInvariant();

        if (ContainsAny(context, "bàn phím", "ban phim", "keyboard", "phím", "phim"))
        {
            return CreateKeyboardProfile();
        }

        if (ContainsAny(context, "remote", "điều khiển", "dieu khien", "nút bấm", "nut bam"))
        {
            return CreateButtonProfile();
        }

        if (ContainsAny(context, "pin", "battery", "ắc quy", "ac quy", "đèn pin", "den pin"))
        {
            return CreateBatteryProfile();
        }

        if (ContainsAny(context, "phích", "phich", "ổ cắm", "o cam", "plug", "nguồn", "nguon", "adapter", "sạc", "sac"))
        {
            return CreatePlugProfile();
        }

        if (ContainsAny(context, "laptop", "tivi", "pc", "màn hình", "man hinh", "cáp", "cap", "socket", "connector"))
        {
            return CreateFlatCableProfile();
        }

        if (ContainsAny(context, "quạt", "quat", "nồi cơm", "noi com", "ấm", "am", "công tắc", "cong tac"))
        {
            return CreateSwitchProfile();
        }

        return CreateGenericProfile();
    }

    private bool ContainsAny(string value, params string[] keywords)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (value.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private CleaningProfile CreateGenericProfile()
    {
        return new CleaningProfile
        {
            Title = "VỆ SINH TIẾP ĐIỂM OXY HÓA",
            BoardLabel = "CỤM CHÂN TIẾP XÚC",
            GuideText = "Hướng dẫn: đọc nhãn DẦU/OXI/RỈ/CHÁY/XỈ, chọn đúng dụng cụ rồi kéo chuột trên vết bẩn. Dụng cụ mạnh sạch nhanh nhưng dễ làm trầy tiếp điểm.",
            ToolAdvice = "Cồn chỉ xử lý tốt DẦU. OXI/XỈ cần gôm, RỈ cần bàn chải đồng, CHÁY cần giấy nhám nhẹ.",
            ContactPrefix = "PIN",
            Layout = ContactLayout.EdgePins,
            ContactCount = 8,
            BasePatchCount = 2,
            ContactSize = new Vector2(76f, 450f),
            RequiredWorkScale = 1f,
            OxidationSizeScale = 1f,
            ScratchLimitScale = 1f,
            TimeBonus = 0f,
            IpaPower = 0.72f,
            EraserPower = 1f,
            BrushPower = 1.35f,
            SandpaperPower = 1.75f,
            IpaDamage = 0.05f,
            EraserDamage = 0.12f,
            BrushDamage = 0.62f,
            SandpaperDamage = 1.3f,
            BoardColor = new Color(0.025f, 0.21f, 0.16f, 1f),
            BoardLightColor = new Color(0.06f, 0.34f, 0.25f, 1f),
            TraceA = new Color(0.09f, 0.62f, 0.42f, 0.55f),
            TraceB = new Color(0.06f, 0.48f, 0.58f, 0.48f),
            MetalMain = new Color(0.92f, 0.58f, 0.16f, 1f),
            MetalHighlight = new Color(1f, 0.9f, 0.42f, 0.85f),
            OxideA = new Color(0.38f, 0.18f, 0.045f, 0.9f),
            OxideB = new Color(0.18f, 0.34f, 0.17f, 0.78f)
        };
    }

    private CleaningProfile CreateButtonProfile()
    {
        CleaningProfile profile = CreateGenericProfile();
        profile.Title = "VỆ SINH PAD NÚT BẤM";
        profile.BoardLabel = "PAD CAO SU DẪN ĐIỆN";
        profile.GuideText = "Pad nút bấm rất mềm: DẦU dùng IPA, OXI/XỈ dùng gôm. Tránh bàn chải/giấy nhám nếu không có mảng CHÁY.";
        profile.ToolAdvice = "Pad cao su dễ rách lớp than dẫn điện. Cồn an toàn cho dầu, nhưng không bóc được lớp xỉ mạ.";
        profile.ContactPrefix = "PAD";
        profile.Layout = ContactLayout.ButtonPads;
        profile.ContactCount = 8;
        profile.BasePatchCount = 3;
        profile.ContactSize = new Vector2(112f, 112f);
        profile.RequiredWorkScale = 0.92f;
        profile.OxidationSizeScale = 0.82f;
        profile.ScratchLimitScale = 0.68f;
        profile.IpaPower = 1.12f;
        profile.EraserPower = 1.22f;
        profile.BrushPower = 0.92f;
        profile.SandpaperPower = 1.18f;
        profile.IpaDamage = 0.025f;
        profile.EraserDamage = 0.08f;
        profile.BrushDamage = 0.95f;
        profile.SandpaperDamage = 1.85f;
        profile.BoardColor = new Color(0.045f, 0.075f, 0.09f, 1f);
        profile.BoardLightColor = new Color(0.09f, 0.16f, 0.18f, 1f);
        profile.TraceA = new Color(0.18f, 0.66f, 0.86f, 0.45f);
        profile.TraceB = new Color(0.18f, 0.9f, 0.62f, 0.38f);
        profile.MetalMain = new Color(0.18f, 0.18f, 0.16f, 1f);
        profile.MetalHighlight = new Color(0.68f, 0.68f, 0.58f, 0.9f);
        profile.OxideA = new Color(0.08f, 0.07f, 0.055f, 0.92f);
        profile.OxideB = new Color(0.2f, 0.25f, 0.19f, 0.72f);
        return profile;
    }

    private CleaningProfile CreateKeyboardProfile()
    {
        CleaningProfile profile = CreateButtonProfile();
        profile.Title = "VỆ SINH MA TRẬN PHÍM";
        profile.BoardLabel = "CỤM PAD BÀN PHÍM";
        profile.GuideText = "Pad bàn phím mỏng hơn remote: DẦU dùng IPA, OXI/XỈ dùng gôm nhẹ. Tránh giấy nhám.";
        profile.ToolAdvice = "Bàn phím cần giữ lớp carbon. Giấy nhám chỉ nên dùng khi có CHÁY và phải dừng ngay khi sạch.";
        profile.ContactCount = 10;
        profile.BasePatchCount = 4;
        profile.RequiredWorkScale = 0.86f;
        profile.ScratchLimitScale = 0.58f;
        profile.SandpaperDamage = 2.15f;
        profile.BrushDamage = 1.18f;
        return profile;
    }

    private CleaningProfile CreateBatteryProfile()
    {
        CleaningProfile profile = CreateGenericProfile();
        profile.Title = "VỆ SINH CỰC PIN BỊ RỈ";
        profile.BoardLabel = "CỰC PIN ÂM / DƯƠNG";
        profile.GuideText = "Cực pin hay có RỈ xanh/nâu: dùng bàn chải đồng trước, cồn để lau dầu sau. CHÁY đen mới cần giấy nhám.";
        profile.ToolAdvice = "Cực pin bẩn nặng nên bàn chải đồng có lợi thế rõ. IPA một mình sẽ bị kẹt lại ở lớp rỉ.";
        profile.ContactPrefix = "CỰC";
        profile.Layout = ContactLayout.BatteryTerminals;
        profile.ContactCount = 2;
        profile.BasePatchCount = 2;
        profile.ContactSize = new Vector2(180f, 180f);
        profile.RequiredWorkScale = 1.22f;
        profile.OxidationSizeScale = 1.18f;
        profile.ScratchLimitScale = 1.35f;
        profile.TimeBonus = 8f;
        profile.IpaPower = 0.58f;
        profile.EraserPower = 0.9f;
        profile.BrushPower = 1.65f;
        profile.SandpaperPower = 1.85f;
        profile.IpaDamage = 0.035f;
        profile.EraserDamage = 0.08f;
        profile.BrushDamage = 0.35f;
        profile.SandpaperDamage = 0.92f;
        profile.BoardColor = new Color(0.045f, 0.055f, 0.052f, 1f);
        profile.BoardLightColor = new Color(0.13f, 0.15f, 0.13f, 1f);
        profile.TraceA = new Color(0.36f, 0.82f, 0.42f, 0.42f);
        profile.TraceB = new Color(0.52f, 0.74f, 1f, 0.36f);
        profile.MetalMain = new Color(0.62f, 0.64f, 0.58f, 1f);
        profile.MetalHighlight = new Color(0.98f, 0.96f, 0.82f, 0.82f);
        profile.OxideA = new Color(0.62f, 0.29f, 0.05f, 0.92f);
        profile.OxideB = new Color(0.12f, 0.42f, 0.24f, 0.82f);
        return profile;
    }

    private CleaningProfile CreatePlugProfile()
    {
        CleaningProfile profile = CreateGenericProfile();
        profile.Title = "VỆ SINH CHÂN CẮM NGUỒN";
        profile.BoardLabel = "CỌC L / N / E";
        profile.GuideText = "Chân cắm kim loại khá cứng: RỈ dùng bàn chải đồng, OXI dùng gôm, CHÁY mới dùng giấy nhám.";
        profile.ToolAdvice = "Chân cắm chịu được bàn chải, nhưng giấy nhám vẫn làm xước và trừ điểm nếu dùng sai vết.";
        profile.ContactPrefix = "CỌC";
        profile.Layout = ContactLayout.PlugProngs;
        profile.ContactCount = 3;
        profile.BasePatchCount = 2;
        profile.ContactSize = new Vector2(112f, 470f);
        profile.RequiredWorkScale = 1.08f;
        profile.OxidationSizeScale = 1.05f;
        profile.ScratchLimitScale = 1.15f;
        profile.IpaPower = 0.62f;
        profile.EraserPower = 1.05f;
        profile.BrushPower = 1.58f;
        profile.SandpaperPower = 1.78f;
        profile.BrushDamage = 0.42f;
        profile.SandpaperDamage = 1.08f;
        profile.BoardColor = new Color(0.035f, 0.105f, 0.13f, 1f);
        profile.BoardLightColor = new Color(0.08f, 0.22f, 0.25f, 1f);
        profile.TraceA = new Color(0.2f, 0.72f, 0.94f, 0.42f);
        profile.TraceB = new Color(0.95f, 0.54f, 0.22f, 0.34f);
        profile.MetalMain = new Color(0.78f, 0.69f, 0.45f, 1f);
        profile.MetalHighlight = new Color(1f, 0.9f, 0.58f, 0.88f);
        return profile;
    }

    private CleaningProfile CreateFlatCableProfile()
    {
        CleaningProfile profile = CreateGenericProfile();
        profile.Title = "VỆ SINH SOCKET CÁP DẸT";
        profile.BoardLabel = "DÃY CHÂN FPC MỎNG";
        profile.GuideText = "Socket cáp dẹt rất sát nhau: DẦU dùng IPA, XỈ/OXI dùng gôm. Bàn chải/giấy nhám rất dễ làm chập.";
        profile.ToolAdvice = "Chân FPC nhỏ, rất dễ xước và chập kề bên. Càng sạch cuối lượt càng phải nhẹ tay.";
        profile.ContactPrefix = "FPC";
        profile.Layout = ContactLayout.FlatCable;
        profile.ContactCount = 12;
        profile.BasePatchCount = 3;
        profile.ContactSize = new Vector2(42f, 430f);
        profile.RequiredWorkScale = 0.98f;
        profile.OxidationSizeScale = 0.72f;
        profile.ScratchLimitScale = 0.62f;
        profile.TimeBonus = 4f;
        profile.IpaPower = 0.98f;
        profile.EraserPower = 1.14f;
        profile.BrushPower = 0.84f;
        profile.SandpaperPower = 1.05f;
        profile.IpaDamage = 0.025f;
        profile.EraserDamage = 0.08f;
        profile.BrushDamage = 1f;
        profile.SandpaperDamage = 2.1f;
        profile.BoardColor = new Color(0.03f, 0.08f, 0.11f, 1f);
        profile.BoardLightColor = new Color(0.07f, 0.18f, 0.23f, 1f);
        profile.TraceA = new Color(0.14f, 0.76f, 1f, 0.42f);
        profile.TraceB = new Color(0.32f, 0.94f, 0.72f, 0.34f);
        profile.MetalMain = new Color(0.94f, 0.64f, 0.22f, 1f);
        profile.MetalHighlight = new Color(1f, 0.92f, 0.48f, 0.86f);
        return profile;
    }

    private CleaningProfile CreateSwitchProfile()
    {
        CleaningProfile profile = CreatePlugProfile();
        profile.Title = "VỆ SINH LÁ ĐỒNG CÔNG TẮC";
        profile.BoardLabel = "CẶP LÁ ĐỒNG TIẾP ĐIỆN";
        profile.GuideText = "Lá đồng công tắc chịu lực hơn pad mềm: RỈ dùng bàn chải, CHÁY dùng giấy nhám nhẹ, OXI dùng gôm.";
        profile.ToolAdvice = "Bàn chải đồng tốt cho rỉ và cháy nhẹ. Gôm giúp hoàn thiện bề mặt tiếp xúc.";
        profile.ContactCount = 2;
        profile.ContactSize = new Vector2(130f, 455f);
        profile.RequiredWorkScale = 1.16f;
        profile.ScratchLimitScale = 1.05f;
        profile.BrushPower = 1.5f;
        profile.SandpaperDamage = 1.18f;
        return profile;
    }

    private List<Vector2> GetPatchPositions(ContactLayout layout)
    {
        switch (layout)
        {
            case ContactLayout.ButtonPads:
                return new List<Vector2>
                {
                    new Vector2(-270f, 150f),
                    new Vector2(-90f, 118f),
                    new Vector2(90f, 150f),
                    new Vector2(270f, 112f),
                    new Vector2(-220f, -105f),
                    new Vector2(0f, -132f),
                    new Vector2(225f, -100f),
                    new Vector2(330f, -10f)
                };
            case ContactLayout.PlugProngs:
                return new List<Vector2>
                {
                    new Vector2(-160f, 150f),
                    new Vector2(140f, 128f),
                    new Vector2(0f, -45f),
                    new Vector2(-210f, -120f),
                    new Vector2(210f, -112f),
                    new Vector2(305f, 65f)
                };
            case ContactLayout.BatteryTerminals:
                return new List<Vector2>
                {
                    new Vector2(-230f, 44f),
                    new Vector2(214f, -38f),
                    new Vector2(-190f, -108f),
                    new Vector2(248f, 102f),
                    new Vector2(0f, 8f)
                };
            case ContactLayout.FlatCable:
                return new List<Vector2>
                {
                    new Vector2(-320f, 150f),
                    new Vector2(-220f, -112f),
                    new Vector2(-100f, 95f),
                    new Vector2(0f, -140f),
                    new Vector2(120f, 140f),
                    new Vector2(240f, -96f),
                    new Vector2(330f, 76f)
                };
            default:
                return new List<Vector2>
                {
                    new Vector2(-330f, 165f),
                    new Vector2(-120f, 125f),
                    new Vector2(120f, 165f),
                    new Vector2(330f, 110f),
                    new Vector2(-230f, -95f),
                    new Vector2(10f, -135f),
                    new Vector2(260f, -95f)
                };
        }
    }

    private Sprite CreateContactBoardSprite(CleaningProfile profile)
    {
        Texture2D texture = new Texture2D(900, 590, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));

        Color board = profile.BoardColor;
        Color boardLight = profile.BoardLightColor;
        Color edge = Color.Lerp(profile.TraceA, Color.white, 0.18f);
        MinigameUiKit.FillRounded(texture, 12, 12, 888, 578, 34, board, edge);
        MinigameUiKit.FillRounded(texture, 38, 38, 862, 552, 24, new Color(0.02f, 0.16f, 0.13f, 0.55f), new Color(1f, 1f, 1f, 0.04f));

        for (int i = 0; i < 12; i++)
        {
            int y = 80 + (i * 38);
            MinigameUiKit.DrawLine(texture, 70, y, 830, y + ((i % 2 == 0) ? 18 : -18), profile.TraceA * new Color(1f, 1f, 1f, 0.36f), 3);
        }

        for (int i = 0; i < 10; i++)
        {
            int x = 75 + (i * 80);
            MinigameUiKit.DrawLine(texture, x, 72, x + ((i % 2 == 0) ? 34 : -34), 518, profile.TraceB * new Color(1f, 1f, 1f, 0.32f), 2);
        }

        for (int i = 0; i < 28; i++)
        {
            int x = UnityEngine.Random.Range(58, 842);
            int y = UnityEngine.Random.Range(56, 534);
            int r = UnityEngine.Random.Range(3, 7);
            MinigameUiKit.FillCircle(texture, x, y, r + 3, new Color(0f, 0f, 0f, 0.18f));
            MinigameUiKit.FillCircle(texture, x, y, r, boardLight);
            MinigameUiKit.FillCircle(texture, x, y, Mathf.Max(1, r - 3), new Color(0.01f, 0.08f, 0.065f, 1f));
        }

        for (int i = 0; i < 4; i++)
        {
            int x = i < 2 ? 54 : 846;
            int y = i % 2 == 0 ? 54 : 536;
            MinigameUiKit.FillCircle(texture, x, y, 22, new Color(0f, 0f, 0f, 0.34f));
            MinigameUiKit.FillCircle(texture, x, y, 16, new Color(0.12f, 0.22f, 0.2f, 1f));
            MinigameUiKit.FillCircle(texture, x, y, 8, new Color(0.02f, 0.07f, 0.06f, 1f));
        }

        for (int i = 0; i < 180; i++)
        {
            int x = UnityEngine.Random.Range(30, 870);
            int y = UnityEngine.Random.Range(30, 560);
            float shade = UnityEngine.Random.Range(-0.025f, 0.035f);
            Color speck = new Color(Mathf.Clamp01(board.r + shade), Mathf.Clamp01(board.g + shade), Mathf.Clamp01(board.b + shade), 1f);
            texture.SetPixel(x, y, speck);
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateGoldStripSprite(CleaningProfile profile)
    {
        Texture2D texture = new Texture2D(96, 512, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));

        MinigameUiKit.FillRounded(texture, 10, 10, 86, 502, 16, new Color(0f, 0f, 0f, 0.28f), new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.FillRounded(texture, 7, 7, 83, 499, 16, profile.MetalMain, profile.MetalHighlight);
        MinigameUiKit.FillRounded(texture, 16, 18, 42, 488, 10, Color.Lerp(profile.MetalHighlight, Color.white, 0.28f) * new Color(1f, 1f, 1f, 0.78f), new Color(1f, 1f, 1f, 0.08f));
        MinigameUiKit.FillRounded(texture, 55, 18, 75, 488, 10, Color.Lerp(profile.MetalMain, Color.black, 0.28f) * new Color(1f, 1f, 1f, 0.58f), new Color(0f, 0f, 0f, 0.03f));

        for (int i = 0; i < 18; i++)
        {
            int y = 38 + (i * 25);
            MinigameUiKit.DrawLine(texture, 18, y, 76, y + UnityEngine.Random.Range(-3, 4), new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.06f, 0.14f)), 2);
        }

        for (int i = 0; i < 34; i++)
        {
            int x = UnityEngine.Random.Range(18, 76);
            int y = UnityEngine.Random.Range(24, 488);
            MinigameUiKit.FillCircle(texture, x, y, UnityEngine.Random.Range(1, 3), new Color(0.42f, 0.19f, 0.03f, UnityEngine.Random.Range(0.12f, 0.28f)));
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateOxidationSprite(CleaningProfile profile)
    {
        Texture2D texture = new Texture2D(190, 155, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));

        MinigameUiKit.FillCircle(texture, 88, 76, 52, profile.OxideA);
        MinigameUiKit.FillCircle(texture, 58, 86, 32, profile.OxideB);
        MinigameUiKit.FillCircle(texture, 128, 58, 34, Color.Lerp(profile.OxideA, new Color(0.74f, 0.32f, 0.05f, 0.72f), 0.45f));
        MinigameUiKit.FillCircle(texture, 124, 102, 28, Color.Lerp(profile.OxideB, new Color(0.2f, 0.42f, 0.18f, 0.62f), 0.45f));
        MinigameUiKit.FillCircle(texture, 92, 78, 28, new Color(0.08f, 0.055f, 0.032f, 0.46f));

        for (int i = 0; i < 38; i++)
        {
            int x = UnityEngine.Random.Range(28, 160);
            int y = UnityEngine.Random.Range(22, 132);
            int r = UnityEngine.Random.Range(3, 11);
            Color color = UnityEngine.Random.value > 0.45f
                ? new Color(0.12f, 0.29f, 0.14f, UnityEngine.Random.Range(0.22f, 0.52f))
                : new Color(0.78f, 0.38f, 0.08f, UnityEngine.Random.Range(0.18f, 0.44f));
            MinigameUiKit.FillCircle(texture, x, y, r, color);
        }

        for (int i = 0; i < 70; i++)
        {
            int x = UnityEngine.Random.Range(34, 154);
            int y = UnityEngine.Random.Range(28, 126);
            MinigameUiKit.FillCircle(texture, x, y, UnityEngine.Random.Range(1, 3), new Color(0.035f, 0.025f, 0.012f, UnityEngine.Random.Range(0.24f, 0.62f)));
        }

        for (int i = 0; i < 12; i++)
        {
            int x0 = UnityEngine.Random.Range(42, 130);
            int y0 = UnityEngine.Random.Range(34, 115);
            MinigameUiKit.DrawLine(texture, x0, y0, x0 + UnityEngine.Random.Range(18, 42), y0 + UnityEngine.Random.Range(-16, 17), new Color(0.92f, 0.66f, 0.28f, 0.18f), UnityEngine.Random.Range(2, 5));
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateToolIconSprite(CleaningTool tool)
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));

        switch (tool)
        {
            case CleaningTool.Ipa:
                MinigameUiKit.FillRounded(texture, 35, 20, 63, 72, 8, new Color(0.78f, 0.94f, 1f, 0.95f), new Color(0.1f, 0.42f, 0.64f, 1f));
                MinigameUiKit.FillRounded(texture, 39, 10, 59, 24, 5, new Color(0.08f, 0.18f, 0.25f, 1f), new Color(1f, 1f, 1f, 0.2f));
                MinigameUiKit.FillRounded(texture, 38, 42, 60, 62, 4, new Color(0.08f, 0.36f, 0.55f, 1f), new Color(1f, 1f, 1f, 0.25f));
                MinigameUiKit.FillCircle(texture, 67, 74, 9, new Color(0.55f, 0.9f, 1f, 0.75f));
                break;
            case CleaningTool.Eraser:
                MinigameUiKit.FillRounded(texture, 20, 32, 74, 64, 10, new Color(0.95f, 0.76f, 0.38f, 1f), new Color(0.48f, 0.33f, 0.12f, 1f));
                MinigameUiKit.FillRounded(texture, 28, 25, 82, 52, 8, new Color(0.98f, 0.9f, 0.58f, 1f), new Color(1f, 1f, 1f, 0.22f));
                MinigameUiKit.DrawLine(texture, 32, 55, 74, 31, new Color(1f, 1f, 1f, 0.28f), 4);
                break;
            case CleaningTool.BrassBrush:
                MinigameUiKit.FillRounded(texture, 18, 48, 78, 68, 7, new Color(0.24f, 0.15f, 0.07f, 1f), new Color(0.92f, 0.58f, 0.15f, 0.55f));
                for (int i = 0; i < 13; i++)
                {
                    int x = 22 + (i * 4);
                    MinigameUiKit.DrawLine(texture, x, 48, x + 8, 22, new Color(1f, 0.76f, 0.24f, 1f), 2);
                }
                MinigameUiKit.FillRounded(texture, 24, 64, 72, 78, 5, new Color(0.62f, 0.34f, 0.1f, 1f), new Color(1f, 0.82f, 0.42f, 0.45f));
                break;
            case CleaningTool.Sandpaper:
                MinigameUiKit.FillRounded(texture, 18, 22, 76, 74, 6, new Color(0.52f, 0.22f, 0.1f, 1f), new Color(0.94f, 0.52f, 0.22f, 0.45f));
                for (int i = 0; i < 42; i++)
                {
                    int x = UnityEngine.Random.Range(25, 70);
                    int y = UnityEngine.Random.Range(28, 68);
                    MinigameUiKit.FillCircle(texture, x, y, UnityEngine.Random.Range(1, 3), new Color(1f, 0.72f, 0.32f, 0.56f));
                }
                MinigameUiKit.DrawLine(texture, 24, 30, 70, 66, new Color(1f, 1f, 1f, 0.16f), 3);
                break;
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void ShowUI(bool show)
    {
        if (_uiRoot == null)
        {
            return;
        }

        _uiRoot.SetActive(show);
        if (show)
        {
            _uiRoot.transform.SetAsLastSibling();
        }
    }

    private void RestoreCursor()
    {
        Cursor.lockState = _previousLockMode;
        Cursor.visible = _previousCursorVisible;
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
