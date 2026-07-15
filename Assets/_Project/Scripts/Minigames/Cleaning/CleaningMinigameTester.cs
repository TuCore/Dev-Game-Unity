using System.Collections.Generic;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Temporary keyboard tester for CleaningMinigame.
/// Attach this to a GameObject together with CleaningMinigame, then press Play.
/// </summary>
[RequireComponent(typeof(CleaningMinigame))]
public class CleaningMinigameTester : MonoBehaviour
{
    private const float MinOrbitVerticalAngle = -45f;
    private const float MaxOrbitVerticalAngle = 30f;

    [Serializable]
    private class CleaningSpotBinding
    {
        public string targetName;
        public string faultId;
        public Renderer spotRenderer;

        [HideInInspector] public Material runtimeMaterial;
        [HideInInspector] public Color originalColor;
        [HideInInspector] public int wipePasses;
        [HideInInspector] public float swipeDistance;
        [HideInInspector] public Vector2 lastSwipeScreenPosition;
        [HideInInspector] public GameObject sparkleObject;
        [HideInInspector] public Renderer sparkleRenderer;
        [HideInInspector] public Material sparkleMaterial;
        [HideInInspector] public float sparkleStartedAt;
        [HideInInspector] public Collider[] spotColliders;
        [HideInInspector] public bool[] originalColliderEnabled;
    }

    [Serializable]
    private class ScrewBinding
    {
        public string targetName;
        public string objectName;
        public GameObject screwObject;
        public bool installOnLocalX;

        [HideInInspector] public Vector3 hiddenLocalPosition;
        [HideInInspector] public Vector3 installedLocalPosition;
        [HideInInspector] public Quaternion originalLocalRotation;
        [HideInInspector] public Renderer[] renderers;
        [HideInInspector] public Collider[] colliders;
        [HideInInspector] public bool installed;
        [HideInInspector] public bool isAnimating;
        [HideInInspector] public float animationStartedAt;
    }

    private class CleaningTargetBinding
    {
        public string targetName;
        public GameObject rootObject;
        public Texture2D iconTexture;
        public int dustSpotCount;
        public int screwCount;
        public bool screwInstallsOnLocalX;
        public bool completed;
        public readonly List<string> faultIds = new List<string>();
    }

    [Header("Test Settings")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private int screenSpaceSpotPaddingPixels = 32;
    [SerializeField] private int screenSpaceScrewPaddingPixels = 24;
    [SerializeField] private bool autoAddSpotMeshColliders = true;
    [SerializeField] private float requiredSwipeDistancePixels = 70f;
    [SerializeField] private int requiredWipePasses = 5;
    [SerializeField] private float opacityLossPerWipe = 0.2f;
    [SerializeField] private float sparkleFrameSeconds = 0.18f;
    [SerializeField] private int sparkleFrameCount = 4;
    [SerializeField] private float screwInstallDuration = 0.45f;
    [SerializeField] private float screwInstallSpinDegrees = 540f;
    [SerializeField] private float screwHiddenLocalX = -0.65f;
    [SerializeField] private float screwInstalledLocalX = 0f;
    [SerializeField] private float screwHiddenLocalY = -0.03f;
    [SerializeField] private float screwInstalledLocalY = 0f;
    [SerializeField] private float wrongInteractionPenalty = 5f;
    [SerializeField] private Vector3 cameraTargetOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private Vector3 cameraViewOffset = new Vector3(0f, 0.8f, -3f);
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private Camera interactionCamera;

    [Header("UI Textures")]
    [SerializeField] private Texture2D clothButtonTexture;
    [SerializeField] private Texture2D clothCursorTexture;
    [SerializeField] private Texture2D screwButtonTexture;

    [Header("Visual Spots")]
    [SerializeField] private List<CleaningSpotBinding> spotBindings = new List<CleaningSpotBinding>();

    [Header("Visual Screws")]
    [SerializeField] private List<ScrewBinding> screwBindings = new List<ScrewBinding>();

    [Header("Visual Tools")]
    [SerializeField] private GameObject cleaningToolObject;

    [Header("Visual Effects")]
    [SerializeField] private Texture2D sparkleTexture1;
    [SerializeField] private Texture2D sparkleTexture2;

    [Header("Camera Orbit")]
    [SerializeField] private Transform orbitTarget;
    [SerializeField] private float orbitHorizontalSpeed = 80f;
    [SerializeField] private float orbitVerticalSpeed = 55f;
    [SerializeField] private float cameraFrameDistanceMultiplier = 2.2f;
    [SerializeField] private float minCameraDistance = 2.5f;
    [SerializeField] private float maxCameraDistance = 18f;
    [SerializeField] private float zoomStepDistance = 0.75f;

    private CleaningMinigame _minigame;
    private int _selectedTaskIndex;
    private GameObject _screwdriverToolObject;
    private bool _showGuideMenu;
    private bool _isClothSelected;
    private bool _isScrewSelected;
    private bool _isChoosingTarget;
    private bool _screwPhaseActive;
    private bool _screwPhaseCompleted;
    private CleaningSpotBinding _activeSwipeBinding;
    private bool _hasRatingResult;
    private bool _showRatingResult;
    private RepairQuality _lastRatingResult;
    private float _orbitVerticalAngle;
    private float _cameraDistance;
    private float _score;

    private readonly List<string> _sampleFaults = new List<string>();
    private readonly List<CleaningTargetBinding> _targetBindings = new List<CleaningTargetBinding>();
    private readonly List<CleaningTargetBinding> _targetOrder = new List<CleaningTargetBinding>();
    private CleaningTargetBinding _activeTargetBinding;
    private int _activeTargetIndex;

    private void Awake()
    {
        _minigame = GetComponent<CleaningMinigame>();
        _minigame.OnMinigameCompleted += HandleMinigameCompleted;
        AutoBindCleaningTargets();
        AutoBindSceneSpots();
        AutoBindSceneScrews();
        AutoBindSceneTools();
        AutoBindInteractionCamera();
        AutoBindOrbitTarget();
        AutoBindUiTextures();
        PrepareSpotMaterials();
        PrepareScrews();
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            RestartTest();
        }
    }

    private void OnDestroy()
    {
        Cursor.visible = true;

        if (_minigame != null)
        {
            _minigame.OnMinigameCompleted -= HandleMinigameCompleted;
        }
    }

    private void Update()
    {
        if (CustomInputManager.GetKeyDown("CleanSpray"))
        {
            _showGuideMenu = !_showGuideMenu;
        }

        UpdateSparkles();
        UpdateScrewAnimations();
        if (!_isChoosingTarget)
        {
            HandleCameraOrbit();
            HandleMouseWheelZoom();
        }

        if (_isChoosingTarget)
        {
            return;
        }

        if (_screwPhaseActive)
        {
            if (_isClothSelected)
            {
                HandleWrongClothOnScrews();
            }
            else
            {
                HandleMouseScrewInstall();
            }

            return;
        }

        if (!_minigame.IsActive)
        {
            return;
        }

        if (_isScrewSelected)
        {
            HandleWrongScrewOnDust();
        }
        else
        {
            HandleMouseCleaning();
        }

        if (CustomInputManager.GetKeyDown("CleanWipe"))
        {
            _minigame.RegisterMistake();
            Debug.Log("[CleaningTester] Forced mistake.");
            LogSelectedTask();
        }

    }

    private void OnGUI()
    {
        if (_minigame == null)
        {
            return;
        }

        GUI.skin.label.fontSize = 10;
        GUI.skin.button.fontSize = 10;
        GUI.skin.box.fontSize = 12;

        if (_isChoosingTarget)
        {
            DrawScoreHud();
            DrawTargetSelectionMenu();
        }
        else
        {
            DrawCleaningProgressHud();
            DrawScoreHud();
            DrawFinishButton();
            DrawClothButton();
            DrawScrewButton();
        }

        DrawGuideHint();

        if (_showGuideMenu)
        {
            DrawGuideMenu();
        }

        if (_showRatingResult)
        {
            DrawRatingResult();
        }

        DrawClothCursor();
    }

