using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Temporary keyboard tester for CleaningMinigame.
/// Attach this to a GameObject together with CleaningMinigame, then press Play.
/// </summary>
[RequireComponent(typeof(CleaningMinigame))]
public class CleaningMinigameTester : MonoBehaviour
{
    private enum CleaningItem
    {
        Cloth,
        Screwdriver
    }

    [Serializable]
    private class CleaningSpotBinding
    {
        public string faultId;
        public Renderer spotRenderer;

        [HideInInspector] public Material runtimeMaterial;
        [HideInInspector] public Color originalColor;
    }

    [Header("Test Settings")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private float cleanInputAmount = 0.35f;
    [SerializeField] private float screwInputAmount = 0.5f;
    [SerializeField] private bool autoStartOnPlay = true;

    [Header("Visual Spots")]
    [SerializeField] private List<CleaningSpotBinding> spotBindings = new List<CleaningSpotBinding>();

    [Header("Visual Tools")]
    [SerializeField] private GameObject cleaningToolObject;
    [SerializeField] private GameObject screwdriverToolObject;

    [Header("Selected Item")]
    [SerializeField] private CleaningItem selectedItem = CleaningItem.Cloth;

    private CleaningMinigame _minigame;
    private int _selectedTaskIndex;

    private readonly List<string> _sampleFaults = new List<string>
    {
        "dust_cover",
        "rust_contact",
        "loose_screw"
    };

    private void Awake()
    {
        _minigame = GetComponent<CleaningMinigame>();
        _minigame.OnMinigameCompleted += HandleMinigameCompleted;
        AutoBindSceneSpots();
        AutoBindSceneTools();
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
        if (_minigame != null)
        {
            _minigame.OnMinigameCompleted -= HandleMinigameCompleted;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartTest();
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

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetSelectedTask(2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetSelectedTask(3);
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CleanSelectedTask();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TightenSelectedScrewWithCorrectDirection();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            _minigame.RegisterMistake();
            Debug.Log("[CleaningTester] Forced mistake.");
            LogSelectedTask();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            RepairQuality quality = _minigame.EndMinigame();
            UpdateToolVisibility();
            Debug.Log($"[CleaningTester] Manual end. Quality: {quality}");
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

        float panelX = Mathf.Min(72f, Screen.width * 0.08f);
        float panelY = 14f;
        float panelWidth = 270f;
        float panelHeight = 310f;
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "MINIGAME 4");
        GUILayout.BeginArea(new Rect(panelX + 8f, panelY + 22f, panelWidth - 16f, panelHeight - 28f));

        GUILayout.Label($"Status: {GetTestStatus()}");
        GUILayout.Label($"Mode: {_minigame.CurrentMode} | Item: {GetSelectedItemLabel()}");
        DrawItemSlots();
        GUILayout.Label($"Done: {CountCompletedTasks()}/{_minigame.Tasks.Count} | Progress: {_minigame.CompletionRatio:P0} | Mistakes: {_minigame.Mistakes}");
        GUILayout.Label($"Quality: {_minigame.PreviewRepairQuality()}");

        if (_minigame.Tasks.Count > 0)
        {
            CleaningMinigame.CleaningTask task = _minigame.Tasks[_selectedTaskIndex];
            GUILayout.Label($"Task #{_selectedTaskIndex + 1}: {task.FaultId}");
            GUILayout.Label($"{task.TaskType} | Task: {task.Progress:P0} | Done: {task.Completed}");

            if (task.TaskType == CleaningMinigame.CleaningTaskType.LooseScrew)
            {
                GUILayout.Label($"Action: TIGHTEN ({task.RequiredDirection})");
            }
            else
            {
                GUILayout.Label("Action: CLEAN");
            }

            GUILayout.Label($"Required Item: {GetRequiredItemLabel(task)}");
        }

        GUILayout.Space(2f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("RESTART", GUILayout.Height(22f)))
        {
            RestartTest();
        }

        if (GUILayout.Button("PREV", GUILayout.Height(22f)))
        {
            SelectPreviousTask();
        }

        if (GUILayout.Button("NEXT", GUILayout.Height(22f)))
        {
            SelectNextTask();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("QUICK", GUILayout.Height(22f)))
        {
            _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Quick);
            Debug.Log("[CleaningTester] Mode: Quick");
        }

        if (GUILayout.Button("THOROUGH", GUILayout.Height(22f)))
        {
            _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
            Debug.Log("[CleaningTester] Mode: Thorough");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CLEAN", GUILayout.Height(24f)))
        {
            CleanSelectedTask();
        }

        if (GUILayout.Button("TIGHTEN", GUILayout.Height(24f)))
        {
            TightenSelectedScrewWithCorrectDirection();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("MISTAKE", GUILayout.Height(22f)))
        {
            _minigame.RegisterMistake();
            Debug.Log("[CleaningTester] Forced mistake.");
            LogSelectedTask();
        }

        if (GUILayout.Button("FORCE END", GUILayout.Height(22f)))
        {
            RepairQuality quality = _minigame.EndMinigame();
            UpdateToolVisibility();
            Debug.Log($"[CleaningTester] Manual end. Quality: {quality}");
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Keys: N next | Space clean | T tighten | R reset");
        GUILayout.EndArea();
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

    private void RestartTest()
    {
        _selectedTaskIndex = 0;
        selectedItem = CleaningItem.Cloth;
        _minigame.Initialize(_sampleFaults, difficultyLevel);
        _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
        _minigame.StartMinigame();
        ResetVisualSpots();
        UpdateVisualSpots();
        UpdateToolVisibility();

        Debug.Log("[CleaningTester] Started Cleaning minigame test.");
        Debug.Log("[CleaningTester] Keys: 1-4 select, N next, Space clean, T tighten screw, Q quick, E thorough, X mistake, Enter end, R restart.");
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

    private void TightenSelectedScrewWithCorrectDirection()
    {
        if (_selectedTaskIndex < 0 || _selectedTaskIndex >= _minigame.Tasks.Count)
        {
            return;
        }

        if (!EnsureSelectedItemMatchesTask("Tighten"))
        {
            return;
        }

        CleaningMinigame.CleaningTask task = _minigame.Tasks[_selectedTaskIndex];
        bool success = _minigame.TightenScrew(_selectedTaskIndex, task.RequiredDirection, screwInputAmount);
        UpdateToolVisibility();
        LogActionResult("Tighten", success);
    }

    private void CleanSelectedTask()
    {
        if (_selectedTaskIndex < 0 || _selectedTaskIndex >= _minigame.Tasks.Count)
        {
            return;
        }

        if (!EnsureSelectedItemMatchesTask("Clean"))
        {
            return;
        }

        bool success = _minigame.CleanTask(_selectedTaskIndex, cleanInputAmount);
        UpdateVisualSpots();
        UpdateToolVisibility();
        LogActionResult("Clean", success);
    }

    private bool EnsureSelectedItemMatchesTask(string actionName)
    {
        CleaningMinigame.CleaningTask task = _minigame.Tasks[_selectedTaskIndex];
        CleaningItem requiredItem = GetRequiredItem(task);
        if (selectedItem == requiredItem)
        {
            return true;
        }

        _minigame.RegisterMistake();
        UpdateToolVisibility();
        Debug.LogWarning(
            $"[CleaningTester] {actionName} blocked: task {task.FaultId} requires {requiredItem}, " +
            $"but selected item is {selectedItem}. Mistakes={_minigame.Mistakes}, " +
            $"quality={_minigame.PreviewRepairQuality()}");
        LogSelectedTask();
        return false;
    }

    private void LogActionResult(string actionName, bool success)
    {
        Debug.Log($"[CleaningTester] {actionName}: {(success ? "success" : "failed")}");
        LogSelectedTask();
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
        UpdateToolVisibility();
        Debug.Log($"[CleaningTester] Completed. Final quality: {quality}");
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

        if (screwdriverToolObject == null)
        {
            screwdriverToolObject = FindSceneObjectByName("Screwdriver");
        }
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

            float alpha = 1f - task.Progress;
            if (task.Completed || alpha <= 0.01f)
            {
                binding.spotRenderer.enabled = false;
                continue;
            }

            binding.spotRenderer.enabled = true;
            SetMaterialAlpha(binding.runtimeMaterial, alpha, binding.originalColor);
        }
    }

    private void UpdateToolVisibility()
    {
        bool showCleaningTool = _minigame.IsActive && selectedItem == CleaningItem.Cloth;
        bool showScrewdriver = _minigame.IsActive && selectedItem == CleaningItem.Screwdriver;

        SetObjectActive(cleaningToolObject, showCleaningTool);
        SetObjectActive(screwdriverToolObject, showScrewdriver);
    }

    private void DrawItemSlots()
    {
        GUILayout.BeginHorizontal();
        DrawItemSlot(CleaningItem.Cloth, "CLOTH");
        DrawItemSlot(CleaningItem.Screwdriver, "SCREWDRIVER");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void DrawItemSlot(CleaningItem item, string label)
    {
        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = selectedItem == item
            ? new Color(0.75f, 0.9f, 1f)
            : new Color(0.45f, 0.45f, 0.45f);

        string buttonText = selectedItem == item ? $"{label}\n[E]" : $"{label}\nITEM";
        if (GUILayout.Button(buttonText, GUILayout.Width(84f), GUILayout.Height(36f)))
        {
            SelectItem(item);
        }

        GUI.backgroundColor = previousBackground;
    }

    private void SelectItem(CleaningItem item)
    {
        selectedItem = item;
        UpdateToolVisibility();
        Debug.Log($"[CleaningTester] Selected item: {selectedItem}");
    }

    private CleaningItem GetRequiredItem(CleaningMinigame.CleaningTask task)
    {
        return task.TaskType == CleaningMinigame.CleaningTaskType.LooseScrew
            ? CleaningItem.Screwdriver
            : CleaningItem.Cloth;
    }

    private string GetSelectedItemLabel()
    {
        return selectedItem == CleaningItem.Cloth ? "Cloth" : "Screwdriver";
    }

    private string GetRequiredItemLabel(CleaningMinigame.CleaningTask task)
    {
        return GetRequiredItem(task) == CleaningItem.Cloth ? "Cloth" : "Screwdriver";
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
