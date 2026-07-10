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
        [HideInInspector] public Texture2D eraseTexture;
        [HideInInspector] public Color32[] originalPixels;
        [HideInInspector] public Color32[] erasePixels;
        [HideInInspector] public int originalVisiblePixels;
        [HideInInspector] public int erasedVisiblePixels;
    }

    [Header("Test Settings")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private int clothEraseRadiusPixels = 800;
    [SerializeField] private int clothEraseStepPixels = 24;
    [SerializeField] private int screenSpaceSpotPaddingPixels = 32;
    [SerializeField] private byte visibleAlphaThreshold = 24;
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private Camera interactionCamera;

    [Header("UI Textures")]
    [SerializeField] private Texture2D clothButtonTexture;
    [SerializeField] private Texture2D clothCursorTexture;

    [Header("Visual Spots")]
    [SerializeField] private List<CleaningSpotBinding> spotBindings = new List<CleaningSpotBinding>();

    [Header("Visual Tools")]
    [SerializeField] private GameObject cleaningToolObject;

    private CleaningMinigame _minigame;
    private int _selectedTaskIndex;
    private GameObject _screwdriverToolObject;
    private bool _showGuideMenu;
    private bool _isClothSelected;
    private CleaningSpotBinding _lastEraseBinding;
    private Vector2 _lastEraseTextureCoord;

    private readonly List<string> _sampleFaults = new List<string>
    {
        "dust_cover",
        "rust_contact"
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

        if (!_minigame.IsActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            SelectNextTask();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetSelectedTask(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetSelectedTask(1);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Quick);
            Debug.Log("[CleaningTester] Mode: Quick");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
            Debug.Log("[CleaningTester] Mode: Thorough");
        }

        HandleMouseCleaning();

        if (Input.GetKeyDown(KeyCode.X))
        {
            _minigame.RegisterMistake();
            Debug.Log("[CleaningTester] Forced mistake.");
            LogSelectedTask();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            TryFinishMinigame();
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

        return "ENDED EARLY / NOT FINISHED";
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
        GUILayout.Label("2. Rê khăn lau qua đâu thì vết bụi hoặc gỉ sét trên đường rê sẽ mất ở đó.");
        GUILayout.Label("3. Lau hết Dust đạt 80% thì có thể bấm FINISH.");
        GUILayout.Label("4. Lau thêm Rust để đạt 100% và có cơ hội Perfect.");
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
        bool canFinish = _minigame.IsActive && _minigame.CanFinish;

        GUI.enabled = canFinish;
        if (GUI.Button(finishRect, "FINISH"))
        {
            TryFinishMinigame();
        }

        GUI.enabled = true;
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
        Cursor.visible = true;
        _minigame.Initialize(_sampleFaults, difficultyLevel);
        _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
        _minigame.StartMinigame();
        ResetVisualSpots();
        UpdateVisualSpots();
        UpdateToolVisibility();

        Debug.Log("[CleaningTester] Started Cleaning minigame test.");
        Debug.Log("[CleaningTester] Drag mouse over dust/rust spots with Cloth selected. Press R to toggle the guide menu.");
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
            ResetEraseStroke();
            return;
        }

        CleaningSpotBinding hitBinding = FindSpotUnderMouse(out Vector2 textureCoord);
        if (hitBinding == null)
        {
            ResetEraseStroke();
            return;
        }

        int taskIndex = FindTaskIndex(hitBinding.faultId);
        if (taskIndex < 0)
        {
            ResetEraseStroke();
            return;
        }

        CleaningMinigame.CleaningTask task = _minigame.Tasks[taskIndex];
        if (task.Completed)
        {
            ResetEraseStroke();
            return;
        }

        _selectedTaskIndex = taskIndex;

        float previousProgress = task.Progress;
        EraseSpotStroke(hitBinding, textureCoord);
        float erasedProgress = GetSpotEraseProgress(hitBinding);
        float progressDelta = Mathf.Clamp01(erasedProgress) - previousProgress;
        if (progressDelta > 0.001f)
        {
            _minigame.CleanTask(taskIndex, progressDelta * task.RequiredWork);
            UpdateVisualSpots();
            UpdateToolVisibility();
        }
    }

    private void ResetEraseStroke()
    {
        _lastEraseBinding = null;
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
        UpdateToolVisibility();
        Debug.Log($"[CleaningTester] Completed. Final quality: {quality}");
    }

    private void TryFinishMinigame()
    {
        if (!_minigame.TryFinishMinigame(out RepairQuality quality))
        {
            Debug.Log($"[CleaningTester] Finish locked. Clean at least 80%. Current quality preview: {quality}");
            return;
        }

        UpdateToolVisibility();
        Debug.Log($"[CleaningTester] Finished manually. Quality: {quality}");
    }

    private void AutoBindSceneSpots()
    {
        TryAddBinding("dust_cover", "DustSpot_01");
        TryAddBinding("rust_contact", "RustSpot_01");
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
#endif
    }

    private void TryAddBinding(string faultId, string objectName)
    {
        if (FindSpotBinding(faultId) != null)
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

            Texture2D sourceTexture = binding.runtimeMaterial.mainTexture as Texture2D;
            if (sourceTexture == null)
            {
                Debug.LogWarning($"[CleaningTester] Spot material has no Texture2D: {binding.faultId}");
                continue;
            }

            binding.eraseTexture = CreateReadableTextureCopy(sourceTexture, $"{sourceTexture.name}_RuntimeErase");
            binding.originalPixels = binding.eraseTexture.GetPixels32();
            binding.erasePixels = new Color32[binding.originalPixels.Length];
            Array.Copy(binding.originalPixels, binding.erasePixels, binding.originalPixels.Length);
            binding.originalVisiblePixels = CountVisiblePixels(binding.originalPixels);
            binding.erasedVisiblePixels = 0;

            binding.runtimeMaterial.mainTexture = binding.eraseTexture;
        }
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

            if (binding.runtimeMaterial == null)
            {
                binding.runtimeMaterial = binding.spotRenderer.material;
            }

            if (binding.eraseTexture != null && binding.originalPixels != null)
            {
                Array.Copy(binding.originalPixels, binding.erasePixels, binding.originalPixels.Length);
                binding.eraseTexture.SetPixels32(binding.erasePixels);
                binding.eraseTexture.Apply();
                binding.erasedVisiblePixels = 0;
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

            if (task.Completed || GetSpotEraseProgress(binding) >= 0.995f)
            {
                binding.spotRenderer.enabled = false;
                continue;
            }

            binding.spotRenderer.enabled = true;
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

    private void EraseSpotStroke(CleaningSpotBinding binding, Vector2 textureCoord)
    {
        if (_lastEraseBinding != binding)
        {
            EraseSpotAt(binding, textureCoord);
            _lastEraseBinding = binding;
            _lastEraseTextureCoord = textureCoord;
            return;
        }

        int width = binding.eraseTexture != null ? binding.eraseTexture.width : 1;
        int height = binding.eraseTexture != null ? binding.eraseTexture.height : 1;
        Vector2 pixelStart = new Vector2(_lastEraseTextureCoord.x * width, _lastEraseTextureCoord.y * height);
        Vector2 pixelEnd = new Vector2(textureCoord.x * width, textureCoord.y * height);
        float pixelDistance = Vector2.Distance(pixelStart, pixelEnd);
        int steps = Mathf.Max(1, Mathf.CeilToInt(pixelDistance / Mathf.Max(1, clothEraseStepPixels)));

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            EraseSpotAt(binding, Vector2.Lerp(_lastEraseTextureCoord, textureCoord, t));
        }

        _lastEraseTextureCoord = textureCoord;
    }

    private void EraseSpotAt(CleaningSpotBinding binding, Vector2 textureCoord)
    {
        if (binding.eraseTexture == null || binding.erasePixels == null || binding.originalVisiblePixels <= 0)
        {
            return;
        }

        int width = binding.eraseTexture.width;
        int height = binding.eraseTexture.height;
        int centerX = Mathf.RoundToInt(textureCoord.x * (width - 1));
        int centerY = Mathf.RoundToInt(textureCoord.y * (height - 1));
        int radius = Mathf.Max(1, clothEraseRadiusPixels);
        int radiusSquared = radius * radius;
        bool changed = false;

        int minX = Mathf.Max(0, centerX - radius);
        int maxX = Mathf.Min(width - 1, centerX + radius);
        int minY = Mathf.Max(0, centerY - radius);
        int maxY = Mathf.Min(height - 1, centerY + radius);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - centerY;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - centerX;
                if (dx * dx + dy * dy > radiusSquared)
                {
                    continue;
                }

                int pixelIndex = y * width + x;
                if (binding.originalPixels[pixelIndex].a <= visibleAlphaThreshold || binding.erasePixels[pixelIndex].a <= visibleAlphaThreshold)
                {
                    continue;
                }

                Color32 pixel = binding.erasePixels[pixelIndex];
                pixel.a = 0;
                binding.erasePixels[pixelIndex] = pixel;
                binding.erasedVisiblePixels++;
                changed = true;
            }
        }

        if (changed)
        {
            binding.eraseTexture.SetPixels32(binding.erasePixels);
            binding.eraseTexture.Apply();
        }
    }

    private float GetSpotEraseProgress(CleaningSpotBinding binding)
    {
        if (binding.originalVisiblePixels <= 0)
        {
            return 1f;
        }

        return Mathf.Clamp01((float)binding.erasedVisiblePixels / binding.originalVisiblePixels);
    }

    private int CountVisiblePixels(Color32[] pixels)
    {
        int visibleCount = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > visibleAlphaThreshold)
            {
                visibleCount++;
            }
        }

        return visibleCount;
    }

    private Texture2D CreateReadableTextureCopy(Texture2D sourceTexture, string textureName)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            sourceTexture.width,
            sourceTexture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);

        Graphics.Blit(sourceTexture, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D readableCopy = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        readableCopy.name = textureName;
        readableCopy.ReadPixels(new Rect(0f, 0f, sourceTexture.width, sourceTexture.height), 0, 0);
        readableCopy.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);
        return readableCopy;
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