    private string GetTestStatus()
    {
        if (_minigame.IsActive)
        {
            return "RUNNING";
        }

        if (_minigame.CompletionRatio >= 1f)
        {
            return "COMPLETED";
        }

        return "ENDED EARLY / INCOMPLETE";
    }

    private int CountCompletedTasks()
    {
        int completedCount = 0;
        for (int i = 0; i < _minigame.Tasks.Count; i++)
        {
            if (_minigame.Tasks[i].Completed)
            {
                completedCount++;
            }
        }

        return completedCount;
    }

    private bool AreDustTasksComplete()
    {
        if (_minigame.Tasks.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < _minigame.Tasks.Count; i++)
        {
            if (!_minigame.Tasks[i].Completed)
            {
                return false;
            }
        }

        return true;
    }

    private int CountInstalledScrews()
    {
        int installedCount = 0;
        for (int i = 0; i < screwBindings.Count; i++)
        {
            if (IsBindingOnActiveTarget(screwBindings[i]) && screwBindings[i].installed)
            {
                installedCount++;
            }
        }

        return installedCount;
    }

    private float GetScrewCompletionRatio()
    {
        int screwCount = CountActiveScrews();
        if (screwCount == 0)
        {
            return 1f;
        }

        return Mathf.Clamp01((float)CountInstalledScrews() / screwCount);
    }

    private bool AreAllScrewsInstalled()
    {
        int screwCount = CountActiveScrews();
        return screwCount > 0 && CountInstalledScrews() >= screwCount;
    }

    private float GetOverallCompletionRatio()
    {
        int totalDustPasses = Mathf.Max(1, GetTotalDustSpotCount()) * Mathf.Max(1, requiredWipePasses);
        int completedDustPasses = 0;
        for (int i = 0; i < spotBindings.Count; i++)
        {
            if (spotBindings[i] != null)
            {
                completedDustPasses += Mathf.Min(spotBindings[i].wipePasses, requiredWipePasses);
            }
        }

        int totalActions = totalDustPasses + Mathf.Max(0, GetTotalScrewCount());
        int completedActions = completedDustPasses + CountAllInstalledScrews();
        return Mathf.Clamp01((float)completedActions / Mathf.Max(1, totalActions));
    }

    private float GetActiveTargetCompletionRatio()
    {
        int totalDustPasses = 0;
        int completedDustPasses = 0;
        for (int i = 0; i < spotBindings.Count; i++)
        {
            CleaningSpotBinding binding = spotBindings[i];
            if (!IsBindingOnActiveTarget(binding))
            {
                continue;
            }

            totalDustPasses += Mathf.Max(1, requiredWipePasses);
            completedDustPasses += Mathf.Min(binding.wipePasses, requiredWipePasses);
        }

        int totalScrews = CountActiveScrews();
        int totalActions = totalDustPasses + totalScrews;
        int completedActions = completedDustPasses + CountInstalledScrews();
        return Mathf.Clamp01((float)completedActions / Mathf.Max(1, totalActions));
    }

    private void AddCorrectInteractionScore()
    {
        int dustActionCount = Mathf.Max(1, GetTotalDustSpotCount()) * Mathf.Max(1, requiredWipePasses);
        int screwActionCount = Mathf.Max(0, GetTotalScrewCount());
        int totalActionCount = Mathf.Max(1, dustActionCount + screwActionCount);
        _score = Mathf.Min(100f, _score + 100f / totalActionCount);
    }

    private void RegisterWrongInteraction(string reason)
    {
        _score -= Mathf.Abs(wrongInteractionPenalty);
        Debug.Log($"[CleaningTester] Wrong interaction: {reason}. Score={Mathf.RoundToInt(_score)}");
    }

    private RepairQuality GetQualityFromScore()
    {
        int roundedScore = Mathf.RoundToInt(_score);
        if (roundedScore >= 100)
        {
            return RepairQuality.Perfect;
        }

        if (roundedScore >= 80)
        {
            return RepairQuality.Good;
        }

        if (roundedScore >= 50)
        {
            return RepairQuality.Passable;
        }

        return RepairQuality.Broken;
    }

    private void DrawCleaningProgressHud()
    {
        int cleanPercent = Mathf.RoundToInt(GetActiveTargetCompletionRatio() * 100f);
        Rect progressRect = new Rect(Screen.width - 220f, 12f, 200f, 34f);
        GUI.Box(progressRect, string.Empty);

        GUIStyle progressStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(progressRect, $"Làm sạch {cleanPercent}%/100%", progressStyle);
    }

    private void DrawScoreHud()
    {
        Rect scoreRect = new Rect(Screen.width - 220f, 52f, 200f, 30f);
        GUI.Box(scoreRect, string.Empty);

        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(scoreRect, $"Diem so: {Mathf.RoundToInt(_score)}", scoreStyle);
    }

