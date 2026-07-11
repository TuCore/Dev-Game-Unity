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
    [Serializable]
    private class CleaningSpotBinding
    {
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

    [Header("Test Settings")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private int screenSpaceSpotPaddingPixels = 32;
    [SerializeField] private float requiredSwipeDistancePixels = 70f;
    [SerializeField] private int requiredWipePasses = 5;
    [SerializeField] private float opacityLossPerWipe = 0.2f;
    [SerializeField] private float sparkleFrameSeconds = 0.18f;
    [SerializeField] private int sparkleFrameCount = 4;
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private Camera interactionCamera;

    [Header("UI Textures")]
    [SerializeField] private Texture2D clothButtonTexture;
    [SerializeField] private Texture2D clothCursorTexture;

    [Header("Visual Spots")]
    [SerializeField] private List<CleaningSpotBinding> spotBindings = new List<CleaningSpotBinding>();

    [Header("Visual Tools")]
    [SerializeField] private GameObject cleaningToolObject;

    [Header("Visual Effects")]
    [SerializeField] private Texture2D sparkleTexture1;
    [SerializeField] private Texture2D sparkleTexture2;

    private CleaningMinigame _minigame;
    private int _selectedTaskIndex;
    private GameObject _screwdriverToolObject;
    private bool _showGuideMenu;
    private bool _isClothSelected;
    private CleaningSpotBinding _activeSwipeBinding;
    private bool _hasRatingResult;
    private bool _showRatingResult;
    private RepairQuality _lastRatingResult;

    private readonly List<string> _sampleFaults = new List<string>
    {
        "dust_spot_01",
        "dust_spot_02",
        "dust_spot_03",
        "dust_spot_04",
        "dust_spot_05"
    };

    private void Awake()
    {
        _minigame = GetComponent<CleaningMinigame>();
        _minigame.OnMinigameCompleted += HandleMinigameCompleted;
        AutoBindSceneSpots();
        AutoBindSceneTools();
        AutoBindInteractionCamera();
        AutoBindUiTextures();
        PrepareSpotMaterials();
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
        if (Input.GetKeyDown(KeyCode.R))
        {
            _showGuideMenu = !_showGuideMenu;
        }

        UpdateSparkles();

        if (!_minigame.IsActive)
        {
            return;
        }

        HandleMouseCleaning();

        if (Input.GetKeyDown(KeyCode.X))
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

        DrawCleaningProgressHud();
        DrawGuideHint();
        DrawFinishButton();
        DrawClothButton();

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

    private void DrawCleaningProgressHud()
    {
        int cleanPercent = Mathf.RoundToInt(_minigame.CompletionRatio * 100f);
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

    private void DrawGuideHint()
    {
        Rect hintRect = new Rect(12f, Screen.height - 48f, 190f, 34f);
        GUI.Box(hintRect, string.Empty);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };
        GUI.Label(hintRect, "Nhấn R để xem hướng dẫn", hintStyle);
    }

    private void DrawGuideMenu()
    {
        Rect menuRect = GetGuideMenuRect();
        GUI.Box(menuRect, "HƯỚNG DẪN");

        GUILayout.BeginArea(new Rect(menuRect.x + 16f, menuRect.y + 34f, menuRect.width - 32f, menuRect.height - 48f));
        GUI.skin.label.fontSize = 13;
        GUILayout.Label("1. Click chọn khăn lau ở góc dưới phải.");
        GUILayout.Label("2. Rê khăn lau trên mỗi vết bụi đủ khoảng cách để tính 1 lượt lau.");
        GUILayout.Label("3. Mỗi lượt lau làm vết bụi mờ đi 20%.");
        GUILayout.Label("4. Lau đủ 5 lượt cho cả 5 vết bụi để hoàn thành.");
        GUILayout.Label("5. Bấm FINISH để xem rating của lượt chơi.");
        GUILayout.Space(8f);
        GUILayout.Label("Nhấn R lần nữa để đóng hướng dẫn.");
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
        Rect ratingRect = new Rect((Screen.width - 260f) * 0.5f, 18f, 260f, 76f);
        GUI.Box(ratingRect, "RESULT");

        GUIStyle ratingStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(ratingRect.x + 12f, ratingRect.y + 28f, ratingRect.width - 24f, 34f), $"Rating: {_lastRatingResult}", ratingStyle);
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
        Cursor.visible = !_isClothSelected;
        UpdateToolVisibility();
    }

    private void RestartTest()
    {
        _selectedTaskIndex = 0;
        _isClothSelected = false;
        _activeSwipeBinding = null;
        _hasRatingResult = false;
        _showRatingResult = false;
        Cursor.visible = true;
        _minigame.Initialize(_sampleFaults, difficultyLevel);
        _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
        _minigame.StartMinigame();
        ResetVisualSpots();
        UpdateVisualSpots();
        UpdateToolVisibility();

        Debug.Log("[CleaningTester] Started Cleaning minigame test.");
        Debug.Log("[CleaningTester] Drag the selected Cloth over each DustSpot. Five valid swipes clean one spot.");
        LogSelectedTask();
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

    private CleaningSpotBinding FindSpotUnderMouse(out Vector2 textureCoord)
    {
        textureCoord = Vector2.zero;

        if (TryFindSpotUnderMouseOnScreen(out CleaningSpotBinding screenBinding, out textureCoord))
        {
            return screenBinding;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        if (hits.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < spotBindings.Count; i++)
        {
            CleaningSpotBinding binding = spotBindings[i];
            if (binding.spotRenderer == null)
            {
                continue;
            }

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                Transform hitTransform = hits[hitIndex].collider.transform;
                Transform spotTransform = binding.spotRenderer.transform;
                if (hitTransform == spotTransform || hitTransform.IsChildOf(spotTransform) || spotTransform.IsChildOf(hitTransform))
                {
                    textureCoord = hits[hitIndex].textureCoord;
                    return binding;
                }
            }
        }

        return null;
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
            if (binding.spotRenderer == null || !binding.spotRenderer.enabled)
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
        Cursor.visible = true;
        _lastRatingResult = quality;
        _hasRatingResult = true;
        UpdateToolVisibility();
        Debug.Log($"[CleaningTester] Completed. Final quality: {quality}");
    }

    private void FinishAndShowRating()
    {
        if (_minigame.IsActive)
        {
            RepairQuality quality = _minigame.EndMinigame();
            _lastRatingResult = quality;
            _hasRatingResult = true;
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

    private void AutoBindSceneSpots()
    {
        for (int i = spotBindings.Count - 1; i >= 0; i--)
        {
            if (spotBindings[i] == null || !_sampleFaults.Contains(spotBindings[i].faultId))
            {
                spotBindings.RemoveAt(i);
            }
        }

        for (int i = 0; i < _sampleFaults.Count; i++)
        {
            TryAddBinding(_sampleFaults[i], $"DustSpot_{i + 1:00}");
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

        if (sparkleTexture1 == null)
        {
            sparkleTexture1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Minigames/Cleaning/Sparkle_1.png");
        }

        if (sparkleTexture2 == null)
        {
            sparkleTexture2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Minigames/Cleaning/Sparkle_2.png");
        }
#endif
    }

    private void TryAddBinding(string faultId, string objectName)
    {
        CleaningSpotBinding existingBinding = FindSpotBinding(faultId);
        if (existingBinding != null && existingBinding.spotRenderer != null)
        {
            return;
        }

        GameObject spotObject = FindSceneObjectByName(objectName);
        if (spotObject == null)
        {
            Debug.LogWarning($"[CleaningTester] Could not find visual spot object: {objectName}");
            return;
        }

        Renderer spotRenderer = spotObject.GetComponent<Renderer>();
        if (spotRenderer == null)
        {
            Debug.LogWarning($"[CleaningTester] Visual spot has no Renderer: {objectName}");
            return;
        }

        if (existingBinding != null)
        {
            existingBinding.spotRenderer = spotRenderer;
            return;
        }

        spotBindings.Add(new CleaningSpotBinding
        {
            faultId = faultId,
            spotRenderer = spotRenderer
        });
    }

    private GameObject FindSceneObjectByName(string objectName)
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

        Debug.LogWarning($"[CleaningTester] Could not find scene object: {objectName}");
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

    private void CacheSpotColliders(CleaningSpotBinding binding)
    {
        if (binding.spotColliders != null || binding.spotRenderer == null)
        {
            return;
        }

        binding.spotColliders = binding.spotRenderer.GetComponentsInChildren<Collider>(true);
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
        sparkleObject.transform.localScale = binding.spotRenderer.transform.lossyScale * 1.08f;

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

        if (GetFinishButtonRect().Contains(guiMousePosition))
        {
            return true;
        }

        return _showGuideMenu && GetTesterPanelRect().Contains(guiMousePosition);
    }

    private Rect GetGuideMenuRect()
    {
        float width = Mathf.Min(420f, Screen.width - 40f);
        float height = 190f;
        return new Rect((Screen.width - width) * 0.5f, 76f, width, height);
    }

    private Rect GetClothButtonRect()
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

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private CleaningSpotBinding FindSpotBinding(string faultId)
    {
        for (int i = 0; i < spotBindings.Count; i++)
        {
            if (spotBindings[i].faultId == faultId)
            {
                return spotBindings[i];
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
