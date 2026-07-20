using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class PolarityWiringMinigame : MonoBehaviour, IMinigame
{
    private sealed class WireTask
    {
        public string Id;
        public string WireLabel;
        public string TerminalLabel;
        public Color WireColor;
        public Vector2 WirePosition;
        public Vector2 TerminalPosition;
        public Button WireButton;
        public Button TerminalButton;
        public Image WireImage;
        public Image TerminalImage;
        public TextMeshProUGUI StatusText;
        public bool Connected;
    }

    private sealed class WireDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PolarityWiringMinigame owner;
        private WireTask task;

        public void Configure(PolarityWiringMinigame newOwner, WireTask newTask)
        {
            owner = newOwner;
            task = newTask;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.BeginWireDrag(task, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.DragWire(task, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.EndWireDrag(task, eventData);
        }
    }


    public string MinigameName => "\u0110\u1ea5u d\u00e2y \u0111\u00fang c\u1ef1c";
    public bool IsActive { get; private set; }
    public event Action<RepairQuality> OnMinigameCompleted;

    [Header("Rules")]
    [SerializeField] private float baseTimeLimit = 75f;
    [SerializeField] private int baseMaxMistakes = 5;

    private readonly List<WireTask> _tasks = new List<WireTask>();
    private readonly List<Image> _connectedLines = new List<Image>();

    private GameObject _uiRoot;
    private RectTransform _boardRect;
    private Transform _wireLayer;
    private Transform _lineLayer;
    private Transform _terminalLayer;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _timerText;
    private TextMeshProUGUI _feedbackText;
    private TextMeshProUGUI _progressText;
    private Button _submitButton;

    private Sprite _solidSprite;
    private Sprite _panelSprite;
    private Sprite _terminalSprite;
    private Sprite _wireSprite;

    private WireTask _selectedTask;
    private WireTask _draggingTask;
    private Image _dragShadow;
    private Image _dragLine;
    private Image _dragShine;
    private Image _dragPlugGhost;

    private int _difficultyLevel = 1;
    private int _mistakes;
    private int _maxMistakes;
    private float _timeRemaining;
    private float _startedAt;
    private bool _isFinishing;
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    private const float TerminalSnapRadius = 88f;


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

        // _timeRemaining -= Time.deltaTime;
        UpdateStatusText();

        if (_timeRemaining <= 0f)
        {
            Fail("\u1ea4y ch\u1eadm qu\u00e1, kh\u00e1ch c\u1eafm l\u1ea1i l\u00e0 d\u1ec5 ch\u1eadp m\u1ea1ch.");
        }
    }

    public void Initialize(List<string> faults, int difficultyLevel)
    {
        EnsureUI();

        _difficultyLevel = Mathf.Clamp(difficultyLevel, 1, 5);
        _mistakes = 0;
        _maxMistakes = Mathf.Max(2, baseMaxMistakes - Mathf.Max(0, _difficultyLevel - 2));
        _timeRemaining = Mathf.Max(40f, baseTimeLimit - ((_difficultyLevel - 1) * 8f));
        _isFinishing = false;
        _selectedTask = null;

        BuildTasks(faults);
        RebuildBoard();
        UpdateStatusText();
        ShowFeedback("Giữ chuột kéo đầu dây bên trái sang đúng cọc bên phải, thả gần cọc để snap vào.", new Color(0.78f, 0.9f, 1f, 1f));
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
            SubtitleManager.Instance.ShowSubtitle("Anh Th\u1ee3 \u0110i\u1ec7n", "N\u1ed1i \u0111\u00fang m\u00e0u v\u00e0 \u0111\u00fang c\u1ef1c. Sai c\u1ef1c l\u00e0 d\u1ec5 ch\u00e1y linh ki\u1ec7n.", 3.5f, "Ti\u1ebfng \u0111\u1eb7t \u0111\u1ed3");
        }
    }

    public RepairQuality EndMinigame()
    {
        if (!IsActive)
        {
            return RepairQuality.Broken;
        }

        RepairQuality quality = EvaluateQuality();
        Complete(quality, false);
        return quality;
    }

    public void AbortMinigame()
    {
        Complete(RepairQuality.Broken, false);
    }

    private void EnsureUI()
    {
        if (_uiRoot != null)
        {
            return;
        }

        _solidSprite = MinigameUiKit.CreateSolidSprite(Color.white);
        _panelSprite = MinigameUiKit.CreateRoundedRectSprite(128, 128, 18, Color.white, new Color(1f, 1f, 1f, 0.18f));
        _terminalSprite = CreateTerminalSocketSprite();
        _wireSprite = CreateWirePlugSprite();

        _uiRoot = MinigameUiKit.CreateCanvasRoot("PolarityWiringUI", transform, 510);
        MinigameWorkbenchVisuals.Install(_uiRoot, MinigameWorkbenchStyle.Wiring, new Color(0.1f, 0.72f, 1f, 1f));


        Image overlay = MinigameUiKit.CreateImage(_uiRoot.transform, "BackgroundOverlay", _solidSprite, new Color(0.006f, 0.008f, 0.012f, 0.92f), false);
        MinigameUiKit.Stretch(overlay.rectTransform);
        overlay.transform.SetAsFirstSibling();

        Image header = MinigameUiKit.CreatePanel(_uiRoot.transform, "Header", _panelSprite, new Color(0.035f, 0.04f, 0.046f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(1760f, 72f));
        MinigameUiKit.AddChrome(header.transform, _solidSprite, new Color(0.1f, 0.72f, 1f, 0.95f));
        _titleText = MinigameUiKit.CreateText(header.transform, "Title", "\u0110\u1ea4U D\u00c2Y \u0110\u00daNG C\u1ef0C", 28, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.95f, 0.98f, 1f, 1f));
        MinigameUiKit.SetAnchored(_titleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(342f, 0f), new Vector2(620f, 48f));

        _timerText = MinigameUiKit.CreateText(header.transform, "Timer", "", 23, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.72f, 0.92f, 1f, 1f));
        MinigameUiKit.SetAnchored(_timerText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-322f, 0f), new Vector2(580f, 48f));

        _progressText = MinigameUiKit.CreateText(header.transform, "Progress", "", 23, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.32f, 1f));
        MinigameUiKit.SetAnchored(_progressText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 48f));

        Image boardPanel = MinigameUiKit.CreatePanel(_uiRoot.transform, "BoardPanel", _panelSprite, new Color(0.019f, 0.024f, 0.028f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(1580f, 820f));
        MinigameUiKit.AddChrome(boardPanel.transform, _solidSprite, new Color(0.06f, 0.52f, 0.88f, 0.55f));
        _boardRect = boardPanel.rectTransform;

        Image guidePanel = MinigameUiKit.CreatePanel(boardPanel.transform, "GuidePanel", _panelSprite, new Color(0.032f, 0.045f, 0.05f, 0.94f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(1140f, 62f));
        MinigameUiKit.AddChrome(guidePanel.transform, _solidSprite, new Color(0.1f, 0.72f, 1f, 0.5f));
        TextMeshProUGUI guideText = MinigameUiKit.CreateText(guidePanel.transform, "GuideText", "Hướng dẫn: giữ chuột kéo đầu dây bên trái sang đúng cọc bên phải. Thả gần cọc để snap vào, sai cực sẽ tính lỗi.", 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.84f, 0.94f, 0.92f, 1f));
        MinigameUiKit.Stretch(guideText.rectTransform);

        TextMeshProUGUI leftLabel = MinigameUiKit.CreateText(boardPanel.transform, "WireLabel", "D\u00c2Y R\u1edcI", 23, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.96f, 0.92f, 1f));
        MinigameUiKit.SetAnchored(leftLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -42f), new Vector2(360f, 42f));
        TextMeshProUGUI rightLabel = MinigameUiKit.CreateText(boardPanel.transform, "TerminalLabel", "C\u1eccC THI\u1ebeT B\u1eca", 23, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.96f, 0.92f, 1f));
        MinigameUiKit.SetAnchored(rightLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-250f, -42f), new Vector2(420f, 42f));

        Image device = MinigameUiKit.CreatePanel(boardPanel.transform, "DeviceBody", _panelSprite, new Color(0.08f, 0.1f, 0.12f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 540f));
        MinigameUiKit.AddChrome(device.transform, _solidSprite, new Color(0.1f, 0.72f, 1f, 0.35f));
        TextMeshProUGUI deviceText = MinigameUiKit.CreateText(device.transform, "DeviceText", "B\u1ed8 NGU\u1ed2N / M\u1ea0CH T\u1ea2I", 23, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.72f, 0.86f, 0.9f, 1f));
        MinigameUiKit.SetAnchored(deviceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(440f, 44f));

        _lineLayer = MinigameUiKit.CreateUIObject("Lines", boardPanel.transform).transform;
        MinigameUiKit.Stretch(_lineLayer.GetComponent<RectTransform>());
        _wireLayer = MinigameUiKit.CreateUIObject("Wires", boardPanel.transform).transform;
        MinigameUiKit.Stretch(_wireLayer.GetComponent<RectTransform>());
        _terminalLayer = MinigameUiKit.CreateUIObject("Terminals", boardPanel.transform).transform;
        MinigameUiKit.Stretch(_terminalLayer.GetComponent<RectTransform>());

        _feedbackText = MinigameUiKit.CreateText(_uiRoot.transform, "Feedback", "", 25, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        MinigameUiKit.SetAnchored(_feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(1320f, 48f));

        _submitButton = MinigameUiKit.CreateButton(_uiRoot.transform, "FinishButton", "KI\u1ec2M TRA", _panelSprite, new Color(0.12f, 0.42f, 0.28f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 58f), new Vector2(220f, 56f), TryFinish);
        MinigameUiKit.CreateButton(_uiRoot.transform, "CancelButton", "H\u1ee6Y", _panelSprite, new Color(0.35f, 0.09f, 0.08f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(130f, 58f), new Vector2(170f, 56f), () => Fail("\u0110\u00e3 h\u1ee7y l\u01b0\u1ee3t n\u1ed1i d\u00e2y."));
        MinigameHintOverlay hintOverlay = MinigameHintOverlay.Attach(_uiRoot.transform, _panelSprite, _solidSprite, "GỢI Ý", GetHintText, new Color(0.1f, 0.72f, 1f, 1f));
        MinigameUiKit.CreateButton(_uiRoot.transform, "HintButton", "GỢI Ý", _panelSprite, new Color(0.12f, 0.24f, 0.34f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(330f, 58f), new Vector2(170f, 56f), hintOverlay.Show);
    }

    private string GetHintText()
    {
        if (_tasks.Count == 0)
        {
            return "Chưa có dây nào được tạo. Hãy bắt đầu minigame lại.";
        }

        List<string> lines = new List<string>();
        lines.Add("Cách giải lượt này: kéo từng dây vào đúng cọc bên phải.");
        lines.Add("");

        for (int i = 0; i < _tasks.Count; i++)
        {
            WireTask task = _tasks[i];
            lines.Add("- " + task.WireLabel + " -> " + task.TerminalLabel);
        }

        lines.Add("");
        lines.Add("Kéo đầu dây, thả gần đúng cọc để dây tự snap vào. Nếu nối nhầm, kéo lại dây đó sang cọc đúng rồi bấm KIỂM TRA.");
        return string.Join("\n", lines);
    }

    private void BuildTasks(List<string> faults)
    {
        _tasks.Clear();

        List<WireTask> pool = new List<WireTask>
        {
            CreateTask("BAT", "D\u00e2y \u0111\u1ecf", "+12V", new Color(0.95f, 0.08f, 0.07f, 1f)),
            CreateTask("GND", "D\u00e2y \u0111en", "GND", new Color(0.035f, 0.04f, 0.048f, 1f)),
            CreateTask("SIG", "D\u00e2y v\u00e0ng", "SIGNAL", new Color(1f, 0.78f, 0.12f, 1f)),
            CreateTask("FIVE", "D\u00e2y xanh d\u01b0\u01a1ng", "+5V", new Color(0.12f, 0.55f, 1f, 1f)),
            CreateTask("MOTOR", "D\u00e2y cam", "MOTOR+", new Color(1f, 0.42f, 0.12f, 1f)),
            CreateTask("SENSE", "D\u00e2y xanh l\u00e1", "SENSE", new Color(0.22f, 0.86f, 0.34f, 1f))
        };

        Shuffle(pool);
        int count = Mathf.Clamp(2 + _difficultyLevel, 3, pool.Count);
        for (int i = 0; i < count; i++)
        {
            _tasks.Add(pool[i]);
        }

        SortByStableOrder(_tasks);

        List<int> terminalSlots = new List<int>();
        for (int i = 0; i < _tasks.Count; i++)
        {
            terminalSlots.Add(i);
        }
        Shuffle(terminalSlots);

        float spacing = Mathf.Min(112f, 560f / Mathf.Max(1, _tasks.Count - 1));
        float topY = ((_tasks.Count - 1) * spacing) * 0.5f;
        for (int i = 0; i < _tasks.Count; i++)
        {
            _tasks[i].WirePosition = new Vector2(-560f, topY - (i * spacing));
            _tasks[i].TerminalPosition = new Vector2(560f, topY - (terminalSlots[i] * spacing));
        }
    }

    private WireTask CreateTask(string id, string wireLabel, string terminalLabel, Color color)
    {
        return new WireTask
        {
            Id = id,
            WireLabel = wireLabel,
            TerminalLabel = terminalLabel,
            WireColor = color
        };
    }

    private void RebuildBoard()
    {
        ClearChildren(_wireLayer);
        ClearChildren(_terminalLayer);
        ClearChildren(_lineLayer);
        _connectedLines.Clear();
        _draggingTask = null;
        _dragShadow = null;
        _dragLine = null;
        _dragShine = null;
        _dragPlugGhost = null;


        BuildHarnessBackdrop();

        for (int i = 0; i < _tasks.Count; i++)
        {
            WireTask task = _tasks[i];
            task.Connected = false;

            Image wireSocket = MinigameUiKit.CreateImage(_wireLayer, "Wire_" + task.Id, _wireSprite, task.WireColor);
            MinigameUiKit.SetAnchored(wireSocket.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), task.WirePosition, new Vector2(226f, 76f));
            Button wireButton = wireSocket.gameObject.AddComponent<Button>();
            wireButton.onClick.AddListener(() => SelectWire(task));
            MinigameUiKit.ConfigureButtonColors(wireButton, task.WireColor);
            wireSocket.gameObject.AddComponent<MinigameUiButtonMotion>();
            WireDragHandle dragHandle = wireSocket.gameObject.AddComponent<WireDragHandle>();
            dragHandle.Configure(this, task);

            task.WireButton = wireButton;
            task.WireImage = wireSocket;

            TextMeshProUGUI wireText = MinigameUiKit.CreateText(wireSocket.transform, "WireText", task.WireLabel, 17, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            MinigameUiKit.SetAnchored(wireText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(-84f, 46f));
            wireText.raycastTarget = false;

            Image terminal = MinigameUiKit.CreateImage(_terminalLayer, "Terminal_" + task.Id, _terminalSprite, Color.white);
            MinigameUiKit.SetAnchored(terminal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), task.TerminalPosition, new Vector2(116f, 116f));
            Button terminalButton = terminal.gameObject.AddComponent<Button>();
            terminalButton.onClick.AddListener(() => TryConnectToTerminal(task));
            MinigameUiKit.ConfigureButtonColors(terminalButton, Color.white);
            terminal.gameObject.AddComponent<MinigameUiButtonMotion>();
            task.TerminalButton = terminalButton;
            task.TerminalImage = terminal;

            TextMeshProUGUI terminalText = MinigameUiKit.CreateText(terminal.transform, "TerminalText", task.TerminalLabel, 17, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            MinigameUiKit.Stretch(terminalText.rectTransform);
            terminalText.raycastTarget = false;

            task.StatusText = MinigameUiKit.CreateText(_terminalLayer, "Status_" + task.Id, "", 15, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.8f, 0.92f, 1f, 1f));
            MinigameUiKit.SetAnchored(task.StatusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), task.TerminalPosition + new Vector2(0f, -68f), new Vector2(180f, 30f));
        }
    }

