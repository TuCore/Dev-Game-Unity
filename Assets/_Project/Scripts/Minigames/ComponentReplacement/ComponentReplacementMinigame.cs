using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComponentReplacementMinigame : MonoBehaviour, IMinigame
{
    private enum Tool
    {
        Iron,
        Pump,
        Tweezers,
        Replacement
    }

    private enum ComponentKind
    {
        Resistor,
        Capacitor,
        Diode,
        Ic
    }

    private sealed class PinData
    {
        public Vector2 Offset;
        public float Heat;
        public bool Melted;
        public bool Cleaned;
        public bool Soldered;
        public Button Button;
        public Image Image;
        public TextMeshProUGUI Label;
    }

    private sealed class ComponentTask
    {
        public string Id;
        public string Label;
        public ComponentKind Kind;
        public Vector2 Position;
        public Vector2 Size;
        public bool NeedsOrientation;
        public int CorrectRotation;
        public bool Removed;
        public bool Placed;
        public bool Complete;
        public List<PinData> Pins = new List<PinData>();
        public Button BodyButton;
        public Image BodyImage;
        public TextMeshProUGUI StatusText;
    }

    public string MinigameName => "Thay linh kien chay";
    public bool IsActive { get; private set; }
    public event Action<RepairQuality> OnMinigameCompleted;

    [Header("Rules")]
    [SerializeField] private float baseTimeLimit = 105f;
    [SerializeField] private int baseMistakeLimit = 5;
    [SerializeField] private float maxHeatDamage = 100f;

    private readonly List<ComponentTask> _tasks = new List<ComponentTask>();
    private readonly Dictionary<Tool, Button> _toolButtons = new Dictionary<Tool, Button>();

    private GameObject _uiRoot;
    private Transform _boardLayer;
    private Transform _traceLayer;
    private Transform _componentLayer;
    private TextMeshProUGUI _timerText;
    private TextMeshProUGUI _progressText;
    private TextMeshProUGUI _feedbackText;
    private TextMeshProUGUI _toolHintText;
    private TextMeshProUGUI _orientationText;
    private TextMeshProUGUI _selectedToolText;
    private Image _heatBar;
    private Button _rotateButton;
    private Button _finishButton;

    private Sprite _solidSprite;
    private Sprite _panelSprite;
    private Sprite _pinSprite;
    private Sprite _pinMeltedSprite;
    private Sprite _pinCleanSprite;
    private Sprite _pinSolderSprite;
    private Sprite _burnSprite;
    private Sprite _boardSprite;
    private Sprite _ironSprite;
    private Sprite _pumpSprite;
    private Sprite _tweezersSprite;
    private Sprite _partSprite;

    private Tool _selectedTool = Tool.Iron;
    private int _replacementRotation;
    private int _difficultyLevel = 1;
    private int _mistakes;
    private int _mistakeLimit;
    private float _timeRemaining;
    private float _startedAt;
    private float _heatDamage;
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
            return;
        }

        _timeRemaining -= Time.deltaTime;
        UpdateStatusText();

        if (_timeRemaining <= 0f)
        {
            Complete(RepairQuality.Broken, true, "Hết giờ. Linh kiện chưa được thay xong.");
        }
    }

    public void Initialize(List<string> faults, int difficultyLevel)
    {
        EnsureUI();

        _difficultyLevel = Mathf.Clamp(difficultyLevel, 1, 5);
        _mistakes = 0;
        _mistakeLimit = Mathf.Max(2, baseMistakeLimit - Mathf.Max(0, _difficultyLevel - 2));
        _heatDamage = 0f;
        _timeRemaining = Mathf.Max(55f, baseTimeLimit - ((_difficultyLevel - 1) * 9f));
        _replacementRotation = UnityEngine.Random.Range(0, 4) * 90;
        _selectedTool = Tool.Iron;
        _isFinishing = false;

        BuildTasks();
        RebuildBoard();
        SelectTool(_selectedTool);
        ShowFeedback("Chọn mỏ hàn, bấm từng chân để làm chảy thiếc. Làm theo đúng thứ tự trên bảng hướng dẫn.", new Color(0.78f, 0.9f, 1f, 1f));
        UpdateStatusText();
    }

    public void StartMinigame()
    {
        EnsureUI();
        if (_tasks.Count == 0)
        {
            Initialize(null, _difficultyLevel);
        }

        IsActive = true;
        _startedAt = Time.time;

        _previousLockMode = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowUI(true);
        MinigameSfxKit.Play(MinigameSfxCue.Open, 0.58f);
        UpdateStatusText();

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowSubtitle("Anh Thợ Điện", "Thay linh kiện cháy: xả thiếc, gắp linh kiện cũ, đặt linh kiện mới đúng chiều rồi hàn lại.", 4f, "Tiếng đặt đồ");
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
        Complete(RepairQuality.Broken, false, "Đã hủy thay linh kiện.");
    }

    private void EnsureUI()
    {
        if (_uiRoot != null)
        {
            return;
        }

        EnsureSprites();
        _uiRoot = MinigameUiKit.CreateCanvasRoot("ComponentReplacementUI", transform, 520);

        Image overlay = MinigameUiKit.CreateImage(_uiRoot.transform, "BackgroundOverlay", _solidSprite, new Color(0.006f, 0.008f, 0.012f, 0.93f), false);
        MinigameUiKit.Stretch(overlay.rectTransform);
        overlay.transform.SetAsFirstSibling();

        Image header = MinigameUiKit.CreatePanel(_uiRoot.transform, "Header", _panelSprite, new Color(0.035f, 0.04f, 0.046f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(1760f, 72f));
        MinigameUiKit.AddChrome(header.transform, _solidSprite, new Color(1f, 0.45f, 0.18f, 0.95f));
        TextMeshProUGUI title = MinigameUiKit.CreateText(header.transform, "Title", "THAY LINH KIỆN CHÁY", 29, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.95f, 0.98f, 1f, 1f));
        MinigameUiKit.SetAnchored(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(620f, 48f));

        _progressText = MinigameUiKit.CreateText(header.transform, "Progress", "", 23, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.32f, 1f));
        MinigameUiKit.SetAnchored(_progressText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 48f));

        _timerText = MinigameUiKit.CreateText(header.transform, "Timer", "", 23, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.72f, 0.92f, 1f, 1f));
        MinigameUiKit.SetAnchored(_timerText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(620f, 48f));

        Image boardPanel = MinigameUiKit.CreatePanel(_uiRoot.transform, "BoardPanel", _panelSprite, new Color(0.018f, 0.024f, 0.027f, 0.98f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(640f, -18f), new Vector2(1210f, 820f));
        MinigameUiKit.AddChrome(boardPanel.transform, _solidSprite, new Color(1f, 0.45f, 0.18f, 0.5f));
        Image board = MinigameUiKit.CreateImage(boardPanel.transform, "BoardArt", _boardSprite, Color.white, false);
        MinigameUiKit.SetAnchored(board.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(1040f, 620f));

        Image guidePanel = MinigameUiKit.CreatePanel(boardPanel.transform, "GuidePanel", _panelSprite, new Color(0.032f, 0.045f, 0.05f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1120f, 56f));
        MinigameUiKit.AddChrome(guidePanel.transform, _solidSprite, new Color(1f, 0.45f, 0.18f, 0.45f));
        TextMeshProUGUI guide = MinigameUiKit.CreateText(guidePanel.transform, "Guide", "Thứ tự: 1 Mỏ hàn làm chảy thiếc  2 Hút thiếc  3 Nhíp gắp linh kiện cũ  4 Đặt linh kiện mới, xoay đúng chiều  5 Hàn lại từng chân", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.86f, 0.94f, 0.92f, 1f));
        MinigameUiKit.Stretch(guide.rectTransform);

        _traceLayer = MinigameUiKit.CreateUIObject("TraceLayer", boardPanel.transform).transform;
        MinigameUiKit.Stretch(_traceLayer.GetComponent<RectTransform>());
        _boardLayer = MinigameUiKit.CreateUIObject("BoardLayer", boardPanel.transform).transform;
        MinigameUiKit.Stretch(_boardLayer.GetComponent<RectTransform>());
        _componentLayer = MinigameUiKit.CreateUIObject("ComponentLayer", boardPanel.transform).transform;
        MinigameUiKit.Stretch(_componentLayer.GetComponent<RectTransform>());

        Image toolPanel = MinigameUiKit.CreatePanel(_uiRoot.transform, "ToolPanel", _panelSprite, new Color(0.025f, 0.029f, 0.036f, 0.98f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-300f, -18f), new Vector2(520f, 820f));
        MinigameUiKit.AddChrome(toolPanel.transform, _solidSprite, new Color(1f, 0.45f, 0.18f, 0.5f));
        BuildToolPanel(toolPanel.transform);

        _feedbackText = MinigameUiKit.CreateText(_uiRoot.transform, "Feedback", "", 25, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        MinigameUiKit.SetAnchored(_feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 42f), new Vector2(1240f, 48f));

        _finishButton = MinigameUiKit.CreateButton(_uiRoot.transform, "FinishButton", "KIỂM TRA", _panelSprite, new Color(0.12f, 0.42f, 0.28f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 58f), new Vector2(220f, 56f), TryFinish);
        MinigameUiKit.CreateButton(_uiRoot.transform, "CancelButton", "HỦY", _panelSprite, new Color(0.35f, 0.09f, 0.08f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(130f, 58f), new Vector2(170f, 56f), () => Complete(RepairQuality.Broken, true, "Đã hủy thay linh kiện."));
    }

    private void BuildToolPanel(Transform parent)
    {
        TextMeshProUGUI title = MinigameUiKit.CreateText(parent, "ToolTitle", "CÔNG CỤ", 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.98f, 1f, 1f));
        MinigameUiKit.SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(440f, 42f));

        AddToolButton(parent, Tool.Iron, "MỎ HÀN", _ironSprite, new Vector2(0f, -122f), new Color(0.48f, 0.16f, 0.1f, 1f));
        AddToolButton(parent, Tool.Pump, "HÚT THIẾC", _pumpSprite, new Vector2(0f, -194f), new Color(0.1f, 0.32f, 0.5f, 1f));
        AddToolButton(parent, Tool.Tweezers, "NHÍP", _tweezersSprite, new Vector2(0f, -266f), new Color(0.18f, 0.22f, 0.28f, 1f));
        AddToolButton(parent, Tool.Replacement, "LINH KIỆN MỚI", _partSprite, new Vector2(0f, -338f), new Color(0.12f, 0.38f, 0.24f, 1f));

        _rotateButton = MinigameUiKit.CreateButton(parent, "RotateButton", "XOAY 90°", _panelSprite, new Color(0.24f, 0.18f, 0.46f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-112f, -416f), new Vector2(200f, 52f), RotateReplacement);
        _orientationText = MinigameUiKit.CreateText(parent, "Orientation", "", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.72f, 1f));
        MinigameUiKit.SetAnchored(_orientationText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(112f, -416f), new Vector2(210f, 40f));

        _selectedToolText = MinigameUiKit.CreateText(parent, "SelectedTool", "", 19, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.72f, 0.9f, 1f, 1f));
        MinigameUiKit.SetAnchored(_selectedToolText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(440f, 34f));

        _toolHintText = MinigameUiKit.CreateText(parent, "ToolHint", "", 18, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.8f, 0.9f, 0.92f, 1f));
        MinigameUiKit.SetAnchored(_toolHintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(430f, 110f));

        TextMeshProUGUI heatLabel = MinigameUiKit.CreateText(parent, "HeatLabel", "Nhiet/hu hong board", 18, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.92f, 0.96f, 1f, 1f));
        MinigameUiKit.SetAnchored(heatLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -690f), new Vector2(430f, 30f));
        Image heatBg = MinigameUiKit.CreatePanel(parent, "HeatBarBg", _panelSprite, new Color(0.06f, 0.07f, 0.08f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -735f), new Vector2(430f, 34f));
        _heatBar = MinigameUiKit.CreateImage(heatBg.transform, "Fill", _solidSprite, new Color(1f, 0.44f, 0.2f, 1f), false);
        MinigameUiKit.SetAnchored(_heatBar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 34f));
        _heatBar.rectTransform.pivot = new Vector2(0f, 0.5f);
    }

    private void AddToolButton(Transform parent, Tool tool, string label, Sprite icon, Vector2 position, Color color)
    {
        Button button = MinigameUiKit.CreateButton(parent, "Tool_" + tool, label, _panelSprite, color, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(440f, 56f), () => SelectTool(tool));
        Image iconImage = MinigameUiKit.CreateImage(button.transform, "Icon", icon, Color.white, false);
        MinigameUiKit.SetAnchored(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(44f, 44f));

        Transform labelTransform = button.transform.Find("Label");
        if (labelTransform != null)
        {
            TextMeshProUGUI text = labelTransform.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                MinigameUiKit.SetAnchored(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(36f, 0f), new Vector2(-78f, 48f));
                text.alignment = TextAlignmentOptions.Center;
            }
        }

        _toolButtons[tool] = button;
    }

    private void BuildTasks()
    {
        _tasks.Clear();

        int count = Mathf.Clamp(_difficultyLevel, 1, 3);
        List<ComponentKind> kinds = new List<ComponentKind>();
        if (_difficultyLevel <= 1)
        {
            kinds.Add(ComponentKind.Resistor);
        }
        else if (_difficultyLevel == 2)
        {
            kinds.Add(ComponentKind.Capacitor);
            kinds.Add(ComponentKind.Resistor);
        }
        else
        {
            kinds.Add(ComponentKind.Ic);
            kinds.Add(ComponentKind.Capacitor);
            kinds.Add(UnityEngine.Random.value > 0.5f ? ComponentKind.Diode : ComponentKind.Resistor);
        }

        List<Vector2> positions = new List<Vector2>
        {
            new Vector2(-300f, -28f),
            new Vector2(0f, 70f),
            new Vector2(310f, -48f)
        };

        for (int i = 0; i < count; i++)
        {
            ComponentTask task = CreateTask(i, kinds[i], positions[i]);
            _tasks.Add(task);
        }
    }

    private ComponentTask CreateTask(int index, ComponentKind kind, Vector2 position)
    {
        ComponentTask task = new ComponentTask();
        task.Id = "CMP" + (index + 1);
        task.Kind = kind;
        task.Position = position;
        task.CorrectRotation = 0;

        switch (kind)
        {
            case ComponentKind.Resistor:
                task.Label = "R" + (index + 1);
                task.Size = new Vector2(150f, 66f);
                task.Pins.Add(CreatePin(new Vector2(-86f, 0f)));
                task.Pins.Add(CreatePin(new Vector2(86f, 0f)));
                break;
            case ComponentKind.Capacitor:
                task.Label = "C" + (index + 1) + " +/-";
                task.Size = new Vector2(116f, 132f);
                task.NeedsOrientation = true;
                task.Pins.Add(CreatePin(new Vector2(-42f, -78f)));
                task.Pins.Add(CreatePin(new Vector2(42f, -78f)));
                break;
            case ComponentKind.Diode:
                task.Label = "D" + (index + 1) + " |<";
                task.Size = new Vector2(150f, 66f);
                task.NeedsOrientation = true;
                task.Pins.Add(CreatePin(new Vector2(-86f, 0f)));
                task.Pins.Add(CreatePin(new Vector2(86f, 0f)));
                break;
            case ComponentKind.Ic:
                task.Label = "IC" + (index + 1);
                task.Size = new Vector2(190f, 138f);
                task.NeedsOrientation = true;
                for (int i = 0; i < 4; i++)
                {
                    float y = -48f + (i * 32f);
                    task.Pins.Add(CreatePin(new Vector2(-112f, y)));
                    task.Pins.Add(CreatePin(new Vector2(112f, y)));
                }
                break;
        }

        return task;
    }

    private PinData CreatePin(Vector2 offset)
    {
        return new PinData { Offset = offset };
    }

    private void RebuildBoard()
    {
        ClearChildren(_boardLayer);
        ClearChildren(_traceLayer);
        ClearChildren(_componentLayer);

        for (int i = 0; i < _tasks.Count; i++)
        {
            ComponentTask task = _tasks[i];
            DrawTaskTraces(task);
            BuildTaskView(task);
        }
    }

    private void DrawTaskTraces(ComponentTask task)
    {
        for (int i = 0; i < task.Pins.Count; i++)
        {
            Vector2 pinPos = task.Position + task.Pins[i].Offset;
            Vector2 traceEnd = pinPos + new Vector2((pinPos.x < 0f ? -1f : 1f) * 72f, UnityEngine.Random.Range(-45f, 45f));
            Image trace = MinigameUiKit.CreateLine(_traceLayer, "Trace_" + task.Id + "_" + i, _solidSprite, new Color(0.08f, 0.62f, 0.43f, 0.9f), pinPos, traceEnd, 8f);
            trace.transform.SetAsFirstSibling();
        }
    }

    private void BuildTaskView(ComponentTask task)
    {
        Image scorch = MinigameUiKit.CreateImage(_componentLayer, "Burn_" + task.Id, _burnSprite, new Color(1f, 1f, 1f, 0.52f), false);
        MinigameUiKit.SetAnchored(scorch.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), task.Position, task.Size * 1.55f);

        Image body = MinigameUiKit.CreateImage(_componentLayer, "Body_" + task.Id, CreateComponentSprite(task.Kind, true), Color.white);
        MinigameUiKit.SetAnchored(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), task.Position, task.Size);
        Button bodyButton = body.gameObject.AddComponent<Button>();
        bodyButton.onClick.AddListener(() => HandleBodyClicked(task));
        task.BodyImage = body;
        task.BodyButton = bodyButton;

        TextMeshProUGUI label = MinigameUiKit.CreateText(body.transform, "Label", task.Label, 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        MinigameUiKit.Stretch(label.rectTransform);
        label.raycastTarget = false;

        for (int i = 0; i < task.Pins.Count; i++)
        {
            PinData pin = task.Pins[i];
            Image pinImage = MinigameUiKit.CreateImage(_componentLayer, "Pin_" + task.Id + "_" + i, _pinSprite, Color.white);
            MinigameUiKit.SetAnchored(pinImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), task.Position + pin.Offset, new Vector2(42f, 42f));
            Button pinButton = pinImage.gameObject.AddComponent<Button>();
            int pinIndex = i;
            pinButton.onClick.AddListener(() => HandlePinClicked(task, task.Pins[pinIndex]));
            pin.Image = pinImage;
            pin.Button = pinButton;

            pin.Label = MinigameUiKit.CreateText(pinImage.transform, "PinLabel", (i + 1).ToString(), 13, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.12f, 0.08f, 0.03f, 1f));
            MinigameUiKit.Stretch(pin.Label.rectTransform);
            pin.Label.raycastTarget = false;
        }

        task.StatusText = MinigameUiKit.CreateText(_componentLayer, "Status_" + task.Id, "Cháy: làm chảy thiếc", 15, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.98f, 0.78f, 0.42f, 1f));
        MinigameUiKit.SetAnchored(task.StatusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), task.Position + new Vector2(0f, -(task.Size.y * 0.5f) - 34f), new Vector2(250f, 32f));
    }

    private void HandlePinClicked(ComponentTask task, PinData pin)
    {
        if (!IsActive || _isFinishing || task.Complete)
        {
            return;
        }

        if (_selectedTool == Tool.Iron)
        {
            if (!task.Removed)
            {
                HeatPin(task, pin);
            }
            else if (task.Placed)
            {
                SolderPin(task, pin);
            }
            else
            {
                RegisterMistake("Chưa đặt linh kiện mới, không thể hàn lại.");
            }
        }
        else if (_selectedTool == Tool.Pump)
        {
            PumpPin(task, pin);
        }
        else
        {
            RegisterMistake("Công cụ này không dùng trên chân hàn.");
        }

        UpdateTaskVisual(task);
        UpdateStatusText();
    }

    private void HandleBodyClicked(ComponentTask task)
    {
        if (!IsActive || _isFinishing || task.Complete)
        {
            return;
        }

        if (_selectedTool == Tool.Tweezers)
        {
            RemoveComponent(task);
        }
        else if (_selectedTool == Tool.Replacement)
        {
            PlaceReplacement(task);
        }
        else
        {
            RegisterMistake("Hãy thao tác trên chân hàn với công cụ này.");
        }

        UpdateTaskVisual(task);
        UpdateStatusText();
    }

    private void HeatPin(ComponentTask task, PinData pin)
    {
        if (pin.Cleaned)
        {
            RegisterMistake("Chân này đã được hút thiếc rồi.");
            _heatDamage += 5f;
            return;
        }

        if (pin.Melted)
        {
            _heatDamage += 9f + _difficultyLevel;
            MinigameSfxKit.Play(MinigameSfxCue.Error, 0.56f);
            ShowFeedback("Quá nhiệt trên chân " + task.Label + ". Dùng hút thiếc đi.", new Color(1f, 0.56f, 0.28f, 1f));
            CheckHeatFailure();
            return;
        }

        pin.Heat = 1f;
        pin.Melted = true;
        _heatDamage += 1.8f + (_difficultyLevel * 0.5f);
        MinigameSfxKit.Play(MinigameSfxCue.Solder, 0.58f);
        ShowFeedback("Thiếc đã chảy trên " + task.Label + ". Dùng hút thiếc để dọn chân.", new Color(1f, 0.78f, 0.34f, 1f));
        CheckHeatFailure();
    }

    private void PumpPin(ComponentTask task, PinData pin)
    {
        if (!pin.Melted)
        {
            RegisterMistake("Thiếc chưa chảy, hút thiếc không có tác dụng.");
            return;
        }

        if (pin.Cleaned)
        {
            RegisterMistake("Chân này đã sạch rồi.");
            return;
        }

        pin.Cleaned = true;
        MinigameSfxKit.Play(MinigameSfxCue.Pump, 0.66f);
        ShowFeedback("Đã hút sạch thiếc trên " + task.Label + ".", new Color(0.58f, 0.92f, 1f, 1f));
    }

    private void RemoveComponent(ComponentTask task)
    {
        if (task.Removed)
        {
            RegisterMistake("Linh kiện cũ đã được gắp ra.");
            return;
        }

        if (!AllPinsCleaned(task))
        {
            RegisterMistake("Còn chân chưa hút thiếc, gắp lúc này sẽ bong pad.");
            _heatDamage += 7f;
            CheckHeatFailure();
            return;
        }

        task.Removed = true;
        MinigameSfxKit.Play(MinigameSfxCue.Tweezers, 0.64f);
        task.BodyImage.sprite = CreateComponentSprite(task.Kind, false);
        task.BodyImage.color = new Color(1f, 1f, 1f, 0.18f);
        ShowFeedback("Đã gắp " + task.Label + " cháy ra. Chọn linh kiện mới để đặt vào.", new Color(0.72f, 0.9f, 1f, 1f));
    }

    private void PlaceReplacement(ComponentTask task)
    {
        if (!task.Removed)
        {
            RegisterMistake("Chưa tháo linh kiện cháy ra.");
            return;
        }

        if (task.Placed)
        {
            RegisterMistake("Linh kiện mới đã đặt rồi. Hãy hàn lại chân.");
            return;
        }

        if (task.NeedsOrientation && NormalizeAngle(_replacementRotation) != task.CorrectRotation)
        {
            RegisterMistake("Sai chiều linh kiện. Xoay về 0 độ rồi đặt lại.");
            if (_difficultyLevel >= 4)
            {
                _heatDamage += 12f;
            }
            return;
        }

        task.Placed = true;
        MinigameSfxKit.Play(MinigameSfxCue.PlacePart, 0.62f);
        task.BodyImage.sprite = CreateComponentSprite(task.Kind, false);
        task.BodyImage.color = Color.white;
        task.BodyImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, task.CorrectRotation);
        for (int i = 0; i < task.Pins.Count; i++)
        {
            task.Pins[i].Soldered = false;
        }

        ShowFeedback("Đã đặt linh kiện mới vào " + task.Label + ". Dùng mỏ hàn để hàn lại từng chân.", new Color(0.48f, 1f, 0.62f, 1f));
    }

    private void SolderPin(ComponentTask task, PinData pin)
    {
        if (!pin.Cleaned)
        {
            RegisterMistake("Chân chưa sạch, mối hàn sẽ bị lỗi.");
            return;
        }

        if (pin.Soldered)
        {
            _heatDamage += 7f;
            RegisterMistake("Chân này đã hàn rồi, đang quá nhiệt.");
            CheckHeatFailure();
            return;
        }

        pin.Soldered = true;
        _heatDamage += 1.2f + (_difficultyLevel * 0.35f);
        MinigameSfxKit.Play(MinigameSfxCue.Solder, 0.5f, 1.12f);
        ShowFeedback("Mối hàn sáng trên " + task.Label + ".", new Color(0.64f, 0.98f, 1f, 1f));

        if (AllPinsSoldered(task))
        {
            task.Complete = true;
            ShowFeedback(task.Label + " da thay xong.", new Color(0.42f, 1f, 0.7f, 1f));
            if (AllTasksComplete())
            {
                Complete(EvaluateQuality(), true, GetResultText(EvaluateQuality()));
            }
        }

        CheckHeatFailure();
    }

    private void SelectTool(Tool tool)
    {
        _selectedTool = tool;
        if (IsActive)
        {
            MinigameSfxKit.Play(MinigameSfxCue.Select, 0.42f);
        }

        foreach (KeyValuePair<Tool, Button> pair in _toolButtons)
        {
            Image image = pair.Value.GetComponent<Image>();
            if (image != null)
            {
                Color buttonColor = pair.Key == tool ? Color.Lerp(GetToolColor(pair.Key), Color.white, 0.24f) : GetToolColor(pair.Key);
                image.color = buttonColor;
                MinigameUiKit.ConfigureButtonColors(pair.Value, buttonColor);
            }
        }

        if (_selectedToolText != null)
        {
            _selectedToolText.text = "Đang chọn: " + GetToolName(tool);
        }

        if (_toolHintText != null)
        {
            _toolHintText.text = GetToolHint(tool);
        }

        UpdateOrientationText();
    }

    private void RotateReplacement()
    {
        _replacementRotation = NormalizeAngle(_replacementRotation + 90);
        MinigameSfxKit.Play(MinigameSfxCue.Rotate, 0.5f);
        UpdateOrientationText();
        ShowFeedback("Huong linh kien moi: " + _replacementRotation + " do.", new Color(0.82f, 0.88f, 1f, 1f));
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

    private void RegisterMistake(string message)
    {
        _mistakes++;
        MinigameSfxKit.Play(MinigameSfxCue.Error, 0.64f);
        ShowFeedback(message, new Color(1f, 0.48f, 0.28f, 1f));
        if (_mistakes >= _mistakeLimit)
        {
            Complete(RepairQuality.Broken, true, "Sai thao tac qua nhieu, board bi hu.");
        }
    }

    private void CheckHeatFailure()
    {
        if (_heatDamage >= maxHeatDamage)
        {
            Complete(RepairQuality.Broken, true, "Quá nhiệt, pad trên board đã bong.");
        }
    }

    private void UpdateTaskVisual(ComponentTask task)
    {
        for (int i = 0; i < task.Pins.Count; i++)
        {
            PinData pin = task.Pins[i];
            if (pin.Image == null)
            {
                continue;
            }

            if (pin.Soldered)
            {
                pin.Image.sprite = _pinSolderSprite;
            }
            else if (pin.Cleaned)
            {
                pin.Image.sprite = _pinCleanSprite;
            }
            else if (pin.Melted)
            {
                pin.Image.sprite = _pinMeltedSprite;
            }
            else
            {
                pin.Image.sprite = _pinSprite;
            }
        }

        if (task.StatusText != null)
        {
            task.StatusText.text = GetTaskStatus(task);
            task.StatusText.color = task.Complete ? new Color(0.46f, 1f, 0.62f, 1f) : new Color(0.98f, 0.82f, 0.42f, 1f);
        }
    }

    private string GetTaskStatus(ComponentTask task)
    {
        if (task.Complete)
        {
            return "Đã thay xong";
        }

        if (!AllPinsMelted(task))
        {
            return "1 Mỏ hàn từng chân";
        }

        if (!AllPinsCleaned(task))
        {
            return "2 Hút thiếc";
        }

        if (!task.Removed)
        {
            return "3 Dùng nhíp gắp ra";
        }

        if (!task.Placed)
        {
            return task.NeedsOrientation ? "4 Đặt đúng chiều" : "4 Đặt linh kiện mới";
        }

        return "5 Hàn lại chân";
    }

    private void UpdateStatusText()
    {
        int done = 0;
        for (int i = 0; i < _tasks.Count; i++)
        {
            if (_tasks[i].Complete)
            {
                done++;
            }
        }

        if (_progressText != null)
        {
            _progressText.text = $"Linh kiện {done}/{_tasks.Count}";
        }

        if (_timerText != null)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(_timeRemaining));
            _timerText.text = $"Thời gian {seconds / 60:00}:{seconds % 60:00}  |  Sai {_mistakes}/{_mistakeLimit}";
        }

        if (_heatBar != null)
        {
            _heatBar.rectTransform.sizeDelta = new Vector2(430f * Mathf.Clamp01(_heatDamage / maxHeatDamage), 34f);
        }

        if (_finishButton != null)
        {
            _finishButton.interactable = AllTasksComplete();
        }

        UpdateOrientationText();
    }

    private void UpdateOrientationText()
    {
        if (_orientationText != null)
        {
            _orientationText.text = "Huong: " + _replacementRotation + " do";
        }

        if (_rotateButton != null)
        {
            _rotateButton.interactable = IsActive && !_isFinishing;
        }
    }

    private bool AllPinsMelted(ComponentTask task)
    {
        for (int i = 0; i < task.Pins.Count; i++)
        {
            if (!task.Pins[i].Melted)
            {
                return false;
            }
        }

        return true;
    }

    private bool AllPinsCleaned(ComponentTask task)
    {
        for (int i = 0; i < task.Pins.Count; i++)
        {
            if (!task.Pins[i].Cleaned)
            {
                return false;
            }
        }

        return true;
    }

    private bool AllPinsSoldered(ComponentTask task)
    {
        for (int i = 0; i < task.Pins.Count; i++)
        {
            if (!task.Pins[i].Soldered)
            {
                return false;
            }
        }

        return true;
    }

    private bool AllTasksComplete()
    {
        for (int i = 0; i < _tasks.Count; i++)
        {
            if (!_tasks[i].Complete)
            {
                return false;
            }
        }

        return _tasks.Count > 0;
    }

    private RepairQuality EvaluateQuality()
    {
        if (!AllTasksComplete())
        {
            return RepairQuality.Broken;
        }

        float elapsed = Mathf.Max(0f, Time.time - _startedAt);
        float score = 100f;
        score -= _mistakes * 16f;
        score -= _heatDamage * 0.55f;
        score -= Mathf.Max(0f, elapsed - 58f) * 0.22f;

        if (score >= 90f && _mistakes == 0 && _heatDamage < 18f)
        {
            return RepairQuality.Perfect;
        }

        if (score >= 68f)
        {
            return RepairQuality.Good;
        }

        if (score >= 44f)
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
        yield return new WaitForSeconds(0.75f);
        FinishNow(quality);
    }

    private void FinishNow(RepairQuality quality)
    {
        ShowUI(false);
        RestoreCursor();
        OnMinigameCompleted?.Invoke(quality);
    }

    private string GetResultText(RepairQuality quality)
    {
        switch (quality)
        {
            case RepairQuality.Perfect: return "Thay linh kiện rất gọn, đúng chiều, mối hàn đẹp.";
            case RepairQuality.Good: return "Linh kiện mới đã hoạt động tốt.";
            case RepairQuality.Passable: return "Sửa được nhưng thao tác còn nóng tay.";
            default: return "Thay linh kiện thất bại: sai thao tác, quá nhiệt hoặc đặt ngược chiều.";
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

    private string GetToolName(Tool tool)
    {
        switch (tool)
        {
            case Tool.Iron: return "Mỏ hàn";
            case Tool.Pump: return "Hút thiếc";
            case Tool.Tweezers: return "Nhíp";
            case Tool.Replacement: return "Linh kiện mới";
            default: return "";
        }
    }

    private string GetToolHint(Tool tool)
    {
        switch (tool)
        {
            case Tool.Iron:
                return "Mỏ hàn: bấm vào từng chân để làm chảy thiếc cũ. Sau khi đặt linh kiện mới, dùng lại để hàn chân.";
            case Tool.Pump:
                return "Hút thiếc: chỉ dùng được sau khi chân đã nóng/chảy thiếc.";
            case Tool.Tweezers:
                return "Nhíp: gắp linh kiện cháy ra khi tất cả chân đã được hút thiếc.";
            case Tool.Replacement:
                return "Linh kiện mới: bấm vào vị trí linh kiện đã tháo. Nếu là tụ/diode/IC, xoay đúng 0 độ trước khi đặt.";
            default:
                return "";
        }
    }

    private Color GetToolColor(Tool tool)
    {
        switch (tool)
        {
            case Tool.Iron: return new Color(0.48f, 0.16f, 0.1f, 1f);
            case Tool.Pump: return new Color(0.1f, 0.32f, 0.5f, 1f);
            case Tool.Tweezers: return new Color(0.18f, 0.22f, 0.28f, 1f);
            case Tool.Replacement: return new Color(0.12f, 0.38f, 0.24f, 1f);
            default: return Color.gray;
        }
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

    private int NormalizeAngle(int value)
    {
        int angle = value % 360;
        if (angle < 0)
        {
            angle += 360;
        }

        return angle;
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

    private void EnsureSprites()
    {
        if (_solidSprite != null)
        {
            return;
        }

        _solidSprite = MinigameUiKit.CreateSolidSprite(Color.white);
        _panelSprite = MinigameUiKit.CreateRoundedRectSprite(128, 128, 18, Color.white, new Color(1f, 1f, 1f, 0.18f));
        _pinSprite = MinigameUiKit.CreateCircleSprite(64, new Color(0.82f, 0.64f, 0.26f, 1f), new Color(0.2f, 0.13f, 0.04f, 1f), 8);
        _pinMeltedSprite = MinigameUiKit.CreateCircleSprite(64, new Color(1f, 0.72f, 0.25f, 1f), new Color(1f, 0.32f, 0.12f, 1f), 8);
        _pinCleanSprite = MinigameUiKit.CreateCircleSprite(64, new Color(0.58f, 0.68f, 0.72f, 1f), new Color(0.92f, 0.96f, 1f, 1f), 8);
        _pinSolderSprite = MinigameUiKit.CreateCircleSprite(64, new Color(0.82f, 0.9f, 0.96f, 1f), new Color(0.35f, 0.85f, 1f, 1f), 8);
        _burnSprite = CreateBurnSprite();
        _boardSprite = CreateBoardSprite();
        _ironSprite = CreateIronSprite();
        _pumpSprite = CreatePumpSprite();
        _tweezersSprite = CreateTweezersSprite();
        _partSprite = CreatePartIconSprite();
    }

    private Sprite CreateBoardSprite()
    {
        Texture2D texture = new Texture2D(960, 580, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0.025f, 0.29f, 0.2f, 1f));
        MinigameUiKit.FillRounded(texture, 18, 18, 942, 562, 28, new Color(0.035f, 0.34f, 0.24f, 1f), new Color(0.52f, 0.86f, 0.72f, 0.5f));
        for (int i = 0; i < 18; i++)
        {
            int y = 54 + i * 28;
            MinigameUiKit.DrawLine(texture, 48, y, 912, y + ((i % 2 == 0) ? 14 : -14), new Color(0.08f, 0.5f, 0.36f, 0.55f), 2);
        }

        for (int i = 0; i < 36; i++)
        {
            int x = 68 + ((i * 97) % 820);
            int y = 66 + ((i * 57) % 450);
            MinigameUiKit.FillCircle(texture, x, y, 8, new Color(0.76f, 0.61f, 0.24f, 1f));
            MinigameUiKit.FillCircle(texture, x, y, 4, new Color(0.035f, 0.34f, 0.24f, 1f));
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateBurnSprite()
    {
        Texture2D texture = new Texture2D(160, 140, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.FillCircle(texture, 76, 70, 48, new Color(0.02f, 0.012f, 0.006f, 0.78f));
        MinigameUiKit.FillCircle(texture, 95, 58, 30, new Color(0.24f, 0.08f, 0.02f, 0.58f));
        MinigameUiKit.FillCircle(texture, 55, 86, 26, new Color(0f, 0f, 0f, 0.55f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateComponentSprite(ComponentKind kind, bool burned)
    {
        Texture2D texture = new Texture2D(220, 160, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));
        Color body = burned ? new Color(0.06f, 0.045f, 0.035f, 1f) : new Color(0.14f, 0.23f, 0.42f, 1f);
        Color metal = burned ? new Color(0.28f, 0.24f, 0.2f, 1f) : new Color(0.84f, 0.88f, 0.9f, 1f);
        Color accent = burned ? new Color(0.85f, 0.17f, 0.05f, 0.6f) : new Color(0.36f, 0.9f, 1f, 1f);

        switch (kind)
        {
            case ComponentKind.Resistor:
                MinigameUiKit.FillRect(texture, 0, 76, 220, 84, metal);
                MinigameUiKit.FillRounded(texture, 52, 48, 168, 112, 24, burned ? new Color(0.22f, 0.12f, 0.06f, 1f) : new Color(0.8f, 0.62f, 0.32f, 1f), new Color(0f, 0f, 0f, 0.25f));
                MinigameUiKit.FillRect(texture, 76, 50, 84, 110, burned ? accent : new Color(0.18f, 0.1f, 0.04f, 1f));
                MinigameUiKit.FillRect(texture, 102, 50, 110, 110, burned ? accent : new Color(0.86f, 0.04f, 0.04f, 1f));
                MinigameUiKit.FillRect(texture, 128, 50, 136, 110, burned ? accent : new Color(1f, 0.78f, 0.1f, 1f));
                break;
            case ComponentKind.Capacitor:
                MinigameUiKit.FillRounded(texture, 72, 22, 148, 138, 26, body, new Color(0f, 0f, 0f, 0.25f));
                MinigameUiKit.FillRect(texture, 105, 30, 116, 130, accent);
                MinigameUiKit.FillRect(texture, 82, 138, 90, 160, metal);
                MinigameUiKit.FillRect(texture, 130, 138, 138, 160, metal);
                break;
            case ComponentKind.Diode:
                MinigameUiKit.FillRect(texture, 0, 76, 220, 84, metal);
                MinigameUiKit.FillRounded(texture, 58, 52, 162, 108, 12, burned ? new Color(0.06f, 0.055f, 0.045f, 1f) : new Color(0.08f, 0.09f, 0.1f, 1f), new Color(0f, 0f, 0f, 0.25f));
                MinigameUiKit.FillRect(texture, 124, 52, 136, 108, burned ? accent : new Color(0.94f, 0.94f, 0.82f, 1f));
                break;
            case ComponentKind.Ic:
                MinigameUiKit.FillRounded(texture, 50, 30, 170, 130, 12, burned ? new Color(0.025f, 0.023f, 0.02f, 1f) : new Color(0.025f, 0.03f, 0.04f, 1f), new Color(0f, 0f, 0f, 0.3f));
                for (int i = 0; i < 4; i++)
                {
                    int y = 45 + i * 24;
                    MinigameUiKit.FillRect(texture, 30, y, 50, y + 8, metal);
                    MinigameUiKit.FillRect(texture, 170, y, 190, y + 8, metal);
                }
                MinigameUiKit.FillCircle(texture, 72, 52, 7, accent);
                break;
        }

        if (burned)
        {
            MinigameUiKit.FillCircle(texture, 112, 78, 34, new Color(0f, 0f, 0f, 0.42f));
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateIronSprite()
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.DrawLine(texture, 20, 72, 62, 30, new Color(0.16f, 0.18f, 0.2f, 1f), 18);
        MinigameUiKit.DrawLine(texture, 56, 34, 82, 14, new Color(0.9f, 0.84f, 0.72f, 1f), 8);
        MinigameUiKit.FillCircle(texture, 82, 14, 5, new Color(1f, 0.36f, 0.14f, 1f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreatePumpSprite()
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.FillRounded(texture, 22, 24, 62, 76, 10, new Color(0.1f, 0.45f, 0.72f, 1f), new Color(0.75f, 0.9f, 1f, 1f));
        MinigameUiKit.FillRect(texture, 38, 10, 46, 24, new Color(0.8f, 0.9f, 0.96f, 1f));
        MinigameUiKit.FillRect(texture, 62, 46, 86, 54, new Color(0.82f, 0.86f, 0.9f, 1f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateTweezersSprite()
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.DrawLine(texture, 28, 18, 58, 82, new Color(0.86f, 0.9f, 0.94f, 1f), 7);
        MinigameUiKit.DrawLine(texture, 42, 18, 68, 82, new Color(0.62f, 0.7f, 0.76f, 1f), 7);
        MinigameUiKit.DrawLine(texture, 58, 82, 50, 88, new Color(0.9f, 0.94f, 0.96f, 1f), 4);
        MinigameUiKit.DrawLine(texture, 68, 82, 76, 88, new Color(0.9f, 0.94f, 0.96f, 1f), 4);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreatePartIconSprite()
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.FillRounded(texture, 24, 30, 72, 66, 12, new Color(0.12f, 0.56f, 0.36f, 1f), new Color(0.72f, 1f, 0.82f, 1f));
        MinigameUiKit.FillRect(texture, 8, 45, 24, 51, new Color(0.86f, 0.88f, 0.9f, 1f));
        MinigameUiKit.FillRect(texture, 72, 45, 88, 51, new Color(0.86f, 0.88f, 0.9f, 1f));
        MinigameUiKit.FillCircle(texture, 36, 48, 5, new Color(0.92f, 1f, 0.9f, 1f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f);
    }
}