    private void DrawTargetSelectionMenu()
    {
        List<CleaningTargetBinding> availableTargets = GetAvailableTargets();
        float panelWidth = Mathf.Min(520f, Screen.width - 40f);
        float panelHeight = 190f;
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

        GUI.Box(panelRect, "CHON VAT PHAM CAN VE SINH");

        if (availableTargets.Count == 0)
        {
            GUIStyle emptyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 58f, panelRect.width - 32f, 60f), "Tat ca vat pham da duoc ve sinh.", emptyStyle);
            return;
        }

        const float iconSize = 92f;
        const float gap = 20f;
        float totalWidth = availableTargets.Count * iconSize + (availableTargets.Count - 1) * gap;
        float startX = panelRect.x + (panelRect.width - totalWidth) * 0.5f;
        float iconY = panelRect.y + 54f;

        for (int i = 0; i < availableTargets.Count; i++)
        {
            CleaningTargetBinding target = availableTargets[i];
            Rect buttonRect = new Rect(startX + i * (iconSize + gap), iconY, iconSize, iconSize);
            GUIContent content = target.iconTexture != null
                ? new GUIContent(target.iconTexture, target.targetName)
                : new GUIContent(GetTargetDisplayName(target.targetName));

            if (GUI.Button(buttonRect, content))
            {
                StartTarget(target);
            }

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(buttonRect.x - 8f, buttonRect.yMax + 4f, buttonRect.width + 16f, 22f), GetTargetDisplayName(target.targetName), labelStyle);
        }
    }

    private void DrawGuideHint()
    {
        Rect hintRect = new Rect(12f, Screen.height - 48f, 190f, 34f);
        GUI.Box(hintRect, string.Empty);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };
        GUI.Label(hintRect, "Nhan R de xem huong dan", hintStyle);
    }

    private void DrawGuideMenu()
    {
        Rect menuRect = GetGuideMenuRect();
        GUI.Box(menuRect, "HUONG DAN");

        GUILayout.BeginArea(new Rect(menuRect.x + 16f, menuRect.y + 34f, menuRect.width - 32f, menuRect.height - 48f));
        GUI.skin.label.fontSize = 13;
        GUILayout.Label("1. Chon icon vat pham can ve sinh.");
        GUILayout.Label("2. Click icon khan lau, roi re chuot tren tung vet bui.");
        GUILayout.Label("3. Moi vet bui can 5 luot lau hop le de hoan thanh.");
        GUILayout.Label("4. Sau khi lau xong, chon icon oc vit va click tung oc vit.");
        GUILayout.Label("5. Dung phim mui ten de xoay camera quanh vat pham.");
        GUILayout.Label("6. Lan chuot len/xuong de zoom in/zoom out.");
        GUILayout.Label("7. Ve sinh xong vat pham se quay lai menu chon vat pham con lai.");
        GUILayout.Space(8f);
        GUILayout.Label("Nhan R lan nua de dong huong dan.");
        GUILayout.EndArea();
    }

    private void DrawClothButton()
    {
        Rect buttonRect = GetClothButtonRect();
        Rect frameRect = new Rect(buttonRect.x - 5f, buttonRect.y - 5f, buttonRect.width + 10f, buttonRect.height + 10f);

        Color previousColor = GUI.color;
        if (_isClothSelected)
        {
            GUI.color = new Color(0.25f, 1f, 0.25f, 0.85f);
            GUI.Box(frameRect, string.Empty);
        }

        GUI.color = previousColor;

        if (clothButtonTexture != null)
        {
            if (GUI.Button(buttonRect, clothButtonTexture))
            {
                ToggleClothSelection();
            }
        }
        else if (GUI.Button(buttonRect, "CLOTH"))
        {
            ToggleClothSelection();
        }
    }

    private void DrawScrewButton()
    {
        Rect buttonRect = GetScrewButtonRect();
        Rect frameRect = new Rect(buttonRect.x - 5f, buttonRect.y - 5f, buttonRect.width + 10f, buttonRect.height + 10f);

        Color previousColor = GUI.color;
        if (_isScrewSelected)
        {
            GUI.color = new Color(0.25f, 1f, 0.25f, 0.85f);
            GUI.Box(frameRect, string.Empty);
        }

        GUI.color = previousColor;

        if (screwButtonTexture != null)
        {
            if (GUI.Button(buttonRect, screwButtonTexture))
            {
                ToggleScrewSelection();
            }
        }
        else if (GUI.Button(buttonRect, "SCREW"))
        {
            ToggleScrewSelection();
        }
    }

    private void DrawFinishButton()
    {
        Rect finishRect = GetFinishButtonRect();
        bool canUseFinish = _minigame.IsActive || _hasRatingResult;

        GUI.enabled = canUseFinish;
        if (GUI.Button(finishRect, "FINISH"))
        {
            FinishAndShowRating();
        }

        GUI.enabled = true;
    }

    private void DrawRatingResult()
    {
        Rect ratingRect = new Rect((Screen.width - 280f) * 0.5f, 18f, 280f, 118f);
        GUI.Box(ratingRect, "RESULT");

        GUIStyle ratingStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(ratingRect.x + 12f, ratingRect.y + 22f, ratingRect.width - 24f, 28f), $"Rating: {_lastRatingResult}", ratingStyle);
        GUI.Label(new Rect(ratingRect.x + 12f, ratingRect.y + 50f, ratingRect.width - 24f, 22f), $"Score: {Mathf.RoundToInt(_score)}", GUI.skin.label);

        Rect okRect = new Rect(ratingRect.x + (ratingRect.width - 92f) * 0.5f, ratingRect.y + 78f, 92f, 28f);
        if (GUI.Button(okRect, "OK"))
        {
            _showRatingResult = false;
            RestartTest();
        }
    }

    private void DrawClothCursor()
    {
        if (!_isClothSelected || clothCursorTexture == null)
        {
            return;
        }

        Vector2 mousePosition = Event.current.mousePosition;
        Rect cursorRect = new Rect(mousePosition.x - 18f, mousePosition.y - 18f, 44f, 44f);
        GUI.DrawTexture(cursorRect, clothCursorTexture, ScaleMode.ScaleToFit, true);
    }

    private void ToggleClothSelection()
    {
        _isClothSelected = !_isClothSelected;
        if (_isClothSelected)
        {
            _isScrewSelected = false;
        }

        Cursor.visible = !_isClothSelected;
        UpdateToolVisibility();
    }

    private void ToggleScrewSelection()
    {
        _isScrewSelected = !_isScrewSelected;
        if (_isScrewSelected)
        {
            _isClothSelected = false;
        }

        Cursor.visible = true;
        UpdateToolVisibility();
    }

    private void RestartTest()
    {
        _selectedTaskIndex = 0;
        _isClothSelected = false;
        _isScrewSelected = false;
        _isChoosingTarget = true;
        _screwPhaseActive = false;
        _screwPhaseCompleted = false;
        _activeTargetBinding = null;
        _activeTargetIndex = -1;
        _activeSwipeBinding = null;
        _hasRatingResult = false;
        _showRatingResult = false;
        _score = 0f;
        Cursor.visible = true;
        ResetTargetCompletion();
        ResetVisualSpots();
        ResetScrewsForStart();
        BuildTargetOrder();
        SetActiveCleaningTarget(null);
        UpdateToolVisibility();

        Debug.Log("[CleaningTester] Started Cleaning minigame test.");
        Debug.Log("[CleaningTester] Select a cleaning target icon to begin.");
    }

    private void BuildTargetOrder()
    {
        _targetOrder.Clear();
        for (int i = 0; i < _targetBindings.Count; i++)
        {
            CleaningTargetBinding target = _targetBindings[i];
            if (target != null && target.rootObject != null && target.faultIds.Count > 0)
            {
                _targetOrder.Add(target);
            }
        }
    }

    private void StartTarget(int targetIndex)
    {
        if (_targetOrder.Count == 0)
        {
            Debug.LogWarning("[CleaningTester] No cleaning targets were found.");
            return;
        }

        _activeTargetIndex = Mathf.Clamp(targetIndex, 0, _targetOrder.Count - 1);
        _activeTargetBinding = _targetOrder[_activeTargetIndex];
        if (_activeTargetBinding.completed)
        {
            Debug.LogWarning($"[CleaningTester] Target already completed: {_activeTargetBinding.targetName}");
            ShowTargetSelection();
            return;
        }

        _selectedTaskIndex = 0;
        _isChoosingTarget = false;
        _screwPhaseActive = false;
        _screwPhaseCompleted = false;
        _activeSwipeBinding = null;
        _isClothSelected = false;
        _isScrewSelected = false;
        Cursor.visible = true;

        SetActiveCleaningTarget(_activeTargetBinding);

        _sampleFaults.Clear();
        _sampleFaults.AddRange(_activeTargetBinding.faultIds);
        _minigame.Initialize(_sampleFaults, difficultyLevel);
        _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
        _minigame.StartMinigame();

        ResetScrewsForTarget(_activeTargetBinding, false);
        UpdateVisualSpots();
        UpdateToolVisibility();

        Debug.Log($"[CleaningTester] Active cleaning target: {_activeTargetBinding.targetName}");
    }

    private void StartTarget(CleaningTargetBinding target)
    {
        int targetIndex = _targetOrder.IndexOf(target);
        if (targetIndex < 0)
        {
            Debug.LogWarning($"[CleaningTester] Target is not available: {target?.targetName}");
            return;
        }

        StartTarget(targetIndex);
    }

    private void SelectNextTask()
    {
        if (_minigame.Tasks.Count == 0)
        {
            return;
        }

        _selectedTaskIndex = (_selectedTaskIndex + 1) % _minigame.Tasks.Count;
        UpdateToolVisibility();
        LogSelectedTask();
    }

    private void SelectPreviousTask()
    {
        if (_minigame.Tasks.Count == 0)
        {
            return;
        }

        _selectedTaskIndex--;
        if (_selectedTaskIndex < 0)
        {
            _selectedTaskIndex = _minigame.Tasks.Count - 1;
        }

        UpdateToolVisibility();
        LogSelectedTask();
    }

    private void SetSelectedTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= _minigame.Tasks.Count)
        {
            Debug.LogWarning($"[CleaningTester] Task {taskIndex + 1} does not exist.");
            return;
        }

        _selectedTaskIndex = taskIndex;
        UpdateToolVisibility();
        LogSelectedTask();
    }

    private void HandleMouseCleaning()
    {
        if (!_isClothSelected || interactionCamera == null || IsMouseOverBlockedUi())
        {
            ResetSwipeTracking();
            return;
        }

        CleaningSpotBinding hitBinding = FindSpotUnderMouse(out Vector2 textureCoord);
        if (hitBinding == null)
        {
            ResetSwipeTracking();
            return;
        }

        int taskIndex = FindTaskIndex(hitBinding.faultId);
        if (taskIndex < 0)
        {
            ResetSwipeTracking();
            return;
        }

        CleaningMinigame.CleaningTask task = _minigame.Tasks[taskIndex];
        if (task.Completed)
        {
            ResetSwipeTracking();
            return;
        }

        _selectedTaskIndex = taskIndex;
        TrackDustSwipe(hitBinding, taskIndex);
    }

    private void HandleWrongScrewOnDust()
    {
        if (!_isScrewSelected || interactionCamera == null || IsMouseOverBlockedUi())
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (FindSpotUnderMouse(out _) != null)
        {
            ResetSwipeTracking();
            RegisterWrongInteraction("Screw tool used on dust");
        }
    }

    private void HandleWrongClothOnScrews()
    {
        if (!_isClothSelected || interactionCamera == null || IsMouseOverBlockedUi())
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (FindScrewUnderMouse() != null)
        {
            RegisterWrongInteraction("Cloth used on screw");
        }
    }

    private void HandleCameraOrbit()
    {
        if (interactionCamera == null || orbitTarget == null)
        {
            return;
        }

        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput += 1f;
        }

        float verticalInput = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput += 1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput -= 1f;
        }

        if (Mathf.Approximately(horizontalInput, 0f) && Mathf.Approximately(verticalInput, 0f))
        {
            return;
        }

        Transform cameraTransform = interactionCamera.transform;
        Vector3 targetPosition = CalculateTargetBounds(orbitTarget).center + cameraTargetOffset;

        if (!Mathf.Approximately(horizontalInput, 0f))
        {
            cameraTransform.RotateAround(targetPosition, Vector3.up, horizontalInput * orbitHorizontalSpeed * Time.deltaTime);
        }

        if (!Mathf.Approximately(verticalInput, 0f))
        {
            float requestedDelta = verticalInput * orbitVerticalSpeed * Time.deltaTime;
            float nextVerticalAngle = Mathf.Clamp(_orbitVerticalAngle + requestedDelta, MinOrbitVerticalAngle, MaxOrbitVerticalAngle);
            float appliedDelta = nextVerticalAngle - _orbitVerticalAngle;

            if (!Mathf.Approximately(appliedDelta, 0f))
            {
                cameraTransform.RotateAround(targetPosition, cameraTransform.right, appliedDelta);
                _orbitVerticalAngle = nextVerticalAngle;
            }
        }
    }

    private void HandleMouseWheelZoom()
    {
        if (interactionCamera == null || orbitTarget == null || IsMouseOverBlockedUi())
        {
            return;
        }

        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        AdjustCameraZoom(-scrollDelta * zoomStepDistance);
    }

    private void TrackDustSwipe(CleaningSpotBinding binding, int taskIndex)
    {
        Vector2 mousePosition = Input.mousePosition;
        if (_activeSwipeBinding != binding)
        {
            _activeSwipeBinding = binding;
            binding.swipeDistance = 0f;
            binding.lastSwipeScreenPosition = mousePosition;
            return;
        }

        float moveDistance = Vector2.Distance(binding.lastSwipeScreenPosition, mousePosition);
        binding.lastSwipeScreenPosition = mousePosition;

        if (moveDistance <= 0.01f)
        {
            return;
        }

        binding.swipeDistance += moveDistance;
        if (binding.swipeDistance < requiredSwipeDistancePixels)
        {
            return;
        }

        binding.swipeDistance = 0f;
        RegisterWipePass(binding, taskIndex);
    }

    private void ResetSwipeTracking()
    {
        _activeSwipeBinding = null;
    }

    private void RegisterWipePass(CleaningSpotBinding binding, int taskIndex)
    {
        if (binding.wipePasses >= requiredWipePasses)
        {
            return;
        }

        binding.wipePasses++;
        AddCorrectInteractionScore();
        float progressPerPass = 1f / Mathf.Max(1, requiredWipePasses);
        CleaningMinigame.CleaningTask task = _minigame.Tasks[taskIndex];
        _minigame.CleanTask(taskIndex, progressPerPass * task.RequiredWork);

        float alpha = Mathf.Clamp01(1f - binding.wipePasses * opacityLossPerWipe);
        SetMaterialAlpha(binding.runtimeMaterial, alpha, binding.originalColor);

        if (binding.wipePasses >= requiredWipePasses)
        {
            binding.spotRenderer.enabled = false;
            SetSpotCollidersEnabled(binding, false);
            ActivateSparkle(binding);
            ResetSwipeTracking();
        }

        UpdateVisualSpots();
        Debug.Log($"[CleaningTester] Wiped {binding.faultId}: {binding.wipePasses}/{requiredWipePasses}");
    }

    private void ActivateSparkle(CleaningSpotBinding binding)
    {
        if (binding.sparkleObject == null || binding.sparkleMaterial == null)
        {
            return;
        }

        binding.sparkleStartedAt = Time.time;
        binding.sparkleMaterial.mainTexture = sparkleTexture1 != null ? sparkleTexture1 : sparkleTexture2;
        binding.sparkleObject.SetActive(true);
    }

    private void UpdateSparkles()
    {
        for (int i = 0; i < spotBindings.Count; i++)
        {
            CleaningSpotBinding binding = spotBindings[i];
            if (binding.sparkleObject == null || !binding.sparkleObject.activeSelf || binding.sparkleMaterial == null)
            {
                continue;
            }

            int frame = Mathf.FloorToInt((Time.time - binding.sparkleStartedAt) / Mathf.Max(0.01f, sparkleFrameSeconds));
            if (frame >= Mathf.Max(1, sparkleFrameCount))
            {
                binding.sparkleObject.SetActive(false);
                continue;
            }

            Texture2D sparkleTexture = frame % 2 == 0
                ? sparkleTexture1 != null ? sparkleTexture1 : sparkleTexture2
                : sparkleTexture2 != null ? sparkleTexture2 : sparkleTexture1;
            binding.sparkleMaterial.mainTexture = sparkleTexture;
        }
    }

    private void HandleMouseScrewInstall()
    {
        if (!_isScrewSelected || interactionCamera == null || IsMouseOverBlockedUi())
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        ScrewBinding binding = FindScrewUnderMouse();
        if (binding == null || binding.installed || binding.isAnimating)
        {
            return;
        }

        StartScrewInstall(binding);
    }

    private void UpdateScrewAnimations()
    {
        bool changedAnyScrew = false;
        for (int i = 0; i < screwBindings.Count; i++)
        {
            ScrewBinding binding = screwBindings[i];
            if (binding == null || binding.screwObject == null || !binding.isAnimating)
            {
                continue;
            }

            float elapsed = Time.time - binding.animationStartedAt;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, screwInstallDuration));
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (binding.installOnLocalX)
            {
                Vector3 localPosition = binding.installedLocalPosition;
                localPosition.x = Mathf.Lerp(screwHiddenLocalX, screwInstalledLocalX, easedProgress);
                binding.screwObject.transform.localPosition = localPosition;
                binding.screwObject.transform.localRotation = binding.originalLocalRotation;
            }
            else
            {
                binding.screwObject.transform.localPosition = Vector3.Lerp(
                    binding.hiddenLocalPosition,
                    binding.installedLocalPosition,
                    easedProgress);
                binding.screwObject.transform.localRotation =
                    binding.originalLocalRotation * Quaternion.Euler(0f, screwInstallSpinDegrees * easedProgress, 0f);
            }

            if (progress >= 1f)
            {
                binding.isAnimating = false;
                binding.installed = true;
                binding.screwObject.transform.localPosition = binding.installedLocalPosition;
                binding.screwObject.transform.localRotation = binding.originalLocalRotation;
                SetScrewCollidersEnabled(binding, false);
                SetObjectActive(binding.screwObject, false);
                changedAnyScrew = true;
                Debug.Log($"[CleaningTester] Installed {binding.objectName}.");
            }
        }

        if (changedAnyScrew && AreAllScrewsInstalled())
        {
            CompleteScrewPhase();
        }
    }

    private void BeginScrewPhase()
    {
        if (CountActiveScrews() == 0)
        {
            Debug.LogWarning("[CleaningTester] No screws were found for the active target. Skipping screw phase.");
            CompleteScrewPhase();
            return;
        }

        _screwPhaseActive = true;
        _screwPhaseCompleted = false;
        _hasRatingResult = false;
        _showRatingResult = false;
        _isClothSelected = false;
        _isScrewSelected = false;
        Cursor.visible = true;

        for (int i = 0; i < screwBindings.Count; i++)
        {
            if (IsBindingOnActiveTarget(screwBindings[i]))
            {
                ResetScrewBinding(screwBindings[i], true);
            }
        }

        UpdateToolVisibility();
    }

    private void CompleteScrewPhase()
    {
        _screwPhaseActive = false;
        _screwPhaseCompleted = true;
        _isScrewSelected = false;

        if (_activeTargetBinding != null)
        {
            _activeTargetBinding.completed = true;
        }

        if (HasAvailableTargets())
        {
            ShowTargetSelection();
            Debug.Log("[CleaningTester] Target completed. Select another cleaning target.");
            return;
        }

        _lastRatingResult = GetQualityFromScore();
        _hasRatingResult = true;
        _showRatingResult = true;
        Cursor.visible = true;
        UpdateToolVisibility();
        Debug.Log($"[CleaningTester] Screw phase completed. Final quality: {_lastRatingResult}");
    }

    private void StartScrewInstall(ScrewBinding binding)
    {
        AddCorrectInteractionScore();
        binding.isAnimating = true;
        binding.animationStartedAt = Time.time;
        SetScrewCollidersEnabled(binding, false);
    }

    private ScrewBinding FindScrewUnderMouse()
    {
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            for (int i = 0; i < screwBindings.Count; i++)
            {
                ScrewBinding binding = screwBindings[i];
                if (!IsBindingOnActiveTarget(binding) || binding.screwObject == null || binding.installed || binding.isAnimating)
                {
                    continue;
                }

                if (hit.transform == binding.screwObject.transform || hit.transform.IsChildOf(binding.screwObject.transform))
                {
                    return binding;
                }
            }
        }

        Vector2 mousePosition = Input.mousePosition;
        for (int i = 0; i < screwBindings.Count; i++)
        {
            ScrewBinding binding = screwBindings[i];
            if (!IsBindingOnActiveTarget(binding) || binding.screwObject == null || binding.installed || binding.isAnimating)
            {
                continue;
            }

            if (TryGetScrewScreenRect(binding, out Rect screenRect) && screenRect.Contains(mousePosition))
            {
                return binding;
            }
        }

        return null;
    }

    private CleaningSpotBinding FindSpotUnderMouse(out Vector2 textureCoord)
    {
        textureCoord = Vector2.zero;

        if (TryFindSpotUnderMouseByRaycast(out CleaningSpotBinding raycastBinding, out textureCoord))
        {
            return raycastBinding;
        }

        if (ActiveTargetRequiresPhysicalSpotHit())
        {
            return null;
        }

        if (TryFindSpotUnderMouseOnScreen(out CleaningSpotBinding screenBinding, out textureCoord))
        {
            return screenBinding;
        }

        return null;
    }

    private bool TryFindSpotUnderMouseByRaycast(out CleaningSpotBinding hitBinding, out Vector2 textureCoord)
    {
        hitBinding = null;
        textureCoord = Vector2.zero;

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        if (hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
        {
            Transform hitTransform = hits[hitIndex].collider.transform;

            for (int i = 0; i < spotBindings.Count; i++)
            {
                CleaningSpotBinding binding = spotBindings[i];
                if (!IsBindingOnActiveTarget(binding) || binding.spotRenderer == null || !binding.spotRenderer.enabled)
                {
                    continue;
                }

                Transform spotTransform = binding.spotRenderer.transform;
                if (hitTransform == spotTransform || hitTransform.IsChildOf(spotTransform) || spotTransform.IsChildOf(hitTransform))
                {
                    textureCoord = hits[hitIndex].textureCoord;
                    hitBinding = binding;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryFindSpotUnderMouseOnScreen(out CleaningSpotBinding hitBinding, out Vector2 textureCoord)
    {
        hitBinding = null;
        textureCoord = Vector2.zero;

        Vector2 mousePosition = Input.mousePosition;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < spotBindings.Count; i++)
        {
            CleaningSpotBinding binding = spotBindings[i];
            if (!IsBindingOnActiveTarget(binding) || binding.spotRenderer == null || !binding.spotRenderer.enabled)
            {
                continue;
            }

            if (!TryGetRendererScreenRect(binding.spotRenderer, out Rect screenRect))
            {
                continue;
            }

            Rect paddedRect = new Rect(
                screenRect.xMin - screenSpaceSpotPaddingPixels,
                screenRect.yMin - screenSpaceSpotPaddingPixels,
                screenRect.width + screenSpaceSpotPaddingPixels * 2f,
                screenRect.height + screenSpaceSpotPaddingPixels * 2f);

            if (!paddedRect.Contains(mousePosition))
            {
                continue;
            }

            Vector2 center = screenRect.center;
            float distance = Vector2.Distance(mousePosition, center);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            hitBinding = binding;
            textureCoord = new Vector2(
                Mathf.InverseLerp(screenRect.xMin, screenRect.xMax, mousePosition.x),
                Mathf.InverseLerp(screenRect.yMin, screenRect.yMax, mousePosition.y));
        }

        if (hitBinding == null)
        {
            return false;
        }

        textureCoord.x = Mathf.Clamp01(textureCoord.x);
        textureCoord.y = Mathf.Clamp01(textureCoord.y);
        return true;
    }

    private bool TryGetRendererScreenRect(Renderer targetRenderer, out Rect screenRect)
    {
        screenRect = Rect.zero;

        Bounds bounds = targetRenderer.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        bool hasVisibleCorner = false;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screenPoint = interactionCamera.WorldToScreenPoint(corners[i]);
            if (screenPoint.z <= interactionCamera.nearClipPlane)
            {
                continue;
            }

            hasVisibleCorner = true;
            minX = Mathf.Min(minX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxX = Mathf.Max(maxX, screenPoint.x);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        if (!hasVisibleCorner || maxX <= minX || maxY <= minY)
        {
            return false;
        }

        screenRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private bool TryGetScrewScreenRect(ScrewBinding binding, out Rect screenRect)
    {
        screenRect = Rect.zero;
        if (binding == null || binding.renderers == null)
        {
            return false;
        }

        bool hasRect = false;
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < binding.renderers.Length; i++)
        {
            Renderer targetRenderer = binding.renderers[i];
            if (targetRenderer == null || !targetRenderer.enabled)
            {
                continue;
            }

            if (!TryGetRendererScreenRect(targetRenderer, out Rect rendererRect))
            {
                continue;
            }

            hasRect = true;
            minX = Mathf.Min(minX, rendererRect.xMin);
            minY = Mathf.Min(minY, rendererRect.yMin);
            maxX = Mathf.Max(maxX, rendererRect.xMax);
            maxY = Mathf.Max(maxY, rendererRect.yMax);
        }

        if (!hasRect)
        {
            return false;
        }

        screenRect = Rect.MinMaxRect(
            minX - screenSpaceScrewPaddingPixels,
            minY - screenSpaceScrewPaddingPixels,
            maxX + screenSpaceScrewPaddingPixels,
            maxY + screenSpaceScrewPaddingPixels);
        return true;
    }

    private void LogSelectedTask()
    {
        if (_minigame.Tasks.Count == 0)
        {
            Debug.Log("[CleaningTester] No tasks.");
            return;
        }

        CleaningMinigame.CleaningTask task = _minigame.Tasks[_selectedTaskIndex];
        Debug.Log(
            $"[CleaningTester] Selected #{_selectedTaskIndex + 1}: {task.FaultId}, " +
            $"type={task.TaskType}, progress={task.Progress:P0}, completed={task.Completed}, " +
            $"mode={_minigame.CurrentMode}, mistakes={_minigame.Mistakes}, " +
            $"preview={_minigame.PreviewRepairQuality()}");
    }

    private void HandleMinigameCompleted(RepairQuality quality)
    {
        _isClothSelected = false;
        _isScrewSelected = false;
        Cursor.visible = true;
        _lastRatingResult = quality;

        if (AreDustTasksComplete() && !_screwPhaseCompleted)
        {
            BeginScrewPhase();
            Debug.Log("[CleaningTester] Dust phase completed. Install all screws to finish.");
            return;
        }

        _hasRatingResult = true;
        UpdateToolVisibility();
        Debug.Log($"[CleaningTester] Completed. Final quality: {quality}");
    }

    private void FinishAndShowRating()
    {
        if (_minigame.IsActive)
        {
            _minigame.EndMinigame();
            _lastRatingResult = GetQualityFromScore();
            _hasRatingResult = true;
        }
        else if (_hasRatingResult)
        {
            _lastRatingResult = GetQualityFromScore();
        }

        if (_hasRatingResult)
        {
            _showRatingResult = true;
            _isClothSelected = false;
            Cursor.visible = true;
            UpdateToolVisibility();
            Debug.Log($"[CleaningTester] Rating shown: {_lastRatingResult}");
        }
    }

    private void AutoBindCleaningTargets()
    {
        _targetBindings.Clear();
        TryAddCleaningTarget("OldTableFan", 5, 4, false);
        TryAddCleaningTarget("InductionCooker", 4, 1, true);
    }

    private void TryAddCleaningTarget(string targetName, int dustSpotCount, int screwCount, bool screwInstallsOnLocalX)
    {
        GameObject rootObject = FindSceneObjectByName(targetName, false);
        if (rootObject == null)
        {
            Debug.LogWarning($"[CleaningTester] Could not find cleaning target: {targetName}");
            return;
        }

        _targetBindings.Add(new CleaningTargetBinding
        {
            targetName = targetName,
            rootObject = rootObject,
            dustSpotCount = dustSpotCount,
            screwCount = screwCount,
            screwInstallsOnLocalX = screwInstallsOnLocalX
        });
    }

    private void AutoBindSceneSpots()
    {
        for (int i = spotBindings.Count - 1; i >= 0; i--)
        {
            if (spotBindings[i] == null || FindTargetBinding(spotBindings[i].targetName) == null)
            {
                spotBindings.RemoveAt(i);
            }
        }

        for (int targetIndex = 0; targetIndex < _targetBindings.Count; targetIndex++)
        {
            CleaningTargetBinding target = _targetBindings[targetIndex];
            for (int i = 0; i < target.dustSpotCount; i++)
            {
                string faultId = GetTargetFaultId(target, i);
                if (TryAddBinding(target, faultId, $"DustSpot_{i + 1:00}"))
                {
                    target.faultIds.Add(faultId);
                }
            }
        }
    }

    private void AutoBindSceneScrews()
    {
        for (int i = screwBindings.Count - 1; i >= 0; i--)
        {
            if (screwBindings[i] == null || FindTargetBinding(screwBindings[i].targetName) == null)
            {
                screwBindings.RemoveAt(i);
            }
        }

        for (int targetIndex = 0; targetIndex < _targetBindings.Count; targetIndex++)
        {
            CleaningTargetBinding target = _targetBindings[targetIndex];
            for (int i = 1; i <= target.screwCount; i++)
            {
                TryAddScrewBinding(target, $"Screw_{i}");
            }
        }
    }

    private void AutoBindSceneTools()
    {
        if (cleaningToolObject == null)
        {
            cleaningToolObject = FindSceneObjectByName("Cloth");
        }

        if (_screwdriverToolObject == null)
        {
            _screwdriverToolObject = FindSceneObjectByName("Screwdriver");
        }
    }

    private void AutoBindInteractionCamera()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (interactionCamera == null)
        {
            interactionCamera = FindFirstObjectByType<Camera>();
        }
    }

    private void AutoBindOrbitTarget()
    {
        if (orbitTarget != null)
        {
            return;
        }

        CleaningTargetBinding firstTarget = _targetBindings.Count > 0 ? _targetBindings[0] : null;
        if (firstTarget != null && firstTarget.rootObject != null)
        {
            orbitTarget = firstTarget.rootObject.transform;
        }
    }

    private void AutoBindUiTextures()
    {
#if UNITY_EDITOR
        if (clothButtonTexture == null)
        {
            clothButtonTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/UI/Cloth_Button.png");
        }

        if (clothCursorTexture == null)
        {
            clothCursorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/UI/Cloth_Mouse.png");
        }

        if (screwButtonTexture == null)
        {
            screwButtonTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/UI/Screw_Button.png");
        }

        if (sparkleTexture1 == null)
        {
            sparkleTexture1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Minigames/Cleaning/Sparkle_1.png");
        }

        if (sparkleTexture2 == null)
        {
            sparkleTexture2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Minigames/Cleaning/Sparkle_2.png");
        }

        AssignTargetIcon("OldTableFan", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/UI/Icons/OldTableFan_Icon.png"));
        AssignTargetIcon("InductionCooker", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/UI/Icons/InductionCooker_Icon.png"));
#endif
    }

    private void AssignTargetIcon(string targetName, Texture2D iconTexture)
    {
        CleaningTargetBinding target = FindTargetBinding(targetName);
        if (target != null && iconTexture != null)
        {
            target.iconTexture = iconTexture;
        }
    }

    private bool TryAddBinding(CleaningTargetBinding target, string faultId, string objectName)
    {
        CleaningSpotBinding existingBinding = FindSpotBinding(faultId);
        if (existingBinding != null && existingBinding.spotRenderer != null)
        {
            return true;
        }

        GameObject spotObject = FindChildObjectByName(target.rootObject.transform, objectName);
        if (spotObject == null)
        {
            Debug.LogWarning($"[CleaningTester] Could not find visual spot object: {target.targetName}/{objectName}");
            return false;
        }

        Renderer spotRenderer = spotObject.GetComponent<Renderer>();
        if (spotRenderer == null)
        {
            Debug.LogWarning($"[CleaningTester] Visual spot has no Renderer: {objectName}");
            return false;
        }

        if (existingBinding != null)
        {
            existingBinding.targetName = target.targetName;
            existingBinding.spotRenderer = spotRenderer;
            return true;
        }

        spotBindings.Add(new CleaningSpotBinding
        {
            targetName = target.targetName,
            faultId = faultId,
            spotRenderer = spotRenderer
        });
        return true;
    }

    private void TryAddScrewBinding(CleaningTargetBinding target, string objectName)
    {
        ScrewBinding existingBinding = FindScrewBinding(target.targetName, objectName);
        if (existingBinding != null && existingBinding.screwObject != null)
        {
            return;
        }

        GameObject screwObject = FindChildObjectByName(target.rootObject.transform, objectName);
        if (screwObject == null)
        {
            Debug.LogWarning($"[CleaningTester] Could not find screw object: {target.targetName}/{objectName}");
            return;
        }

        if (existingBinding != null)
        {
            existingBinding.targetName = target.targetName;
            existingBinding.screwObject = screwObject;
            existingBinding.installOnLocalX = target.screwInstallsOnLocalX;
            return;
        }

        screwBindings.Add(new ScrewBinding
        {
            targetName = target.targetName,
            objectName = objectName,
            screwObject = screwObject,
            installOnLocalX = target.screwInstallsOnLocalX
        });
    }

    private GameObject FindSceneScrewObjectByName(string objectName)
    {
        GameObject bestObject = null;
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject sceneObject = allObjects[i];
            if (sceneObject.name != objectName || !sceneObject.scene.IsValid())
            {
                continue;
            }

            if (sceneObject.transform.childCount > 0)
            {
                return sceneObject;
            }

            if (bestObject == null)
            {
                bestObject = sceneObject;
            }
        }

        return bestObject;
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        return FindSceneObjectByName(objectName, true);
    }

    private GameObject FindSceneObjectByName(string objectName, bool warnIfMissing)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
        {
            return activeObject;
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject sceneObject = allObjects[i];
            if (sceneObject.name == objectName && sceneObject.scene.IsValid())
            {
                return sceneObject;
            }
        }

        if (warnIfMissing)
        {
            Debug.LogWarning($"[CleaningTester] Could not find scene object: {objectName}");
        }

        return null;
    }

    private GameObject FindChildObjectByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == objectName)
            {
                return children[i].gameObject;
            }
        }

        return null;
    }

    private void PrepareSpotMaterials()
    {
        for (int i = 0; i < spotBindings.Count; i++)
        {
            CleaningSpotBinding binding = spotBindings[i];
            if (binding.spotRenderer == null)
            {
                continue;
            }

            binding.runtimeMaterial = binding.spotRenderer.material;
            binding.originalColor = GetMaterialColor(binding.runtimeMaterial);
            CacheSpotColliders(binding);
            CreateSparkleForBinding(binding);
        }
    }

    private void PrepareScrews()
    {
        for (int i = 0; i < screwBindings.Count; i++)
        {
            ScrewBinding binding = screwBindings[i];
            if (binding.screwObject == null)
            {
                continue;
            }

            binding.renderers = binding.screwObject.GetComponentsInChildren<Renderer>(true);
            binding.colliders = binding.screwObject.GetComponentsInChildren<Collider>(true);
            binding.originalLocalRotation = binding.screwObject.transform.localRotation;

            Vector3 installedPosition = binding.screwObject.transform.localPosition;
            if (binding.installOnLocalX)
            {
                installedPosition.x = screwInstalledLocalX;
            }
            else
            {
                installedPosition.y = screwInstalledLocalY;
            }

            binding.installedLocalPosition = installedPosition;

            Vector3 hiddenPosition = installedPosition;
            if (binding.installOnLocalX)
            {
                hiddenPosition.x = screwHiddenLocalX;
            }
            else
            {
                hiddenPosition.y = screwHiddenLocalY;
            }

            binding.hiddenLocalPosition = hiddenPosition;

            ResetScrewBinding(binding, false);
        }
    }

    private void ResetScrewsForStart()
    {
        for (int i = 0; i < screwBindings.Count; i++)
        {
            ResetScrewBinding(screwBindings[i], false);
        }
    }

    private void ResetScrewBinding(ScrewBinding binding, bool visible)
    {
        if (binding == null || binding.screwObject == null)
        {
            return;
        }

        binding.installed = false;
        binding.isAnimating = false;
        binding.animationStartedAt = 0f;
        binding.screwObject.transform.localPosition = binding.hiddenLocalPosition;
        binding.screwObject.transform.localRotation = binding.originalLocalRotation;
        SetScrewCollidersEnabled(binding, visible);
        SetObjectActive(binding.screwObject, visible);
    }

    private void CacheSpotColliders(CleaningSpotBinding binding)
    {
        if (binding.spotColliders != null || binding.spotRenderer == null)
        {
            return;
        }

        binding.spotColliders = binding.spotRenderer.GetComponentsInChildren<Collider>(true);
        if (binding.spotColliders.Length == 0 && autoAddSpotMeshColliders)
        {
            MeshFilter meshFilter = binding.spotRenderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = binding.spotRenderer.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                binding.spotColliders = new Collider[] { meshCollider };
            }
        }

        binding.originalColliderEnabled = new bool[binding.spotColliders.Length];
        for (int i = 0; i < binding.spotColliders.Length; i++)
        {
            binding.originalColliderEnabled[i] = binding.spotColliders[i] != null && binding.spotColliders[i].enabled;
        }
    }

    private void CreateSparkleForBinding(CleaningSpotBinding binding)
    {
        if (binding.sparkleObject != null || binding.spotRenderer == null)
        {
            return;
        }

        GameObject sparkleObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sparkleObject.name = $"{binding.spotRenderer.name}_Sparkle";
        sparkleObject.transform.SetPositionAndRotation(
            binding.spotRenderer.transform.position + binding.spotRenderer.transform.forward * 0.02f,
            binding.spotRenderer.transform.rotation);
        sparkleObject.transform.SetParent(binding.spotRenderer.transform, true);
        sparkleObject.transform.localScale = Vector3.one * 1.08f;

        Collider sparkleCollider = sparkleObject.GetComponent<Collider>();
        if (sparkleCollider != null)
        {
            Destroy(sparkleCollider);
        }

        Renderer sparkleRenderer = sparkleObject.GetComponent<Renderer>();
        Shader sparkleShader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Texture");
        Material sparkleMaterial = new Material(sparkleShader);
        sparkleMaterial.mainTexture = sparkleTexture1 != null ? sparkleTexture1 : sparkleTexture2;
        sparkleRenderer.material = sparkleMaterial;

        binding.sparkleObject = sparkleObject;
        binding.sparkleRenderer = sparkleRenderer;
        binding.sparkleMaterial = sparkleMaterial;
        binding.sparkleObject.SetActive(false);
    }

    private void ResetVisualSpots()
    {
        for (int i = 0; i < spotBindings.Count; i++)
        {
            CleaningSpotBinding binding = spotBindings[i];
            if (binding.spotRenderer == null)
            {
                continue;
            }

            binding.spotRenderer.enabled = true;
            binding.wipePasses = 0;
            binding.swipeDistance = 0f;
            RestoreSpotColliders(binding);

            if (binding.runtimeMaterial == null)
            {
                binding.runtimeMaterial = binding.spotRenderer.material;
            }

            if (binding.sparkleObject != null)
            {
                binding.sparkleObject.SetActive(false);
            }

            SetMaterialAlpha(binding.runtimeMaterial, 1f, binding.originalColor);
        }
    }

    private void UpdateVisualSpots()
    {
        for (int i = 0; i < spotBindings.Count; i++)
        {
            CleaningSpotBinding binding = spotBindings[i];
            CleaningMinigame.CleaningTask task = FindTask(binding.faultId);
            if (task == null || binding.spotRenderer == null)
            {
                continue;
            }

            if (task.Completed || binding.wipePasses >= requiredWipePasses)
            {
                binding.spotRenderer.enabled = false;
                SetSpotCollidersEnabled(binding, false);
                continue;
            }

            binding.spotRenderer.enabled = true;
            RestoreSpotColliders(binding);
        }
    }

    private void RestoreSpotColliders(CleaningSpotBinding binding)
    {
        if (binding.spotColliders == null || binding.originalColliderEnabled == null)
        {
            return;
        }

        for (int i = 0; i < binding.spotColliders.Length; i++)
        {
            if (binding.spotColliders[i] != null && i < binding.originalColliderEnabled.Length)
            {
                binding.spotColliders[i].enabled = binding.originalColliderEnabled[i];
            }
        }
    }

    private void SetSpotCollidersEnabled(CleaningSpotBinding binding, bool enabled)
    {
        if (binding.spotColliders == null)
        {
            return;
        }

        for (int i = 0; i < binding.spotColliders.Length; i++)
        {
            if (binding.spotColliders[i] != null)
            {
                binding.spotColliders[i].enabled = enabled;
            }
        }
    }

    private Rect GetTesterPanelRect()
    {
        return _showGuideMenu ? GetGuideMenuRect() : Rect.zero;
    }

    private bool IsMouseOverBlockedUi()
    {
        Vector2 guiMousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (GetClothButtonRect().Contains(guiMousePosition))
        {
            return true;
        }

        if (GetScrewButtonRect().Contains(guiMousePosition))
        {
            return true;
        }

        if (GetFinishButtonRect().Contains(guiMousePosition))
        {
            return true;
        }

        return _showGuideMenu && GetTesterPanelRect().Contains(guiMousePosition);
    }

    private Rect GetGuideMenuRect()
    {
        float width = Mathf.Min(420f, Screen.width - 40f);
        float height = 230f;
        return new Rect((Screen.width - width) * 0.5f, 76f, width, height);
    }

    private Rect GetClothButtonRect()
    {
        const float size = 72f;
        const float gap = 12f;
        return new Rect(Screen.width - size * 2f - gap - 24f, Screen.height - size - 24f, size, size);
    }

    private Rect GetScrewButtonRect()
    {
        const float size = 72f;
        return new Rect(Screen.width - size - 24f, Screen.height - size - 24f, size, size);
    }

    private Rect GetFinishButtonRect()
    {
        return new Rect(Screen.width - 132f, Screen.height - 124f, 108f, 36f);
    }

    private void UpdateToolVisibility()
    {
        SetObjectActive(cleaningToolObject, false);
        SetObjectActive(_screwdriverToolObject, false);
    }

    private void SetActiveCleaningTarget(CleaningTargetBinding activeTarget)
    {
        for (int i = 0; i < _targetBindings.Count; i++)
        {
            CleaningTargetBinding target = _targetBindings[i];
            if (target == null || target.rootObject == null)
            {
                continue;
            }

            SetObjectActive(target.rootObject, target == activeTarget);
        }

        if (activeTarget != null && activeTarget.rootObject != null)
        {
            orbitTarget = activeTarget.rootObject.transform;
            FrameCameraOnTarget(activeTarget.rootObject.transform);
        }
    }

    private void ShowTargetSelection()
    {
        _isChoosingTarget = true;
        _activeTargetBinding = null;
        _activeTargetIndex = -1;
        _activeSwipeBinding = null;
        _screwPhaseActive = false;
        _screwPhaseCompleted = false;
        _isClothSelected = false;
        _isScrewSelected = false;
        Cursor.visible = true;
        SetActiveCleaningTarget(null);
        UpdateToolVisibility();
    }

    private void ResetTargetCompletion()
    {
        for (int i = 0; i < _targetBindings.Count; i++)
        {
            if (_targetBindings[i] != null)
            {
                _targetBindings[i].completed = false;
            }
        }
    }

    private List<CleaningTargetBinding> GetAvailableTargets()
    {
        List<CleaningTargetBinding> availableTargets = new List<CleaningTargetBinding>();
        for (int i = 0; i < _targetOrder.Count; i++)
        {
            CleaningTargetBinding target = _targetOrder[i];
            if (target != null && target.rootObject != null && !target.completed)
            {
                availableTargets.Add(target);
            }
        }

        return availableTargets;
    }

    private bool HasAvailableTargets()
    {
        for (int i = 0; i < _targetOrder.Count; i++)
        {
            CleaningTargetBinding target = _targetOrder[i];
            if (target != null && target.rootObject != null && !target.completed)
            {
                return true;
            }
        }

        return false;
    }

    private string GetTargetDisplayName(string targetName)
    {
        if (targetName == "OldTableFan")
        {
            return "Old Table Fan";
        }

        if (targetName == "InductionCooker")
        {
            return "Induction Cooker";
        }

        return targetName;
    }

    private void FrameCameraOnTarget(Transform target)
    {
        if (interactionCamera == null || target == null)
        {
            return;
        }

        Bounds targetBounds = CalculateTargetBounds(target);
        Vector3 lookPoint = targetBounds.center + cameraTargetOffset;
        Vector3 viewDirection = cameraViewOffset.sqrMagnitude > 0.001f
            ? cameraViewOffset.normalized
            : new Vector3(0f, 0.25f, -1f).normalized;
        float largestDimension = Mathf.Max(targetBounds.size.x, Mathf.Max(targetBounds.size.y, targetBounds.size.z));
        _cameraDistance = Mathf.Clamp(
            largestDimension * Mathf.Max(1f, cameraFrameDistanceMultiplier),
            minCameraDistance,
            maxCameraDistance);

        Transform cameraTransform = interactionCamera.transform;
        cameraTransform.position = lookPoint + viewDirection * _cameraDistance;
        cameraTransform.LookAt(lookPoint);
        _orbitVerticalAngle = 0f;
    }

    private void AdjustCameraZoom(float distanceDelta)
    {
        if (interactionCamera == null || orbitTarget == null)
        {
            return;
        }

        Bounds targetBounds = CalculateTargetBounds(orbitTarget);
        Vector3 lookPoint = targetBounds.center + cameraTargetOffset;
        Transform cameraTransform = interactionCamera.transform;
        Vector3 fromTarget = cameraTransform.position - lookPoint;
        if (fromTarget.sqrMagnitude < 0.001f)
        {
            fromTarget = cameraViewOffset.sqrMagnitude > 0.001f
                ? cameraViewOffset.normalized
                : new Vector3(0f, 0.25f, -1f).normalized;
        }

        _cameraDistance = Mathf.Clamp(fromTarget.magnitude + distanceDelta, minCameraDistance, maxCameraDistance);
        cameraTransform.position = lookPoint + fromTarget.normalized * _cameraDistance;
        cameraTransform.LookAt(lookPoint);
    }

    private Bounds CalculateTargetBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(target.position, Vector3.one);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || !targetRenderer.enabled || !targetRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(targetRenderer.bounds);
            }
        }

        return hasBounds ? bounds : bounds;
    }

    private bool IsBindingOnActiveTarget(CleaningSpotBinding binding)
    {
        return binding != null &&
               _activeTargetBinding != null &&
               binding.targetName == _activeTargetBinding.targetName;
    }

    private bool IsBindingOnActiveTarget(ScrewBinding binding)
    {
        return binding != null &&
               _activeTargetBinding != null &&
               binding.targetName == _activeTargetBinding.targetName;
    }

    private bool ActiveTargetRequiresPhysicalSpotHit()
    {
        return _activeTargetBinding != null && _activeTargetBinding.targetName == "InductionCooker";
    }

    private int CountActiveScrews()
    {
        int count = 0;
        for (int i = 0; i < screwBindings.Count; i++)
        {
            if (IsBindingOnActiveTarget(screwBindings[i]))
            {
                count++;
            }
        }

        return count;
    }

    private int CountAllInstalledScrews()
    {
        int count = 0;
        for (int i = 0; i < screwBindings.Count; i++)
        {
            if (screwBindings[i] != null && screwBindings[i].installed)
            {
                count++;
            }
        }

        return count;
    }

    private int GetTotalDustSpotCount()
    {
        return spotBindings.Count;
    }

    private int GetTotalScrewCount()
    {
        return screwBindings.Count;
    }

    private void ResetScrewsForTarget(CleaningTargetBinding target, bool visible)
    {
        if (target == null)
        {
            return;
        }

        for (int i = 0; i < screwBindings.Count; i++)
        {
            ScrewBinding binding = screwBindings[i];
            if (binding != null && binding.targetName == target.targetName)
            {
                ResetScrewBinding(binding, visible);
            }
        }
    }

    private CleaningTargetBinding FindTargetBinding(string targetName)
    {
        for (int i = 0; i < _targetBindings.Count; i++)
        {
            if (_targetBindings[i] != null && _targetBindings[i].targetName == targetName)
            {
                return _targetBindings[i];
            }
        }

        return null;
    }

    private string GetTargetFaultId(CleaningTargetBinding target, int dustIndex)
    {
        string normalizedName = target.targetName.ToLowerInvariant();
        return $"{normalizedName}_dust_spot_{dustIndex + 1:00}";
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private void SetScrewCollidersEnabled(ScrewBinding binding, bool enabled)
    {
        if (binding == null || binding.colliders == null)
        {
            return;
        }

        for (int i = 0; i < binding.colliders.Length; i++)
        {
            if (binding.colliders[i] != null)
            {
                binding.colliders[i].enabled = enabled;
            }
        }
    }

    private CleaningSpotBinding FindSpotBinding(string faultId)
    {
        for (int i = 0; i < spotBindings.Count; i++)
        {
            if (spotBindings[i] != null && spotBindings[i].faultId == faultId)
            {
                return spotBindings[i];
            }
        }

        return null;
    }

    private ScrewBinding FindScrewBinding(string targetName, string objectName)
    {
        for (int i = 0; i < screwBindings.Count; i++)
        {
            if (screwBindings[i] != null &&
                screwBindings[i].targetName == targetName &&
                screwBindings[i].objectName == objectName)
            {
                return screwBindings[i];
            }
        }

        return null;
    }

    private CleaningMinigame.CleaningTask FindTask(string faultId)
    {
        for (int i = 0; i < _minigame.Tasks.Count; i++)
        {
            if (_minigame.Tasks[i].FaultId == faultId)
            {
                return _minigame.Tasks[i];
            }
        }

        return null;
    }

    private int FindTaskIndex(string faultId)
    {
        for (int i = 0; i < _minigame.Tasks.Count; i++)
        {
            if (_minigame.Tasks[i].FaultId == faultId)
            {
                return i;
            }
        }

        return -1;
    }

    private Color GetMaterialColor(Material material)
    {
        if (material == null)
        {
            return Color.white;
        }

        if (material.HasProperty("_Color"))
        {
            return material.color;
        }

        return Color.white;
    }

    private void SetMaterialAlpha(Material material, float alpha, Color fallbackColor)
    {
        if (material == null || !material.HasProperty("_Color"))
        {
            return;
        }

        Color color = fallbackColor;
        color.a = Mathf.Clamp01(alpha);
        material.color = color;
    }
}