private void BuildHarnessBackdrop()
    {
        Image leftTray = MinigameUiKit.CreatePanel(_lineLayer, "WireTray", _panelSprite, new Color(0.02f, 0.027f, 0.032f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560f, 0f), new Vector2(380f, 650f));
        leftTray.raycastTarget = false;
        MinigameUiKit.AddChrome(leftTray.transform, _solidSprite, new Color(0.12f, 0.72f, 1f, 0.35f));

        Image terminalRail = MinigameUiKit.CreatePanel(_lineLayer, "TerminalRail", _panelSprite, new Color(0.028f, 0.033f, 0.036f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 0f), new Vector2(350f, 650f));
        terminalRail.raycastTarget = false;
        MinigameUiKit.AddChrome(terminalRail.transform, _solidSprite, new Color(0.95f, 0.76f, 0.28f, 0.35f));

        TextMeshProUGUI trayLabel = MinigameUiKit.CreateText(_lineLayer, "TrayLabel", "BÓ DÂY VÀO", 16, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.62f, 0.82f, 0.9f, 0.82f));
        MinigameUiKit.SetAnchored(trayLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560f, 292f), new Vector2(280f, 30f));
        trayLabel.raycastTarget = false;

        TextMeshProUGUI railLabel = MinigameUiKit.CreateText(_lineLayer, "RailLabel", "TERMINAL BLOCK", 16, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.84f, 0.42f, 0.82f));
        MinigameUiKit.SetAnchored(railLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 292f), new Vector2(280f, 30f));
        railLabel.raycastTarget = false;

        Image deviceFace = MinigameUiKit.CreatePanel(_lineLayer, "DeviceFace", _panelSprite, new Color(0.07f, 0.11f, 0.13f, 0.38f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 420f));
        deviceFace.raycastTarget = false;

        for (int i = 0; i < _tasks.Count; i++)
        {
            WireTask task = _tasks[i];
            Color cableColor = task.WireColor;
            Color cableShadow = new Color(0f, 0f, 0f, 0.28f);
            Color cableHighlight = Color.Lerp(task.WireColor, Color.white, 0.38f);
            cableHighlight.a = 0.72f;

            Vector2 cableStart = task.WirePosition + new Vector2(-220f, 0f);
            Vector2 cableEnd = task.WirePosition + new Vector2(-106f, 0f);
            MinigameUiKit.CreateLine(_lineLayer, "LooseCableShadow_" + task.Id, _solidSprite, cableShadow, cableStart + new Vector2(0f, -4f), cableEnd + new Vector2(0f, -4f), 24f).raycastTarget = false;
            MinigameUiKit.CreateLine(_lineLayer, "LooseCable_" + task.Id, _solidSprite, new Color(cableColor.r, cableColor.g, cableColor.b, 0.74f), cableStart, cableEnd, 18f).raycastTarget = false;
            MinigameUiKit.CreateLine(_lineLayer, "LooseCableShine_" + task.Id, _solidSprite, cableHighlight, cableStart + new Vector2(0f, 5f), cableEnd + new Vector2(0f, 5f), 4f).raycastTarget = false;

            Vector2 terminalStemStart = task.TerminalPosition - new Vector2(150f, 0f);
            Vector2 terminalStemEnd = task.TerminalPosition - new Vector2(78f, 0f);
            MinigameUiKit.CreateLine(_lineLayer, "TerminalStem_" + task.Id, _solidSprite, new Color(0.18f, 0.54f, 0.66f, 0.32f), terminalStemStart, terminalStemEnd, 5f).raycastTarget = false;
        }
    }

    private void SelectWire(WireTask task)
    {
        if (!IsActive || _isFinishing || task == null || task.Connected)
        {
            return;
        }

        _selectedTask = task;
        MinigameSfxKit.Play(MinigameSfxCue.WirePick, 0.56f);
        HighlightSelectedWire(task);
        ShowFeedback("Đang cầm " + task.WireLabel + ". Kéo sang cọc " + task.TerminalLabel + " rồi thả.", new Color(0.72f, 0.9f, 1f, 1f));
    }

    private void TryConnectToTerminal(WireTask terminalTask)
    {
        if (!IsActive || _isFinishing)
        {
            return;
        }

        if (_selectedTask == null)
        {
            RegisterMistake("Chưa cầm dây. Kéo một đầu dây bên trái sang cọc cần nối.");
            return;
        }

        if (_selectedTask != terminalTask)
        {
            RegisterMistake("Sai cực: " + _selectedTask.WireLabel + " không được nối vào " + terminalTask.TerminalLabel + ".");
            ResetWireSelection(_selectedTask);
            _selectedTask = null;
            return;
        }

        ConnectTask(terminalTask);
    }

    private void BeginWireDrag(WireTask task, PointerEventData eventData)
    {
        if (!IsActive || _isFinishing || task == null || task.Connected)
        {
            return;
        }

        _draggingTask = task;
        _selectedTask = task;
        MinigameSfxKit.Play(MinigameSfxCue.WirePick, 0.5f);
        HighlightSelectedWire(task);
        EnsureDragPreview();

        if (TryGetBoardLocalPoint(eventData, out Vector2 localPoint))
        {
            UpdateDragPreview(task, localPoint);
            SetDragPreviewVisible(true);
        }

        ShowFeedback("Kéo " + task.WireLabel + " tới cọc " + task.TerminalLabel + ".", new Color(0.72f, 0.9f, 1f, 1f));
    }

    private void DragWire(WireTask task, PointerEventData eventData)
    {
        if (_draggingTask == null || task != _draggingTask || task.Connected)
        {
            return;
        }

        if (TryGetBoardLocalPoint(eventData, out Vector2 localPoint))
        {
            UpdateDragPreview(task, localPoint);
            WireTask hoveredTerminal = FindTerminalAt(localPoint);
            HighlightTerminalHover(hoveredTerminal);
        }
    }

    private void EndWireDrag(WireTask task, PointerEventData eventData)
    {
        if (_draggingTask == null || task != _draggingTask)
        {
            return;
        }

        SetDragPreviewVisible(false);
        ClearTerminalHover();
        _draggingTask = null;

        if (!TryGetBoardLocalPoint(eventData, out Vector2 localPoint))
        {
            ResetWireSelection(task);
            ShowFeedback("Thả dây vào vùng cọc để nối.", new Color(1f, 0.82f, 0.34f, 1f));
            return;
        }

        WireTask terminalTask = FindTerminalAt(localPoint);
        if (terminalTask == null)
        {
            ResetWireSelection(task);
            ShowFeedback("Chưa chạm cọc nào. Kéo đầu dây vào đúng terminal block.", new Color(1f, 0.82f, 0.34f, 1f));
            return;
        }

        if (terminalTask != task)
        {
            RegisterMistake("Sai cực: " + task.WireLabel + " không được nối vào " + terminalTask.TerminalLabel + ".");
            ResetWireSelection(task);
            _selectedTask = null;
            return;
        }

        ConnectTask(task);
    }

    private void EnsureDragPreview()
    {
        if (_dragLine != null)
        {
            return;
        }

        _dragShadow = MinigameUiKit.CreateLine(_lineLayer, "DragCableShadow", _solidSprite, new Color(0f, 0f, 0f, 0.46f), Vector2.zero, Vector2.right, 24f);
        _dragLine = MinigameUiKit.CreateLine(_lineLayer, "DragCable", _solidSprite, Color.white, Vector2.zero, Vector2.right, 16f);
        _dragShine = MinigameUiKit.CreateLine(_lineLayer, "DragCableShine", _solidSprite, Color.white, Vector2.zero, Vector2.right, 5f);
        _dragShadow.raycastTarget = false;
        _dragLine.raycastTarget = false;
        _dragShine.raycastTarget = false;

        _dragPlugGhost = MinigameUiKit.CreateImage(_wireLayer, "DragPlugGhost", _wireSprite, Color.white, false);
        MinigameUiKit.SetAnchored(_dragPlugGhost.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 58f));
        SetDragPreviewVisible(false);
    }

    private void SetDragPreviewVisible(bool visible)
    {
        if (_dragShadow != null) _dragShadow.gameObject.SetActive(visible);
        if (_dragLine != null) _dragLine.gameObject.SetActive(visible);
        if (_dragShine != null) _dragShine.gameObject.SetActive(visible);
        if (_dragPlugGhost != null) _dragPlugGhost.gameObject.SetActive(visible);
    }

    private void UpdateDragPreview(WireTask task, Vector2 localPoint)
    {
        EnsureDragPreview();
        Vector2 start = WireLeadPoint(task);
        Color shineColor = Color.Lerp(task.WireColor, Color.white, 0.42f);
        MinigameUiKit.SetLine(_dragShadow.rectTransform, start + new Vector2(0f, -5f), localPoint + new Vector2(0f, -5f), 24f);
        MinigameUiKit.SetLine(_dragLine.rectTransform, start, localPoint, 16f);
        MinigameUiKit.SetLine(_dragShine.rectTransform, start + new Vector2(0f, 5f), localPoint + new Vector2(0f, 5f), 5f);
        _dragLine.color = task.WireColor;
        _dragShine.color = shineColor;
        _dragPlugGhost.color = Color.Lerp(task.WireColor, Color.white, 0.12f);
        _dragPlugGhost.rectTransform.anchoredPosition = localPoint;
        _dragShadow.transform.SetAsLastSibling();
        _dragLine.transform.SetAsLastSibling();
        _dragShine.transform.SetAsLastSibling();
        _dragPlugGhost.transform.SetAsLastSibling();
    }

    private bool TryGetBoardLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (_boardRect == null || eventData == null)
        {
            return false;
        }

        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_boardRect, eventData.position, eventCamera, out localPoint);
    }

    private WireTask FindTerminalAt(Vector2 localPoint)
    {
        WireTask best = null;
        float bestDistance = TerminalSnapRadius;
        for (int i = 0; i < _tasks.Count; i++)
        {
            WireTask task = _tasks[i];
            if (task.Connected)
            {
                continue;
            }

            float distance = Vector2.Distance(localPoint, task.TerminalPosition);
            if (distance <= bestDistance)
            {
                best = task;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void HighlightSelectedWire(WireTask selected)
    {
        for (int i = 0; i < _tasks.Count; i++)
        {
            WireTask item = _tasks[i];
            if (item.WireImage == null || item.WireButton == null || item.Connected)
            {
                continue;
            }

            Color buttonColor = item == selected ? Color.Lerp(item.WireColor, Color.white, 0.35f) : item.WireColor;
            item.WireImage.color = buttonColor;
            MinigameUiKit.ConfigureButtonColors(item.WireButton, buttonColor);
        }
    }

    private void ResetWireSelection(WireTask task)
    {
        if (task != null && task.WireImage != null && task.WireButton != null && !task.Connected)
        {
            task.WireImage.color = task.WireColor;
            MinigameUiKit.ConfigureButtonColors(task.WireButton, task.WireColor);
        }
    }

    private void HighlightTerminalHover(WireTask hovered)
    {
        for (int i = 0; i < _tasks.Count; i++)
        {
            WireTask task = _tasks[i];
            if (task.TerminalImage == null || task.Connected)
            {
                continue;
            }

            task.TerminalImage.color = task == hovered ? Color.Lerp(task.WireColor, Color.white, 0.3f) : Color.white;
        }
    }

    private void ClearTerminalHover()
    {
        for (int i = 0; i < _tasks.Count; i++)
        {
            WireTask task = _tasks[i];
            if (task.TerminalImage != null && !task.Connected)
            {
                task.TerminalImage.color = Color.white;
            }
        }
    }

    private Vector2 WireLeadPoint(WireTask task)
    {
        return task.WirePosition + new Vector2(112f, 0f);
    }

    private Vector2 TerminalLeadPoint(WireTask task)
    {
        return task.TerminalPosition - new Vector2(70f, 0f);
    }


    private void ConnectTask(WireTask task)
    {
        task.Connected = true;
        task.WireButton.interactable = false;
        task.TerminalButton.interactable = false;
        MinigameSfxKit.Play(MinigameSfxCue.WireConnect, 0.72f);
        task.WireImage.color = Color.Lerp(task.WireColor, Color.white, 0.18f);
        task.TerminalImage.color = Color.Lerp(task.WireColor, Color.white, 0.25f);
        MinigameUiKit.ConfigureButtonColors(task.WireButton, task.WireImage.color);
        MinigameUiKit.ConfigureButtonColors(task.TerminalButton, task.TerminalImage.color);
        task.StatusText.text = "\u0110\u00e3 n\u1ed1i";
        task.StatusText.color = new Color(0.36f, 1f, 0.58f, 1f);

        Vector2 start = WireLeadPoint(task);
        Vector2 end = TerminalLeadPoint(task);
        Image shadow = MinigameUiKit.CreateLine(_lineLayer, "ConnectedShadow_" + task.Id, _solidSprite, new Color(0f, 0f, 0f, 0.42f), start + new Vector2(0f, -5f), end + new Vector2(0f, -5f), 24f);
        Image line = MinigameUiKit.CreateLine(_lineLayer, "ConnectedLine_" + task.Id, _solidSprite, task.WireColor, start, end, 16f);
        Image shine = MinigameUiKit.CreateLine(_lineLayer, "ConnectedShine_" + task.Id, _solidSprite, Color.Lerp(task.WireColor, Color.white, 0.42f), start + new Vector2(0f, 5f), end + new Vector2(0f, 5f), 5f);
        shadow.raycastTarget = false;
        line.raycastTarget = false;
        shine.raycastTarget = false;
        shadow.transform.SetAsLastSibling();
        line.transform.SetAsLastSibling();
        shine.transform.SetAsLastSibling();
        _connectedLines.Add(shadow);
        _connectedLines.Add(line);
        _connectedLines.Add(shine);

        _selectedTask = null;
        ShowFeedback("\u0110\u00fang c\u1ef1c: " + task.WireLabel + " -> " + task.TerminalLabel + ".", new Color(0.46f, 1f, 0.62f, 1f));
        UpdateStatusText();

        if (AllConnected())
        {
            Complete(EvaluateQuality(), true);
        }
    }

    private void TryFinish()
    {
        if (!IsActive || _isFinishing)
        {
            return;
        }

        if (!AllConnected())
        {
            RegisterMistake("C\u00f2n d\u00e2y ch\u01b0a n\u1ed1i. Ki\u1ec3m tra l\u1ea1i tr\u01b0\u1edbc khi \u0111\u00f3ng m\u00e1y.");
            return;
        }

        Complete(EvaluateQuality(), true);
    }

    private void RegisterMistake(string message)
    {
        _mistakes++;
        MinigameSfxKit.Play(MinigameSfxCue.Error, 0.64f);
        ShowFeedback(message, new Color(1f, 0.48f, 0.28f, 1f));
        UpdateStatusText();

        if (_mistakes >= _maxMistakes)
        {
            Fail("Sai c\u1ef1c qu\u00e1 nhi\u1ec1u, m\u1ea1ch b\u1ecb ch\u1eadp.");
        }
    }

    private bool AllConnected()
    {
        for (int i = 0; i < _tasks.Count; i++)
        {
            if (!_tasks[i].Connected)
            {
                return false;
            }
        }

        return _tasks.Count > 0;
    }

    private void UpdateStatusText()
    {
        int connected = 0;
        for (int i = 0; i < _tasks.Count; i++)
        {
            if (_tasks[i].Connected)
            {
                connected++;
            }
        }

        if (_progressText != null)
        {
            _progressText.text = $"D\u00e2y {connected}/{_tasks.Count}";
        }

        if (_timerText != null)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(_timeRemaining));
            _timerText.text = $"Th\u1eddi gian {seconds / 60:00}:{seconds % 60:00}  |  Sai {_mistakes}/{_maxMistakes}";
        }

        if (_submitButton != null)
        {
            _submitButton.interactable = AllConnected();
        }
    }

    private RepairQuality EvaluateQuality()
    {
        if (!AllConnected())
        {
            return RepairQuality.Broken;
        }

        float elapsed = Mathf.Max(0f, Time.time - _startedAt);
        float score = 100f;
        score -= _mistakes * 18f;
        score -= Mathf.Max(0f, elapsed - 30f) * 0.35f;

        if (score >= 92f && _mistakes == 0)
        {
            return RepairQuality.Perfect;
        }

        if (score >= 72f)
        {
            return RepairQuality.Good;
        }

        if (score >= 48f)
        {
            return RepairQuality.Passable;
        }

        return RepairQuality.Broken;
    }

    private void Complete(RepairQuality quality, bool delay)
    {
        if (_isFinishing)
        {
            return;
        }

        _isFinishing = true;
        IsActive = false;
        MinigameSfxKit.Play(quality == RepairQuality.Broken ? MinigameSfxCue.Failure : MinigameSfxCue.Success, 0.78f);

        if (delay)
        {
            ShowFeedback(GetResultText(quality), GetResultColor(quality));
            StartCoroutine(CompleteAfterDelay(quality));
        }
        else
        {
            FinishNow(quality);
        }
    }

    private IEnumerator CompleteAfterDelay(RepairQuality quality)
    {
        yield return new WaitForSeconds(0.65f);
        FinishNow(quality);
    }

    private void FinishNow(RepairQuality quality)
    {
        ShowUI(false);
        RestoreCursor();
        OnMinigameCompleted?.Invoke(quality);
    }

    private void Fail(string reason)
    {
        ShowFeedback(reason, new Color(1f, 0.32f, 0.25f, 1f));
        Complete(RepairQuality.Broken, true);
    }

    private string GetResultText(RepairQuality quality)
    {
        switch (quality)
        {
            case RepairQuality.Perfect: return "N\u1ed1i d\u00e2y g\u1ecdn v\u00e0 \u0111\u00fang c\u1ef1c ho\u00e0n h\u1ea3o.";
            case RepairQuality.Good: return "N\u1ed1i \u0111\u00fang c\u1ef1c, m\u1ea1ch an to\u00e0n.";
            case RepairQuality.Passable: return "N\u1ed1i \u0111\u01b0\u1ee3c nh\u01b0ng thao t\u00e1c c\u00f2n l\u1ea9n c\u1ea9n.";
            default: return "N\u1ed1i sai c\u1ef1c, m\u1ea1ch b\u1ecb ch\u1eadp.";
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

    private Sprite CreateTerminalSocketSprite()
    {
        Texture2D texture = new Texture2D(140, 140, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));

        MinigameUiKit.FillRounded(texture, 16, 20, 124, 124, 18, new Color(0f, 0f, 0f, 0.34f), new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.FillRounded(texture, 12, 14, 120, 118, 18, new Color(0.82f, 0.86f, 0.88f, 1f), new Color(0.18f, 0.22f, 0.24f, 1f));
        MinigameUiKit.FillRounded(texture, 22, 24, 110, 108, 14, new Color(0.18f, 0.2f, 0.22f, 1f), new Color(1f, 1f, 1f, 0.16f));
        MinigameUiKit.FillCircle(texture, 66, 66, 38, new Color(0.05f, 0.06f, 0.068f, 1f));
        MinigameUiKit.FillCircle(texture, 66, 66, 28, new Color(0.72f, 0.76f, 0.78f, 1f));
        MinigameUiKit.FillCircle(texture, 66, 66, 18, new Color(0.045f, 0.052f, 0.058f, 1f));
        MinigameUiKit.DrawLine(texture, 43, 48, 89, 84, new Color(0.92f, 0.95f, 0.96f, 0.72f), 7);
        MinigameUiKit.DrawLine(texture, 46, 45, 92, 81, new Color(0.08f, 0.09f, 0.1f, 0.72f), 3);
        MinigameUiKit.FillRect(texture, 28, 105, 106, 111, new Color(1f, 1f, 1f, 0.18f));

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateWirePlugSprite()
    {
        Texture2D texture = new Texture2D(260, 96, TextureFormat.RGBA32, false);
        MinigameUiKit.Clear(texture, new Color(0f, 0f, 0f, 0f));

        MinigameUiKit.FillRounded(texture, 12, 34, 186, 74, 20, new Color(0f, 0f, 0f, 0.32f), new Color(0f, 0f, 0f, 0f));
        MinigameUiKit.FillRounded(texture, 8, 26, 184, 66, 20, new Color(0.92f, 0.94f, 0.95f, 1f), new Color(0.06f, 0.07f, 0.08f, 0.55f));
        MinigameUiKit.FillRounded(texture, 20, 34, 150, 58, 12, new Color(1f, 1f, 1f, 0.68f), new Color(1f, 1f, 1f, 0.12f));
        MinigameUiKit.FillRect(texture, 28, 35, 144, 40, new Color(1f, 1f, 1f, 0.28f));
        MinigameUiKit.FillRect(texture, 28, 54, 144, 58, new Color(0f, 0f, 0f, 0.18f));

        MinigameUiKit.FillRounded(texture, 166, 28, 220, 64, 8, new Color(0.48f, 0.52f, 0.55f, 1f), new Color(0.88f, 0.92f, 0.94f, 0.78f));
        MinigameUiKit.FillRounded(texture, 180, 34, 238, 58, 6, new Color(0.78f, 0.82f, 0.84f, 1f), new Color(0.18f, 0.2f, 0.22f, 0.5f));
        MinigameUiKit.FillRounded(texture, 232, 38, 258, 54, 5, new Color(0.94f, 0.96f, 0.98f, 1f), new Color(0.18f, 0.2f, 0.22f, 0.4f));
        MinigameUiKit.DrawLine(texture, 185, 39, 235, 39, new Color(1f, 1f, 1f, 0.42f), 3);
        MinigameUiKit.DrawLine(texture, 186, 56, 235, 56, new Color(0f, 0f, 0f, 0.22f), 3);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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

    private void SortByStableOrder(List<WireTask> tasks)
    {
        tasks.Sort((a, b) => GetStableOrder(a.Id).CompareTo(GetStableOrder(b.Id)));
    }

    private int GetStableOrder(string id)
    {
        switch (id)
        {
            case "BAT": return 0;
            case "GND": return 1;
            case "SIG": return 2;
            case "FIVE": return 3;
            case "MOTOR": return 4;
            case "SENSE": return 5;
            default: return 99;
        }
    }
}
