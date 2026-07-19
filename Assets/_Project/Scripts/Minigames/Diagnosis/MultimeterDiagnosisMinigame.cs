using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Minigames.Diagnosis
{
    public class MultimeterDiagnosisMinigame : MonoBehaviour, IMinigame
    {
        private enum MeterMode
        {
            Voltage,
            Continuity,
            Resistance,
            Diode
        }

        private enum ProbeLead
        {
            Red,
            Black
        }

        private enum ComponentVisual
        {
            Fuse,
            Regulator,
            Capacitor,
            Resistor,
            Diode,
            Ic,
            Connector
        }

        private sealed class ProbeDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private MultimeterDiagnosisMinigame _owner;
            private ProbeLead _lead;

            public void Configure(MultimeterDiagnosisMinigame owner, ProbeLead lead)
            {
                _owner = owner;
                _lead = lead;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                _owner?.BeginProbeDrag(_lead, eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                _owner?.DragProbe(_lead, eventData);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                _owner?.EndProbeDrag(_lead, eventData);
            }
        }

        private sealed class TestPointData
        {
            public string Id;
            public string Label;
            public Vector2 Position;
            public Image Image;
            public TextMeshProUGUI LabelText;
        }

        private sealed class BoardComponentData
        {
            public string Id;
            public string Label;
            public string Description;
            public ComponentVisual Visual;
            public Vector2 Position;
            public Vector2 Size;
            public Button Button;
            public Image Image;
            public TextMeshProUGUI LabelText;
        }

        private sealed class EvidenceRule
        {
            public MeterMode Mode;
            public string RedPoint;
            public string BlackPoint;
            public bool IgnorePolarity;
            public string Summary;

            public EvidenceRule(MeterMode mode, string redPoint, string blackPoint, bool ignorePolarity, string summary)
            {
                Mode = mode;
                RedPoint = redPoint;
                BlackPoint = blackPoint;
                IgnorePolarity = ignorePolarity;
                Summary = summary;
            }
        }

        private sealed class FaultCase
        {
            public string Id;
            public string Title;
            public string Symptom;
            public string CorrectComponentId;
            public string DiagnosisText;
            public List<EvidenceRule> EvidenceRules = new List<EvidenceRule>();
        }

        private sealed class Reading
        {
            public string Display;
            public string Explanation;
            public bool IsAbnormal;

            public Reading(string display, string explanation, bool isAbnormal)
            {
                Display = display;
                Explanation = explanation;
                IsAbnormal = isAbnormal;
            }
        }

        public string MinigameName => "Dò Lỗi Bằng Đồng Hồ Đo";
        public bool IsActive { get; private set; }
        public event Action<RepairQuality> OnMinigameCompleted;

        [Header("Tuning")]
        [SerializeField] private float baseTimeLimit = 120f;
        [SerializeField] private int baseMistakesAllowed = 5;
        [SerializeField] private Color boardGreen = new Color(0.03f, 0.33f, 0.23f, 1f);
        [SerializeField] private Color copperColor = new Color(0.95f, 0.68f, 0.24f, 1f);
        [SerializeField] private Color activeBlue = new Color(0.12f, 0.62f, 1f, 1f);

        private readonly Dictionary<string, TestPointData> _testPoints = new Dictionary<string, TestPointData>();
        private readonly Dictionary<string, BoardComponentData> _components = new Dictionary<string, BoardComponentData>();
        private readonly List<FaultCase> _faultCases = new List<FaultCase>();
        private readonly HashSet<string> _foundEvidenceKeys = new HashSet<string>();
        private readonly List<string> _history = new List<string>();

        private GameObject _uiRoot;
        private RectTransform _boardRect;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _symptomText;
        private TextMeshProUGUI _timerText;
        private TextMeshProUGUI _evidenceText;
        private TextMeshProUGUI _meterDisplayText;
        private TextMeshProUGUI _meterSubText;
        private TextMeshProUGUI _redProbeText;
        private TextMeshProUGUI _blackProbeText;
        private TextMeshProUGUI _selectedComponentText;
        private TextMeshProUGUI _historyText;
        private TextMeshProUGUI _feedbackText;
        private Button _redProbeButton;
        private Button _blackProbeButton;
        private Button _voltageButton;
        private Button _continuityButton;
        private Button _resistanceButton;
        private Button _diodeButton;
        private Button _submitButton;
        private ProbeLead _draggingLead;
        private Image _probeDragShadow;
        private Image _probeDragLine;
        private bool _isProbeDragging;
        private Image _probeDragGhost;
        private Image _redProbeBoardMarker;
        private Image _blackProbeBoardMarker;

        private Sprite _solidSprite;
        private Sprite _boardSprite;
        private Sprite _meterSprite;
        private Sprite _testPointSprite;
        private Sprite _testPointActiveSprite;
        private Sprite _redProbeSprite;
        private Sprite _blackProbeSprite;
        private Sprite _burnSprite;
        private Sprite _buttonSprite;

        private MeterMode _currentMode = MeterMode.Voltage;
        private ProbeLead _selectedLead = ProbeLead.Red;
        private FaultCase _activeCase;
        private string _redPointId;
        private string _blackPointId;
        private string _selectedComponentId;
        private string _lastMeterDisplay;
        private string _lastMeterExplanation;
        private bool _hasMeterReading;

        private int _difficultyLevel = 1;
        private int _requiredEvidenceCount = 2;
        private int _measurementCount;
        private int _mistakes;
        private float _timeRemaining;
        private bool _isFinishing;
        private CursorLockMode _previousLockMode;
        private bool _previousCursorVisible;

        private const float BoardWidth = 1060f;
        private const float BoardHeight = 690f;

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
                Fail("Hết giờ. Chưa khoanh vùng được lỗi trên bo mạch.");
            }
        }

        public void Initialize(List<string> faults, int difficultyLevel)
        {
            EnsureUI();
            BuildFaultCases();

            _difficultyLevel = Mathf.Clamp(difficultyLevel, 1, 5);
            _activeCase = PickFaultCase(faults);
            _requiredEvidenceCount = Mathf.Clamp(1 + _difficultyLevel, 2, Mathf.Min(4, _activeCase.EvidenceRules.Count));
            _measurementCount = 0;
            _mistakes = 0;
            _timeRemaining = Mathf.Max(55f, baseTimeLimit - ((_difficultyLevel - 1) * 12f));
            _isFinishing = false;
            _redPointId = null;
            _blackPointId = null;
            _selectedComponentId = null;
            ClearMeterReading();

            _foundEvidenceKeys.Clear();
            _history.Clear();
            _currentMode = MeterMode.Voltage;
            _selectedLead = ProbeLead.Red;

            ResetBoardVisuals();
            UpdateAllTexts();
            AddHistory("Bắt đầu kiểm tra: chọn mode đo, kéo 2 que lên test point.");
        }

        public void StartMinigame()
        {
            EnsureUI();
            IsActive = true;

            _previousLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ShowUI(true);
            MinigameSfxKit.Play(MinigameSfxCue.Open, 0.58f);
            UpdateAllTexts();
            Debug.Log("[MultimeterDiagnosis] Minigame UI opened.");

            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.ShowSubtitle("Anh Thợ Điện", "Dùng đồng hồ đo để khoanh vùng lỗi. Đo đủ bằng chứng rồi kết luận linh kiện hỏng.", 4f, "Tiếng đặt đồ");
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

            EnsureSprites();
            EnsureEventSystem();

            _uiRoot = CreateUIObject("MultimeterDiagnosisUI", transform);
            global::MinigameWorkbenchVisuals.Install(_uiRoot, global::MinigameWorkbenchStyle.Diagnosis, new Color(0.14f, 0.76f, 1f, 1f));

            Canvas canvas = _uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _uiRoot.AddComponent<GraphicRaycaster>();
            CanvasGroup group = _uiRoot.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            _uiRoot.AddComponent<MinigameUiCanvasIntro>();

            RectTransform rootRect = _uiRoot.GetComponent<RectTransform>();
            Stretch(rootRect);

            Image overlay = CreateImage(_uiRoot.transform, "BackgroundOverlay", new Color(0.01f, 0.012f, 0.016f, 0.92f), _solidSprite);
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = false;
            overlay.transform.SetAsFirstSibling();

            GameObject header = CreatePanel("Header", _uiRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1840f, 70f), new Color(0.035f, 0.04f, 0.045f, 0.94f));
            MinigameUiKit.AddChrome(header.transform, _solidSprite, new Color(0.14f, 0.76f, 1f, 0.95f));
            _titleText = AddText(header.transform, "DÒ LỖI BẰNG ĐỒNG HỒ ĐO", 27, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.96f, 0.99f, 1f, 1f));
            SetAnchored(_titleText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(620f, 44f));
            _timerText = AddText(header.transform, "", 24, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.65f, 0.9f, 1f, 1f));
            SetAnchored(_timerText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(420f, 44f));
            _evidenceText = AddText(header.transform, "", 21, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.83f, 0.3f, 1f));
            SetAnchored(_evidenceText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 44f));

            GameObject boardPanel = CreatePanel("BoardPanel", _uiRoot.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(620f, -12f), new Vector2(1180f, 885f), new Color(0.022f, 0.028f, 0.03f, 0.96f));
            MinigameUiKit.AddChrome(boardPanel.transform, _solidSprite, new Color(0.14f, 0.76f, 1f, 0.5f));
            _symptomText = AddText(boardPanel.transform, "", 20, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.86f, 0.94f, 0.92f, 1f));
            SetAnchored(_symptomText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1110f, 52f));

            GameObject guidePanel = CreatePanel("GuidePanel", boardPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(1110f, 46f), new Color(0.032f, 0.045f, 0.05f, 0.94f));
            MinigameUiKit.AddChrome(guidePanel.transform, _solidSprite, new Color(0.14f, 0.76f, 1f, 0.45f));
            TextMeshProUGUI guideText = AddText(guidePanel.transform, "Hướng dẫn: chọn mode rồi kéo que đỏ/đen lên test point. Có thể bấm test point nếu muốn thao tác nhanh. Đo đủ bằng chứng rồi kết luận lỗi.", 17, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.84f, 0.94f, 0.92f, 1f));
            Stretch(guideText.rectTransform);

            GameObject boardObject = CreateUIObject("PCB_Board", boardPanel.transform);
            _boardRect = boardObject.GetComponent<RectTransform>();
            SetAnchored(_boardRect, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(BoardWidth, BoardHeight));
            Image boardImage = boardObject.AddComponent<Image>();
            boardImage.sprite = _boardSprite;
            boardImage.color = Color.white;

            BuildBoardVisuals();

            GameObject toolPanel = CreatePanel("ToolPanel", _uiRoot.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-310f, -12f), new Vector2(570f, 885f), new Color(0.025f, 0.028f, 0.033f, 0.97f));
            MinigameUiKit.AddChrome(toolPanel.transform, _solidSprite, new Color(0.14f, 0.76f, 1f, 0.5f));
            BuildToolPanel(toolPanel.transform);

            _feedbackText = AddText(_uiRoot.transform, "", 26, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetAnchored(_feedbackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(1060f, 44f));

            ShowUI(false);
        }

        private void BuildToolPanel(Transform parent)
        {
            Image meter = CreateImage(parent, "MultimeterSprite", Color.white, _meterSprite);
            SetAnchored(meter.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(430f, 255f));

            _meterDisplayText = AddText(meter.transform, "0.00", 32, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.06f, 0.16f, 0.05f, 1f));
            SetAnchored(_meterDisplayText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(285f, 36f));
            _meterSubText = AddText(meter.transform, "Chọn mode và đặt que đo", 13, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.08f, 0.18f, 0.07f, 1f));
            SetAnchored(_meterSubText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(310f, 22f));

            _voltageButton = CreateTextButton(parent, "VOLT", new Vector2(-195f, -318f), new Vector2(92f, 44f), () => SetMode(MeterMode.Voltage));
            _continuityButton = CreateTextButton(parent, "BEEP", new Vector2(-65f, -318f), new Vector2(92f, 44f), () => SetMode(MeterMode.Continuity));
            _resistanceButton = CreateTextButton(parent, "OHM", new Vector2(65f, -318f), new Vector2(92f, 44f), () => SetMode(MeterMode.Resistance));
            _diodeButton = CreateTextButton(parent, "DIODE", new Vector2(195f, -318f), new Vector2(92f, 44f), () => SetMode(MeterMode.Diode));

            Image redProbe = CreateImage(parent, "RedProbeSprite", Color.white, _redProbeSprite);
            SetAnchored(redProbe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-130f, -405f), new Vector2(135f, 82f));
            Image blackProbe = CreateImage(parent, "BlackProbeSprite", Color.white, _blackProbeSprite);
            SetAnchored(blackProbe.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(130f, -405f), new Vector2(135f, 82f));

            redProbe.raycastTarget = true;
            blackProbe.raycastTarget = true;
            ProbeDragHandle redDrag = redProbe.gameObject.AddComponent<ProbeDragHandle>();
            redDrag.Configure(this, ProbeLead.Red);
            ProbeDragHandle blackDrag = blackProbe.gameObject.AddComponent<ProbeDragHandle>();
            blackDrag.Configure(this, ProbeLead.Black);

            _redProbeButton = CreateTextButton(parent, "QUE ĐỎ", new Vector2(-130f, -470f), new Vector2(170f, 44f), () => SelectProbe(ProbeLead.Red));
            _blackProbeButton = CreateTextButton(parent, "QUE ĐEN", new Vector2(130f, -470f), new Vector2(170f, 44f), () => SelectProbe(ProbeLead.Black));
            _redProbeText = AddText(parent, "", 16, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.45f, 0.45f, 1f));
            SetAnchored(_redProbeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-130f, -520f), new Vector2(220f, 32f));
            _blackProbeText = AddText(parent, "", 16, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.82f, 0.86f, 0.9f, 1f));
            SetAnchored(_blackProbeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(130f, -520f), new Vector2(220f, 32f));

            _selectedComponentText = AddText(parent, "Linh kiện nghi lỗi: chưa chọn", 18, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.95f, 0.9f, 0.72f, 1f));
            SetAnchored(_selectedComponentText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -575f), new Vector2(500f, 38f));

            GameObject historyPanel = CreatePanel("HistoryPanel", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -675f), new Vector2(510f, 140f), new Color(0.012f, 0.014f, 0.018f, 0.96f));
            _historyText = AddText(historyPanel.transform, "", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.78f, 0.9f, 0.92f, 1f));
            SetAnchored(_historyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(470f, 108f));

            _submitButton = CreateTextButton(parent, "KẾT LUẬN LỖI", new Vector2(-93f, -810f), new Vector2(300f, 58f), SubmitDiagnosis);
            CreateTextButton(parent, "HỦY", new Vector2(190f, -810f), new Vector2(125f, 58f), () => Fail("Bạn đã hủy lượt dò lỗi."));
        }

        private void BuildBoardVisuals()
        {
            CreateTrace(new Vector2(-474f, 250f), new Vector2(-160f, 250f), 8f, new Color(0.15f, 0.72f, 0.46f, 1f));
            CreateTrace(new Vector2(-160f, 250f), new Vector2(25f, 145f), 8f, new Color(0.15f, 0.72f, 0.46f, 1f));
            CreateTrace(new Vector2(25f, 145f), new Vector2(260f, 145f), 7f, new Color(0.12f, 0.62f, 0.48f, 1f));
            CreateTrace(new Vector2(35f, -80f), new Vector2(330f, -80f), 7f, new Color(0.16f, 0.68f, 0.62f, 1f));
            CreateTrace(new Vector2(-310f, -125f), new Vector2(-115f, -125f), 7f, new Color(0.15f, 0.64f, 0.5f, 1f));
            CreateTrace(new Vector2(-465f, -260f), new Vector2(410f, -260f), 9f, new Color(0.11f, 0.48f, 0.44f, 1f));
            CreateTrace(new Vector2(260f, 145f), new Vector2(260f, -210f), 6f, new Color(0.11f, 0.58f, 0.42f, 1f));

            AddComponent("CONN", "J1", "Jack nguồn vào 12V", ComponentVisual.Connector, new Vector2(-455f, 250f), new Vector2(88f, 70f));
            AddComponent("F1", "F1", "Cầu chì bảo vệ nguồn", ComponentVisual.Fuse, new Vector2(-260f, 250f), new Vector2(120f, 56f));
            AddComponent("U1", "U1", "IC nguồn 12V -> 5V", ComponentVisual.Regulator, new Vector2(55f, 145f), new Vector2(145f, 96f));
            AddComponent("R7", "R7", "Điện trở hồi tiếp 1k", ComponentVisual.Resistor, new Vector2(145f, -80f), new Vector2(130f, 54f));
            AddComponent("D1", "D1", "Diode bảo vệ ngược cực", ComponentVisual.Diode, new Vector2(-210f, -125f), new Vector2(125f, 58f));
            AddComponent("C3", "C3", "Tụ lọc đường 5V", ComponentVisual.Capacitor, new Vector2(260f, -210f), new Vector2(110f, 86f));
            AddComponent("U2", "U2", "Vi điều khiển chính", ComponentVisual.Ic, new Vector2(360f, 55f), new Vector2(165f, 125f));

            AddDecorativeBurn(new Vector2(-250f, 238f), 82f, 0.16f);
            AddDecorativeBurn(new Vector2(78f, 155f), 96f, 0.12f);
            AddDecorativeBurn(new Vector2(252f, -210f), 92f, 0.14f);
            AddDecorativeBurn(new Vector2(-205f, -126f), 74f, 0.12f);

            AddTestPoint("VIN", "VIN", new Vector2(-505f, 300f));
            AddTestPoint("FUSE_IN", "F-IN", new Vector2(-330f, 300f));
            AddTestPoint("FUSE_OUT", "F-OUT", new Vector2(-170f, 300f));
            AddTestPoint("REG_IN", "U1-IN", new Vector2(-18f, 195f));
            AddTestPoint("REG_OUT", "5V", new Vector2(190f, 195f));
            AddTestPoint("TP_3V_A", "3V3-A", new Vector2(5f, -28f));
            AddTestPoint("TP_3V_B", "3V3-B", new Vector2(330f, -28f));
            AddTestPoint("D_A", "D-A", new Vector2(-292f, -78f));
            AddTestPoint("D_K", "D-K", new Vector2(-128f, -78f));
            AddTestPoint("C_PLUS", "C+", new Vector2(195f, -155f));
            AddTestPoint("GND", "GND", new Vector2(-505f, -285f));
            AddTestPoint("GND2", "GND", new Vector2(410f, -285f));

            _redProbeBoardMarker = AddProbeMarker("RedProbeMarker", _redProbeSprite, new Color(1f, 1f, 1f, 1f));
            _blackProbeBoardMarker = AddProbeMarker("BlackProbeMarker", _blackProbeSprite, new Color(1f, 1f, 1f, 1f));
        }

        private void AddTestPoint(string id, string label, Vector2 position)
        {
            GameObject obj = CreateUIObject("TP_" + id, _boardRect);
            RectTransform rt = obj.GetComponent<RectTransform>();
            SetAnchored(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(44f, 44f));
            Image image = obj.AddComponent<Image>();
            image.sprite = _testPointSprite;
            image.color = Color.white;
            Button button = obj.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => HandleTestPointClicked(id));

            TextMeshProUGUI text = AddText(_boardRect, label, 14, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.9f, 1f, 0.86f, 1f));
            SetAnchored(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position + new Vector2(0f, -28f), new Vector2(74f, 22f));

            TestPointData data = new TestPointData();
            data.Id = id;
            data.Label = label;
            data.Position = position;
            data.Image = image;
            data.LabelText = text;
            _testPoints[id] = data;
        }

        private void AddComponent(string id, string label, string description, ComponentVisual visual, Vector2 position, Vector2 size)
        {
            GameObject obj = CreateUIObject("CMP_" + id, _boardRect);
            RectTransform rt = obj.GetComponent<RectTransform>();
            SetAnchored(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Image image = obj.AddComponent<Image>();
            image.sprite = CreateComponentSprite(visual);
            image.color = Color.white;
            Button button = obj.AddComponent<Button>();
            button.onClick.AddListener(() => SelectComponent(id));

            TextMeshProUGUI text = AddText(_boardRect, label, 17, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetAnchored(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position + new Vector2(0f, -(size.y * 0.5f) - 16f), new Vector2(120f, 26f));

            BoardComponentData component = new BoardComponentData();
            component.Id = id;
            component.Label = label;
            component.Description = description;
            component.Visual = visual;
            component.Position = position;
            component.Size = size;
            component.Button = button;
            component.Image = image;
            component.LabelText = text;
            _components[id] = component;
        }

        private Image AddProbeMarker(string name, Sprite sprite, Color color)
        {
            GameObject obj = CreateUIObject(name, _boardRect);
            RectTransform rt = obj.GetComponent<RectTransform>();
            SetAnchored(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 58f));
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            obj.SetActive(false);
            return image;
        }

        private void CreateTrace(Vector2 a, Vector2 b, float width, Color color)
        {
            GameObject obj = CreateUIObject("Trace", _boardRect);
            RectTransform rt = obj.GetComponent<RectTransform>();
            Image image = obj.AddComponent<Image>();
            image.sprite = _solidSprite;
            image.color = color;
            image.raycastTarget = false;

            Vector2 mid = (a + b) * 0.5f;
            float length = Vector2.Distance(a, b);
            SetAnchored(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), mid, new Vector2(length, width));
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            obj.transform.SetAsFirstSibling();
        }

        private void AddDecorativeBurn(Vector2 position, float size, float alpha)
        {
            Image burn = CreateImage(_boardRect, "BoardScorch", new Color(1f, 1f, 1f, alpha), _burnSprite);
            SetAnchored(burn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            burn.raycastTarget = false;
        }

        private void HandleTestPointClicked(string pointId)
        {
            if (!IsActive || _isFinishing)
            {
                return;
            }

            TryPlaceProbe(_selectedLead, pointId, true);
        }

        private bool TryPlaceProbe(ProbeLead lead, string pointId, bool advanceLead)
        {
            if (!IsActive || _isFinishing || !_testPoints.ContainsKey(pointId))
            {
                return false;
            }

            string otherPointId = lead == ProbeLead.Red ? _blackPointId : _redPointId;
            if (!string.IsNullOrEmpty(otherPointId) && otherPointId == pointId)
            {
                MinigameSfxKit.Play(MinigameSfxCue.Error, 0.46f);
                ShowFeedback((lead == ProbeLead.Red ? "Que đỏ" : "Que đen") + " không thể đặt cùng " + PointLabel(pointId) + ". Tách hai que ra hai test point khác nhau.", new Color(1f, 0.58f, 0.28f, 1f));
                UpdateProbeVisuals();
                return false;
            }

            if (lead == ProbeLead.Red)
            {
                _redPointId = pointId;
            }
            else
            {
                _blackPointId = pointId;
            }

            _selectedLead = advanceLead ? (lead == ProbeLead.Red ? ProbeLead.Black : ProbeLead.Red) : lead;
            ClearMeterReading();
            MinigameSfxKit.Play(MinigameSfxCue.Probe, 0.48f);

            bool hasRed = !string.IsNullOrEmpty(_redPointId);
            bool hasBlack = !string.IsNullOrEmpty(_blackPointId);
            if (!hasRed || !hasBlack)
            {
                ShowFeedback((lead == ProbeLead.Red ? "Đã đặt que đỏ tại " : "Đã đặt que đen tại ") + PointLabel(pointId) + ". Đặt tiếp " + (_selectedLead == ProbeLead.Red ? "que đỏ." : "que đen."), new Color(0.78f, 0.92f, 1f, 1f));
                UpdateProbeVisuals();
                UpdateMeterDisplay();
                return true;
            }

            UpdateProbeVisuals();
            MeasureCurrentPair();
            return true;
        }


        private void BeginProbeDrag(ProbeLead lead, PointerEventData eventData)
        {
            if (!IsActive || _isFinishing)
            {
                return;
            }

            _isProbeDragging = true;
            _draggingLead = lead;
            _selectedLead = lead;
            MinigameSfxKit.Play(MinigameSfxCue.Select, 0.36f);
            EnsureProbeDragPreview();
            SetProbeDragPreviewVisible(true);

            if (TryGetBoardLocalPoint(eventData, out Vector2 localPoint))
            {
                UpdateProbeDragPreview(lead, localPoint);
                HighlightProbeHover(FindNearestTestPoint(localPoint));
            }

            ShowFeedback((lead == ProbeLead.Red ? "Đang cầm que đỏ" : "Đang cầm que đen") + ". Kéo đầu que lên vòng test point rồi thả.", new Color(0.72f, 0.9f, 1f, 1f));
            UpdateProbeVisuals();
        }

        private void DragProbe(ProbeLead lead, PointerEventData eventData)
        {
            if (!_isProbeDragging || lead != _draggingLead)
            {
                return;
            }

            if (TryGetBoardLocalPoint(eventData, out Vector2 localPoint))
            {
                UpdateProbeDragPreview(lead, localPoint);
                HighlightProbeHover(FindNearestTestPoint(localPoint));
            }
        }

        private void EndProbeDrag(ProbeLead lead, PointerEventData eventData)
        {
            if (!_isProbeDragging || lead != _draggingLead)
            {
                return;
            }

            _isProbeDragging = false;
            SetProbeDragPreviewVisible(false);
            ClearProbeHover();

            if (!TryGetBoardLocalPoint(eventData, out Vector2 localPoint))
            {
                ShowFeedback("Thả que lên vùng bo mạch để đo.", new Color(1f, 0.82f, 0.34f, 1f));
                UpdateProbeVisuals();
                return;
            }

            string nearestPoint = FindNearestTestPoint(localPoint);
            if (string.IsNullOrEmpty(nearestPoint))
            {
                ShowFeedback("Chưa chạm test point nào. Kéo đầu que vào vòng đồng rồi thả.", new Color(1f, 0.82f, 0.34f, 1f));
                UpdateProbeVisuals();
                return;
            }

            TryPlaceProbe(lead, nearestPoint, true);
        }

        private void EnsureProbeDragPreview()
        {
            if (_probeDragLine != null)
            {
                return;
            }

            _probeDragShadow = MinigameUiKit.CreateLine(_boardRect, "ProbeDragShadow", _solidSprite, new Color(0f, 0f, 0f, 0.42f), Vector2.zero, Vector2.right, 11f);
            _probeDragLine = MinigameUiKit.CreateLine(_boardRect, "ProbeDragLine", _solidSprite, Color.white, Vector2.zero, Vector2.right, 6f);
            _probeDragShadow.raycastTarget = false;
            _probeDragLine.raycastTarget = false;

            _probeDragGhost = CreateImage(_boardRect, "ProbeDragGhost", Color.white, _redProbeSprite);
            SetAnchored(_probeDragGhost.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 64f));
            _probeDragGhost.raycastTarget = false;
            SetProbeDragPreviewVisible(false);
        }

        private void SetProbeDragPreviewVisible(bool visible)
        {
            if (_probeDragShadow != null) _probeDragShadow.gameObject.SetActive(visible);
            if (_probeDragLine != null) _probeDragLine.gameObject.SetActive(visible);
            if (_probeDragGhost != null) _probeDragGhost.gameObject.SetActive(visible);
        }

        private void UpdateProbeDragPreview(ProbeLead lead, Vector2 localPoint)
        {
            EnsureProbeDragPreview();
            Color lineColor = lead == ProbeLead.Red ? new Color(1f, 0.12f, 0.1f, 1f) : new Color(0.02f, 0.025f, 0.03f, 1f);
            Vector2 start = GetProbeAnchorPoint(lead, localPoint);
            MinigameUiKit.SetLine(_probeDragShadow.rectTransform, start + new Vector2(0f, -4f), localPoint + new Vector2(0f, -4f), 11f);
            MinigameUiKit.SetLine(_probeDragLine.rectTransform, start, localPoint, 6f);
            _probeDragLine.color = lineColor;
            _probeDragGhost.sprite = lead == ProbeLead.Red ? _redProbeSprite : _blackProbeSprite;
            _probeDragGhost.rectTransform.anchoredPosition = localPoint;
            _probeDragGhost.rectTransform.localScale = Vector3.one;
            _probeDragGhost.rectTransform.localRotation = Quaternion.identity;
            _probeDragGhost.transform.SetAsLastSibling();
        }

        private Vector2 GetProbeAnchorPoint(ProbeLead lead, Vector2 fallback)
        {
            string pointId = lead == ProbeLead.Red ? _redPointId : _blackPointId;
            Vector2 offset = lead == ProbeLead.Red ? new Vector2(-16f, 22f) : new Vector2(18f, 22f);
            if (!string.IsNullOrEmpty(pointId) && _testPoints.TryGetValue(pointId, out TestPointData point))
            {
                return point.Position + offset;
            }

            return fallback;
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

        private string FindNearestTestPoint(Vector2 localPoint)
        {
            string bestId = null;
            float bestDistance = 58f;
            foreach (KeyValuePair<string, TestPointData> pair in _testPoints)
            {
                float distance = Vector2.Distance(localPoint, pair.Value.Position);
                if (distance <= bestDistance)
                {
                    bestId = pair.Key;
                    bestDistance = distance;
                }
            }

            return bestId;
        }

        private void HighlightProbeHover(string pointId)
        {
            foreach (KeyValuePair<string, TestPointData> pair in _testPoints)
            {
                bool placed = pair.Key == _redPointId || pair.Key == _blackPointId;
                bool hovered = pair.Key == pointId;
                pair.Value.Image.sprite = placed || hovered ? _testPointActiveSprite : _testPointSprite;
                pair.Value.Image.color = hovered ? new Color(0.72f, 1f, 1f, 1f) : Color.white;
                pair.Value.LabelText.color = placed || hovered ? new Color(0.45f, 0.95f, 1f, 1f) : new Color(0.9f, 1f, 0.86f, 1f);
            }
        }

        private void ClearProbeHover()
        {
            UpdateProbeVisuals();
        }

        private void MeasureCurrentPair()
        {
            if (_redPointId == _blackPointId)
            {
                ClearMeterReading();
                _mistakes++;
                MinigameSfxKit.Play(MinigameSfxCue.Error, 0.62f);
                ShowFeedback("Hai que đang chạm cùng một điểm. Kết quả không có giá trị.", new Color(1f, 0.42f, 0.3f, 1f));
                UpdateAllTexts();
                return;
            }

            _measurementCount++;
            Reading reading = CalculateReading(_currentMode, _redPointId, _blackPointId);
            _lastMeterDisplay = reading.Display;
            _lastMeterExplanation = reading.Explanation;
            _hasMeterReading = true;
            UpdateMeterDisplay();

            if (reading.Display == "BEEP")
            {
                MinigameSfxKit.Play(MinigameSfxCue.Beep, 0.5f);
            }

            bool gainedEvidence = TryRegisterEvidence(_currentMode, _redPointId, _blackPointId, out string evidenceSummary);
            if (gainedEvidence)
            {
                AddHistory($"{ModeShortName(_currentMode)} {PointLabel(_redPointId)} - {PointLabel(_blackPointId)}: {reading.Display}  ✓ {evidenceSummary}");
                ShowFeedback("Bằng chứng tốt: " + evidenceSummary, new Color(0.35f, 1f, 0.58f, 1f));
            }
            else
            {
                AddHistory($"{ModeShortName(_currentMode)} {PointLabel(_redPointId)} - {PointLabel(_blackPointId)}: {reading.Display}");
                ShowFeedback(reading.IsAbnormal ? "Có dấu hiệu bất thường, hãy đối chiếu với khu vực nghi lỗi." : "Kết quả đo hợp lệ nhưng chưa đủ khoanh vùng lỗi.", new Color(0.78f, 0.92f, 1f, 1f));
            }

            UpdateAllTexts();
        }

        private Reading CalculateReading(MeterMode mode, string red, string black)
        {
            if (mode == MeterMode.Voltage)
            {
                float value = GetVoltage(red) - GetVoltage(black);
                bool abnormal = Mathf.Abs(value) < 0.15f || Mathf.Abs(value) > 11.5f;
                return new Reading($"{value:0.00} V", "Điện áp RED - BLACK", abnormal);
            }

            if (mode == MeterMode.Continuity)
            {
                float ohms = GetResistanceOhms(red, black);
                if (ohms <= 35f)
                {
                    return new Reading("BEEP", $"{ohms:0.0} Ω - thông mạch", ohms < 2f);
                }

                return new Reading("OL", "Không thông mạch", true);
            }

            if (mode == MeterMode.Resistance)
            {
                float ohms = GetResistanceOhms(red, black);
                if (ohms >= 999999f)
                {
                    return new Reading("OL", "Hở mạch / điện trở vô hạn", true);
                }

                if (ohms >= 1000f)
                {
                    return new Reading($"{ohms / 1000f:0.00} kΩ", "Điện trở đo được", false);
                }

                return new Reading($"{ohms:0.0} Ω", "Điện trở đo được", ohms < 3f);
            }

            return GetDiodeReading(red, black);
        }

        private float GetVoltage(string point)
        {
            if (IsGround(point))
            {
                return 0f;
            }

            switch (_activeCase.Id)
            {
                case "FuseOpen":
                    if (point == "VIN" || point == "FUSE_IN")
                    {
                        return 12.1f;
                    }
                    return 0f;

                case "RegulatorFailed":
                    if (point == "VIN" || point == "FUSE_IN" || point == "FUSE_OUT" || point == "REG_IN")
                    {
                        return 12.0f;
                    }
                    return 0.1f;

                case "BrokenTrace":
                    if (point == "TP_3V_B")
                    {
                        return 0.2f;
                    }
                    break;
            }

            if (point == "VIN" || point == "FUSE_IN" || point == "FUSE_OUT" || point == "REG_IN")
            {
                return 12.0f;
            }

            if (point == "REG_OUT" || point == "C_PLUS")
            {
                return _activeCase.Id == "CapacitorShort" ? 0.7f : 5.02f;
            }

            if (point == "TP_3V_A" || point == "TP_3V_B")
            {
                return 3.31f;
            }

            if (point == "D_A")
            {
                return 12.0f;
            }

            if (point == "D_K")
            {
                return _activeCase.Id == "DiodeShort" ? 12.0f : 11.35f;
            }

            return 0f;
        }

        private float GetResistanceOhms(string a, string b)
        {
            if (SamePair(a, b, "FUSE_IN", "FUSE_OUT"))
            {
                return _activeCase.Id == "FuseOpen" ? 999999f : 0.3f;
            }

            if (SamePair(a, b, "REG_OUT", "C_PLUS"))
            {
                return 0.5f;
            }

            if (SamePair(a, b, "REG_OUT", "GND") || SamePair(a, b, "REG_OUT", "GND2") || SamePair(a, b, "C_PLUS", "GND") || SamePair(a, b, "C_PLUS", "GND2"))
            {
                return _activeCase.Id == "CapacitorShort" ? 0.8f : 5600f;
            }

            if (SamePair(a, b, "TP_3V_A", "TP_3V_B"))
            {
                return _activeCase.Id == "BrokenTrace" ? 999999f : 0.4f;
            }

            if (SamePair(a, b, "D_A", "D_K"))
            {
                return _activeCase.Id == "DiodeShort" ? 0.2f : 850f;
            }

            if (SamePair(a, b, "VIN", "FUSE_IN") || SamePair(a, b, "FUSE_OUT", "REG_IN"))
            {
                return 0.4f;
            }

            if (IsGround(a) && IsGround(b))
            {
                return 0.2f;
            }

            return 999999f;
        }

        private Reading GetDiodeReading(string red, string black)
        {
            if (!SamePair(red, black, "D_A", "D_K"))
            {
                return new Reading("--", "Diode mode chỉ hữu ích trên D-A/D-K", false);
            }

            if (_activeCase.Id == "DiodeShort")
            {
                return new Reading("0.02 V", "Diode chập cả hai chiều", true);
            }

            if (red == "D_A" && black == "D_K")
            {
                return new Reading("0.62 V", "Sụt áp thuận diode", false);
            }

            return new Reading("OL", "Chiều ngược diode bị khóa", false);
        }

        private bool TryRegisterEvidence(MeterMode mode, string red, string black, out string summary)
        {
            summary = "";
            for (int i = 0; i < _activeCase.EvidenceRules.Count; i++)
            {
                EvidenceRule rule = _activeCase.EvidenceRules[i];
                if (rule.Mode != mode)
                {
                    continue;
                }

                bool match = rule.IgnorePolarity
                    ? SamePair(red, black, rule.RedPoint, rule.BlackPoint)
                    : red == rule.RedPoint && black == rule.BlackPoint;

                if (!match)
                {
                    continue;
                }

                string key = _activeCase.Id + ":" + i;
                if (_foundEvidenceKeys.Contains(key))
                {
                    summary = "đã ghi nhận trước đó";
                    return false;
                }

                _foundEvidenceKeys.Add(key);
                summary = rule.Summary;
                return true;
            }

            return false;
        }

        private void SubmitDiagnosis()
        {
            if (!IsActive || _isFinishing)
            {
                return;
            }

            if (string.IsNullOrEmpty(_selectedComponentId))
            {
                _mistakes++;
                MinigameSfxKit.Play(MinigameSfxCue.Error, 0.62f);
                ShowFeedback("Chưa chọn linh kiện nghi lỗi trên bo mạch.", new Color(1f, 0.52f, 0.28f, 1f));
                UpdateAllTexts();
                return;
            }

            if (_foundEvidenceKeys.Count < _requiredEvidenceCount)
            {
                _mistakes++;
                MinigameSfxKit.Play(MinigameSfxCue.Error, 0.62f);
                ShowFeedback($"Chưa đủ bằng chứng. Cần {_requiredEvidenceCount} phép đo hợp lý trước khi kết luận.", new Color(1f, 0.76f, 0.28f, 1f));
                UpdateAllTexts();
                return;
            }

            if (_selectedComponentId != _activeCase.CorrectComponentId)
            {
                _mistakes++;
                MinigameSfxKit.Play(MinigameSfxCue.Error, 0.62f);
                BoardComponentData selected = _components[_selectedComponentId];
                ShowFeedback($"{selected.Label} chưa phải nguyên nhân chính. Đo lại các rail liên quan.", new Color(1f, 0.38f, 0.32f, 1f));
                AddHistory("Kết luận sai: " + selected.Label);
                UpdateAllTexts();

                if (_mistakes >= baseMistakesAllowed + 1)
                {
                    Fail("Kết luận sai quá nhiều lần.");
                }

                return;
            }

            AddHistory("Kết luận đúng: " + _activeCase.DiagnosisText);
            ShowFeedback("Đúng bệnh: " + _activeCase.DiagnosisText, new Color(0.45f, 1f, 0.62f, 1f));
            Complete(EvaluateQuality(), true);
        }

        private RepairQuality EvaluateQuality()
        {
            int expectedMeasurements = _requiredEvidenceCount + Mathf.Max(1, _difficultyLevel - 1);
            int extraMeasurements = Mathf.Max(0, _measurementCount - expectedMeasurements);
            float timeUsed = Mathf.Max(0f, (baseTimeLimit - ((_difficultyLevel - 1) * 12f)) - _timeRemaining);

            float score = 100f;
            score -= _mistakes * 16f;
            score -= extraMeasurements * 4f;
            score -= Mathf.Max(0f, timeUsed - 48f) * 0.35f;

            if (_foundEvidenceKeys.Count < _requiredEvidenceCount || _selectedComponentId != _activeCase.CorrectComponentId)
            {
                return RepairQuality.Broken;
            }

            if (score >= 88f && _mistakes == 0)
            {
                return RepairQuality.Perfect;
            }

            if (score >= 68f)
            {
                return RepairQuality.Good;
            }

            if (score >= 42f)
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

        private void Fail(string reason)
        {
            AddHistory("Thất bại: " + reason);
            ShowFeedback(reason, new Color(1f, 0.35f, 0.3f, 1f));
            Complete(RepairQuality.Broken, true);
        }

        private void SelectComponent(string id)
        {
            if (!IsActive || _isFinishing)
            {
                return;
            }

            _selectedComponentId = id;
            MinigameSfxKit.Play(MinigameSfxCue.Select, 0.42f);
            foreach (var pair in _components)
            {
                pair.Value.Image.color = pair.Key == id ? new Color(1f, 0.9f, 0.4f, 1f) : Color.white;
                pair.Value.LabelText.color = pair.Key == id ? new Color(1f, 0.9f, 0.4f, 1f) : Color.white;
            }

            BoardComponentData component = _components[id];
            _selectedComponentText.text = $"Linh kiện nghi lỗi: {component.Label} - {component.Description}";
            ShowFeedback("Đã chọn " + component.Label + ". Đo đủ bằng chứng rồi bấm Kết luận lỗi.", new Color(0.86f, 0.92f, 1f, 1f));
        }

        private void SetMode(MeterMode mode)
        {
            _currentMode = mode;
            ClearMeterReading();
            if (IsActive)
            {
                MinigameSfxKit.Play(MinigameSfxCue.Select, 0.36f);
            }

            UpdateMeterDisplay();
            UpdateButtonStates();
        }

        private void SelectProbe(ProbeLead lead)
        {
            _selectedLead = lead;
            MinigameSfxKit.Play(MinigameSfxCue.Select, 0.36f);
            UpdateProbeVisuals();
            ShowFeedback(lead == ProbeLead.Red ? "Đang đặt que đỏ." : "Đang đặt que đen.", Color.white);
        }

        private void ResetBoardVisuals()
        {
            foreach (var pair in _components)
            {
                pair.Value.Image.color = Color.white;
                pair.Value.LabelText.color = Color.white;
            }

            foreach (var pair in _testPoints)
            {
                pair.Value.Image.sprite = _testPointSprite;
                pair.Value.Image.color = Color.white;
                pair.Value.LabelText.color = new Color(0.9f, 1f, 0.86f, 1f);
            }

            if (_redProbeBoardMarker != null)
            {
                _redProbeBoardMarker.gameObject.SetActive(false);
            }

            if (_blackProbeBoardMarker != null)
            {
                _blackProbeBoardMarker.gameObject.SetActive(false);
            }
        }

        private void UpdateAllTexts()
        {
            _titleText.text = "DÒ LỖI BẰNG ĐỒNG HỒ ĐO";
            _symptomText.text = _activeCase == null ? "" : $"Triệu chứng: {_activeCase.Symptom}";
            UpdateMeterDisplay();
            UpdateSelectedComponentText();
            UpdateProbeVisuals();
            UpdateStatusText();
            UpdateButtonStates();
            RefreshHistoryText();
        }

        private void UpdateMeterDisplay()
        {
            if (_meterDisplayText == null || _meterSubText == null)
            {
                return;
            }

            if (_hasMeterReading && !string.IsNullOrEmpty(_lastMeterDisplay))
            {
                _meterDisplayText.text = _lastMeterDisplay;
                _meterSubText.text = _lastMeterExplanation;
                return;
            }

            _meterDisplayText.text = ModeDisplay(_currentMode);
            _meterSubText.text = ModeHint(_currentMode);
        }

        private void ClearMeterReading()
        {
            _hasMeterReading = false;
            _lastMeterDisplay = "";
            _lastMeterExplanation = "";
        }

        private void UpdateSelectedComponentText()
        {
            if (_selectedComponentText == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_selectedComponentId) && _components.TryGetValue(_selectedComponentId, out BoardComponentData component))
            {
                _selectedComponentText.text = $"Linh kiện nghi lỗi: {component.Label} - {component.Description}";
                return;
            }

            _selectedComponentText.text = "Linh kiện nghi lỗi: chưa chọn";
        }


        private void UpdateStatusText()
        {
            if (_timerText != null)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(_timeRemaining));
                _timerText.text = $"Thời gian: {seconds / 60:00}:{seconds % 60:00}  |  Lượt đo: {_measurementCount}  |  Sai: {_mistakes}";
            }

            if (_evidenceText != null)
            {
                _evidenceText.text = $"Bằng chứng {_foundEvidenceKeys.Count}/{_requiredEvidenceCount}";
            }
        }

        private void UpdateProbeVisuals()
        {
            _redProbeText.text = string.IsNullOrEmpty(_redPointId) ? "Đỏ: chưa đặt" : "Đỏ: " + PointLabel(_redPointId);
            _blackProbeText.text = string.IsNullOrEmpty(_blackPointId) ? "Đen: chưa đặt" : "Đen: " + PointLabel(_blackPointId);

            foreach (var pair in _testPoints)
            {
                bool active = pair.Key == _redPointId || pair.Key == _blackPointId;
                pair.Value.Image.sprite = active ? _testPointActiveSprite : _testPointSprite;
                pair.Value.LabelText.color = active ? new Color(0.45f, 0.95f, 1f, 1f) : new Color(0.9f, 1f, 0.86f, 1f);
            }

            PositionProbeMarker(_redProbeBoardMarker, _redPointId, new Vector2(-16f, 22f));
            PositionProbeMarker(_blackProbeBoardMarker, _blackPointId, new Vector2(18f, 22f));
            UpdateButtonStates();
        }

        private void PositionProbeMarker(Image marker, string pointId, Vector2 offset)
        {
            if (marker == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(pointId) || !_testPoints.TryGetValue(pointId, out TestPointData point))
            {
                marker.gameObject.SetActive(false);
                return;
            }

            RectTransform markerRect = marker.rectTransform;
            marker.gameObject.SetActive(false);

            if (markerRect.parent != _boardRect)
            {
                markerRect.SetParent(_boardRect, false);
            }

            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(96f, 58f);
            markerRect.localScale = Vector3.one;
            markerRect.localRotation = Quaternion.identity;
            markerRect.anchoredPosition = point.Position + offset;
            marker.transform.SetAsLastSibling();
            marker.gameObject.SetActive(true);
        }

        private void UpdateButtonStates()
        {
            SetButtonSelected(_voltageButton, _currentMode == MeterMode.Voltage);
            SetButtonSelected(_continuityButton, _currentMode == MeterMode.Continuity);
            SetButtonSelected(_resistanceButton, _currentMode == MeterMode.Resistance);
            SetButtonSelected(_diodeButton, _currentMode == MeterMode.Diode);
            SetButtonSelected(_redProbeButton, _selectedLead == ProbeLead.Red);
            SetButtonSelected(_blackProbeButton, _selectedLead == ProbeLead.Black);
        }

        private void SetButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                Color buttonColor = selected ? activeBlue : new Color(0.11f, 0.14f, 0.16f, 1f);
                image.color = buttonColor;
                MinigameUiKit.ConfigureButtonColors(button, buttonColor);
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

        private void AddHistory(string line)
        {
            _history.Add(line);
            while (_history.Count > 6)
            {
                _history.RemoveAt(0);
            }

            RefreshHistoryText();
        }

        private void RefreshHistoryText()
        {
            if (_historyText == null)
            {
                return;
            }

            _historyText.text = string.Join("\n", _history);
        }

        private void ShowUI(bool show)
        {
            if (_uiRoot != null)
            {
                _uiRoot.SetActive(show);
                if (show)
                {
                    _uiRoot.transform.SetAsLastSibling();
                }
            }
        }

        private void RestoreCursor()
        {
            Cursor.lockState = _previousLockMode;
            Cursor.visible = _previousCursorVisible;
        }

        private void BuildFaultCases()
        {
            if (_faultCases.Count > 0)
            {
                return;
            }

            FaultCase fuse = new FaultCase();
            fuse.Id = "FuseOpen";
            fuse.Title = "Cầu chì nguồn bị đứt";
            fuse.Symptom = "Thiết bị không lên nguồn. Jack vào vẫn có 12V nhưng phía sau cầu chì im lặng.";
            fuse.CorrectComponentId = "F1";
            fuse.DiagnosisText = "F1 bị đứt, nguồn không đi qua được.";
            fuse.EvidenceRules.Add(new EvidenceRule(MeterMode.Voltage, "VIN", "GND", false, "Nguồn vào vẫn có 12V"));
            fuse.EvidenceRules.Add(new EvidenceRule(MeterMode.Voltage, "FUSE_OUT", "GND", false, "Sau cầu chì mất áp"));
            fuse.EvidenceRules.Add(new EvidenceRule(MeterMode.Continuity, "FUSE_IN", "FUSE_OUT", true, "Cầu chì không thông mạch"));
            _faultCases.Add(fuse);

            FaultCase regulator = new FaultCase();
            regulator.Id = "RegulatorFailed";
            regulator.Title = "IC nguồn không tạo 5V";
            regulator.Symptom = "Máy có nguồn vào nhưng màn hình không sáng. Khu vực IC nguồn hơi ấm.";
            regulator.CorrectComponentId = "U1";
            regulator.DiagnosisText = "U1 hỏng, có input nhưng mất output 5V.";
            regulator.EvidenceRules.Add(new EvidenceRule(MeterMode.Voltage, "REG_IN", "GND", false, "IC nguồn có input 12V"));
            regulator.EvidenceRules.Add(new EvidenceRule(MeterMode.Voltage, "REG_OUT", "GND", false, "Output 5V gần như bằng 0"));
            regulator.EvidenceRules.Add(new EvidenceRule(MeterMode.Resistance, "REG_OUT", "GND", true, "Đường 5V không bị chập nặng"));
            _faultCases.Add(regulator);

            FaultCase cap = new FaultCase();
            cap.Id = "CapacitorShort";
            cap.Title = "Tụ lọc 5V bị chập";
            cap.Symptom = "Cắm nguồn là adapter sụt áp, board hơi nóng ở cụm tụ lọc.";
            cap.CorrectComponentId = "C3";
            cap.DiagnosisText = "C3 bị chập xuống GND làm sụt đường 5V.";
            cap.EvidenceRules.Add(new EvidenceRule(MeterMode.Resistance, "REG_OUT", "GND", true, "Đường 5V xuống GND điện trở rất thấp"));
            cap.EvidenceRules.Add(new EvidenceRule(MeterMode.Continuity, "C_PLUS", "GND", true, "Tụ C3 đang thông xuống mass"));
            cap.EvidenceRules.Add(new EvidenceRule(MeterMode.Voltage, "REG_OUT", "GND", false, "Đường 5V bị kéo tụt áp"));
            _faultCases.Add(cap);

            FaultCase trace = new FaultCase();
            trace.Id = "BrokenTrace";
            trace.Title = "Đứt đường 3V3";
            trace.Symptom = "Thiết bị lúc được lúc không, gõ nhẹ vào vỏ thì mất tín hiệu.";
            trace.CorrectComponentId = "R7";
            trace.DiagnosisText = "Đường 3V3 qua khu R7 bị đứt trace.";
            trace.EvidenceRules.Add(new EvidenceRule(MeterMode.Continuity, "TP_3V_A", "TP_3V_B", true, "Hai đầu đường 3V3 không thông mạch"));
            trace.EvidenceRules.Add(new EvidenceRule(MeterMode.Voltage, "TP_3V_A", "GND", false, "Một đầu vẫn có 3V3"));
            trace.EvidenceRules.Add(new EvidenceRule(MeterMode.Voltage, "TP_3V_B", "GND", false, "Đầu còn lại mất áp"));
            _faultCases.Add(trace);

            FaultCase diode = new FaultCase();
            diode.Id = "DiodeShort";
            diode.Title = "Diode bảo vệ bị chập";
            diode.Symptom = "Thiết bị kéo dòng lớn ngay khi cắm nguồn. Có mùi khét nhẹ gần diode bảo vệ.";
            diode.CorrectComponentId = "D1";
            diode.DiagnosisText = "D1 bị chập, diode mode gần 0V cả hai chiều.";
            diode.EvidenceRules.Add(new EvidenceRule(MeterMode.Diode, "D_A", "D_K", false, "Diode thuận gần 0V bất thường"));
            diode.EvidenceRules.Add(new EvidenceRule(MeterMode.Diode, "D_K", "D_A", false, "Diode ngược vẫn dẫn, chứng tỏ bị chập"));
            diode.EvidenceRules.Add(new EvidenceRule(MeterMode.Resistance, "D_A", "D_K", true, "Hai đầu diode điện trở rất thấp"));
            _faultCases.Add(diode);
        }

        private FaultCase PickFaultCase(List<string> faults)
        {
            string joined = faults == null ? "" : string.Join(" ", faults).ToLowerInvariant();
            if (joined.Contains("cầu") || joined.Contains("fuse") || joined.Contains("nguồn"))
            {
                return _faultCases[0];
            }
            if (joined.Contains("ic"))
            {
                return _faultCases[1];
            }
            if (joined.Contains("tụ") || joined.Contains("tu"))
            {
                return _faultCases[2];
            }
            if (joined.Contains("đứt") || joined.Contains("dut") || joined.Contains("dây"))
            {
                return _faultCases[3];
            }
            if (joined.Contains("diode"))
            {
                return _faultCases[4];
            }

            int maxIndex = Mathf.Clamp(_difficultyLevel + 1, 1, _faultCases.Count);
            return _faultCases[UnityEngine.Random.Range(0, maxIndex)];
        }

        private string PointLabel(string id)
        {
            return _testPoints.ContainsKey(id) ? _testPoints[id].Label : id;
        }

        private string ModeShortName(MeterMode mode)
        {
            switch (mode)
            {
                case MeterMode.Voltage: return "V";
                case MeterMode.Continuity: return "BEEP";
                case MeterMode.Resistance: return "Ω";
                case MeterMode.Diode: return "DIODE";
            }

            return "?";
        }

        private string ModeDisplay(MeterMode mode)
        {
            switch (mode)
            {
                case MeterMode.Voltage: return "V DC";
                case MeterMode.Continuity: return "BEEP";
                case MeterMode.Resistance: return "Ω";
                case MeterMode.Diode: return "DIODE";
            }

            return "--";
        }

        private string ModeHint(MeterMode mode)
        {
            switch (mode)
            {
                case MeterMode.Voltage: return "Đo điện áp: đặt que đen vào GND.";
                case MeterMode.Continuity: return "Dò thông mạch: BEEP là có nối.";
                case MeterMode.Resistance: return "Đo điện trở khi nghi chập/hở.";
                case MeterMode.Diode: return "Kiểm diode: đúng chiều ~0.6V.";
            }

            return "";
        }

        private bool SamePair(string a, string b, string x, string y)
        {
            return (a == x && b == y) || (a == y && b == x);
        }

        private bool IsGround(string point)
        {
            return point == "GND" || point == "GND2";
        }

        private void EnsureSprites()
        {
            if (_solidSprite != null)
            {
                return;
            }

            _solidSprite = CreateSolidSprite(new Color(1f, 1f, 1f, 1f));
            _buttonSprite = CreateRoundedRectSprite(128, 128, 18, new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0.18f));
            _boardSprite = CreateBoardSprite();
            _meterSprite = CreateMeterSprite();
            _testPointSprite = CreateTestPointSprite(false);
            _testPointActiveSprite = CreateTestPointSprite(true);
            _redProbeSprite = CreateProbeSprite(new Color(0.95f, 0.08f, 0.08f, 1f));
            _blackProbeSprite = CreateProbeSprite(new Color(0.02f, 0.025f, 0.03f, 1f));
            _burnSprite = CreateBurnSprite();
        }

        private Sprite CreateComponentSprite(ComponentVisual visual)
        {
            Texture2D texture = new Texture2D(128, 96, TextureFormat.RGBA32, false);
            Clear(texture, new Color(0f, 0f, 0f, 0f));

            switch (visual)
            {
                case ComponentVisual.Fuse:
                    FillRounded(texture, 16, 36, 112, 60, 10, new Color(0.86f, 0.92f, 0.96f, 1f));
                    FillRect(texture, 24, 43, 104, 53, new Color(0.55f, 0.86f, 1f, 1f));
                    FillRect(texture, 0, 44, 16, 52, copperColor);
                    FillRect(texture, 112, 44, 128, 52, copperColor);
                    break;
                case ComponentVisual.Regulator:
                    FillRounded(texture, 20, 16, 108, 80, 8, new Color(0.06f, 0.07f, 0.08f, 1f));
                    FillRect(texture, 30, 10, 38, 16, copperColor);
                    FillRect(texture, 58, 10, 66, 16, copperColor);
                    FillRect(texture, 86, 10, 94, 16, copperColor);
                    FillCircle(texture, 36, 62, 6, new Color(0.22f, 0.24f, 0.26f, 1f));
                    break;
                case ComponentVisual.Capacitor:
                    FillRounded(texture, 34, 12, 94, 84, 18, new Color(0.14f, 0.24f, 0.72f, 1f));
                    FillRect(texture, 59, 18, 69, 78, new Color(0.78f, 0.88f, 1f, 1f));
                    FillRect(texture, 22, 44, 34, 52, copperColor);
                    FillRect(texture, 94, 44, 106, 52, copperColor);
                    break;
                case ComponentVisual.Resistor:
                    FillRounded(texture, 30, 28, 98, 68, 18, new Color(0.8f, 0.66f, 0.38f, 1f));
                    FillRect(texture, 44, 30, 50, 66, new Color(0.25f, 0.15f, 0.08f, 1f));
                    FillRect(texture, 60, 30, 66, 66, new Color(0.9f, 0.05f, 0.04f, 1f));
                    FillRect(texture, 76, 30, 82, 66, new Color(1f, 0.76f, 0.1f, 1f));
                    FillRect(texture, 0, 45, 30, 51, copperColor);
                    FillRect(texture, 98, 45, 128, 51, copperColor);
                    break;
                case ComponentVisual.Diode:
                    FillRect(texture, 0, 45, 128, 51, copperColor);
                    FillRounded(texture, 34, 28, 94, 68, 10, new Color(0.07f, 0.08f, 0.1f, 1f));
                    FillRect(texture, 72, 28, 79, 68, new Color(0.93f, 0.93f, 0.82f, 1f));
                    break;
                case ComponentVisual.Ic:
                    FillRounded(texture, 22, 16, 106, 80, 8, new Color(0.025f, 0.027f, 0.03f, 1f));
                    for (int i = 0; i < 7; i++)
                    {
                        int x = 15 + (i * 14);
                        FillRect(texture, x, 8, x + 7, 16, copperColor);
                        FillRect(texture, x, 80, x + 7, 88, copperColor);
                    }
                    FillCircle(texture, 36, 62, 5, new Color(0.22f, 0.23f, 0.25f, 1f));
                    break;
                case ComponentVisual.Connector:
                    FillRounded(texture, 18, 16, 110, 80, 10, new Color(0.14f, 0.14f, 0.16f, 1f));
                    FillCircle(texture, 46, 48, 13, new Color(0.02f, 0.02f, 0.025f, 1f));
                    FillCircle(texture, 82, 48, 13, new Color(0.02f, 0.02f, 0.025f, 1f));
                    break;
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateSolidSprite(Color color)
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateBoardSprite()
        {
            Texture2D texture = new Texture2D(1024, 672, TextureFormat.RGBA32, false);
            Clear(texture, boardGreen);
            for (int x = 36; x < 988; x += 64)
            {
                DrawLine(texture, x, 34, x, 638, new Color(0.09f, 0.42f, 0.32f, 0.5f), 2);
            }
            for (int y = 40; y < 640; y += 58)
            {
                DrawLine(texture, 34, y, 990, y, new Color(0.08f, 0.39f, 0.31f, 0.45f), 2);
            }
            for (int i = 0; i < 42; i++)
            {
                int x = 70 + ((i * 83) % 880);
                int y = 70 + ((i * 53) % 540);
                FillCircle(texture, x, y, 8, new Color(0.78f, 0.62f, 0.26f, 1f));
                FillCircle(texture, x, y, 4, boardGreen);
            }
            DrawLine(texture, 24, 18, 1000, 18, new Color(0.58f, 0.92f, 0.78f, 0.5f), 4);
            DrawLine(texture, 1004, 24, 1004, 648, new Color(0.58f, 0.92f, 0.78f, 0.5f), 4);
            DrawLine(texture, 1000, 654, 24, 654, new Color(0.58f, 0.92f, 0.78f, 0.5f), 4);
            DrawLine(texture, 18, 648, 18, 24, new Color(0.58f, 0.92f, 0.78f, 0.5f), 4);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateMeterSprite()
        {
            Texture2D texture = new Texture2D(384, 256, TextureFormat.RGBA32, false);
            Clear(texture, new Color(0f, 0f, 0f, 0f));
            FillRounded(texture, 26, 14, 358, 240, 24, new Color(0.08f, 0.09f, 0.105f, 1f), new Color(0.35f, 0.38f, 0.42f, 1f));
            FillRounded(texture, 82, 36, 302, 100, 10, new Color(0.62f, 0.76f, 0.58f, 1f), new Color(0.16f, 0.22f, 0.14f, 1f));
            FillCircle(texture, 192, 170, 48, new Color(0.035f, 0.038f, 0.044f, 1f));
            FillCircle(texture, 192, 170, 28, new Color(0.16f, 0.17f, 0.19f, 1f));
            FillRect(texture, 76, 202, 112, 226, new Color(0.75f, 0.08f, 0.08f, 1f));
            FillRect(texture, 272, 202, 308, 226, new Color(0.015f, 0.015f, 0.02f, 1f));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateTestPointSprite(bool active)
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Clear(texture, new Color(0f, 0f, 0f, 0f));
            FillCircle(texture, 32, 32, 24, active ? activeBlue : copperColor);
            FillCircle(texture, 32, 32, 14, active ? new Color(0.85f, 1f, 1f, 1f) : new Color(0.19f, 0.12f, 0.05f, 1f));
            FillCircle(texture, 32, 32, 8, boardGreen);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateProbeSprite(Color handleColor)
        {
            Texture2D texture = new Texture2D(160, 90, TextureFormat.RGBA32, false);
            Clear(texture, new Color(0f, 0f, 0f, 0f));
            DrawLine(texture, 26, 62, 130, 30, new Color(0.76f, 0.8f, 0.82f, 1f), 7);
            DrawLine(texture, 70, 49, 138, 26, new Color(0.92f, 0.94f, 0.95f, 1f), 4);
            FillRounded(texture, 18, 48, 82, 78, 12, handleColor, new Color(1f, 1f, 1f, 0.18f));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 160, 90), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateBurnSprite()
        {
            Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Clear(texture, new Color(0f, 0f, 0f, 0f));
            FillCircle(texture, 64, 64, 42, new Color(0.04f, 0.025f, 0.015f, 0.7f));
            FillCircle(texture, 70, 58, 22, new Color(0.22f, 0.1f, 0.035f, 0.55f));
            FillCircle(texture, 50, 74, 16, new Color(0f, 0f, 0f, 0.45f));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateRoundedRectSprite(int width, int height, int radius, Color fill, Color outline)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Clear(texture, new Color(0f, 0f, 0f, 0f));
            FillRounded(texture, 0, 0, width, height, radius, fill, outline);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject obj = CreateUIObject(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            SetAnchored(rt, anchorMin, anchorMax, anchoredPosition, size);
            Image image = obj.AddComponent<Image>();
            image.sprite = _buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            if (size.x >= 80f && size.y >= 42f)
            {
                ApplyPanelDepth(image);
            }

            return obj;
        }

        private void ApplyPanelDepth(Image image)
        {
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -7f);

            UnityEngine.UI.Outline outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.055f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private Button CreateTextButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = CreateUIObject(label + "_Button", parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            SetAnchored(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, size);
            Image image = obj.AddComponent<Image>();
            image.sprite = _buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.11f, 0.14f, 0.16f, 1f);
            ApplyPanelDepth(image);
            Button button = obj.AddComponent<Button>();
            button.onClick.AddListener(() => MinigameSfxKit.Play(MinigameSfxCue.Button, 0.28f));
            button.onClick.AddListener(onClick);
            button.transition = Selectable.Transition.ColorTint;
            MinigameUiKit.ConfigureButtonColors(button, image.color);
            obj.AddComponent<MinigameUiButtonMotion>();

            TextMeshProUGUI text = AddText(obj.transform, label, 17, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            return button;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private Image CreateImage(Transform parent, string name, Color color, Sprite sprite)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private TextMeshProUGUI AddText(Transform parent, string text, int fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject obj = CreateUIObject("Text", parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
        }

        private void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void Clear(Texture2D texture, Color color)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private void FillRect(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(texture.height, y1); y++)
            {
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(texture.width, x1); x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private void FillRounded(Texture2D texture, int x0, int y0, int x1, int y1, int radius, Color fill)
        {
            FillRounded(texture, x0, y0, x1, y1, radius, fill, new Color(0f, 0f, 0f, 0f));
        }

        private void FillRounded(Texture2D texture, int x0, int y0, int x1, int y1, int radius, Color fill, Color outline)
        {
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
                    {
                        continue;
                    }

                    int cx = Mathf.Clamp(x, x0 + radius, x1 - radius - 1);
                    int cy = Mathf.Clamp(y, y0 + radius, y1 - radius - 1);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist <= radius)
                    {
                        bool border = dist > radius - 2 || x <= x0 + 1 || x >= x1 - 2 || y <= y0 + 1 || y >= y1 - 2;
                        texture.SetPixel(x, y, border && outline.a > 0f ? outline : fill);
                    }
                }
            }
        }

        private void FillCircle(Texture2D texture, int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
                    {
                        continue;
                    }

                    int dx = x - cx;
                    int dy = y - cy;
                    if ((dx * dx) + (dy * dy) <= r2)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int width)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                FillCircle(texture, x0, y0, Mathf.Max(1, width / 2), color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}
