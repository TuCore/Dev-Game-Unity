using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary keyboard tester for CleaningMinigame.
/// Attach this to a GameObject together with CleaningMinigame, then press Play.
/// </summary>
[RequireComponent(typeof(CleaningMinigame))]
public class CleaningMinigameTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private int difficultyLevel = 1;
    [SerializeField] private float cleanInputAmount = 0.35f;
    [SerializeField] private float screwInputAmount = 0.5f;
    [SerializeField] private bool autoStartOnPlay = true;

    private CleaningMinigame _minigame;
    private int _selectedTaskIndex;

    private readonly List<string> _sampleFaults = new List<string>
    {
        "dirt_filter",
        "rust_contact",
        "loose_screw",
        "dust_cover"
    };

    private void Awake()
    {
        _minigame = GetComponent<CleaningMinigame>();
        _minigame.OnMinigameCompleted += HandleMinigameCompleted;
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
            bool success = _minigame.CleanTask(_selectedTaskIndex, cleanInputAmount);
            LogActionResult("Clean", success);
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
            Debug.Log($"[CleaningTester] Manual end. Quality: {quality}");
        }
    }

    private void OnGUI()
    {
        if (_minigame == null)
        {
            return;
        }

        GUI.skin.label.fontSize = 13;
        GUI.skin.button.fontSize = 13;
        GUI.skin.box.fontSize = 14;

        GUI.Box(new Rect(10f, 10f, 360f, 365f), "MINIGAME 4 - CLEANING");
        GUILayout.BeginArea(new Rect(20f, 34f, 340f, 330f));

        GUILayout.Label($"Status: {GetTestStatus()}");
        GUILayout.Label($"Active: {_minigame.IsActive}    Mode: {_minigame.CurrentMode}");
        GUILayout.Label($"Done Tasks: {CountCompletedTasks()}/{_minigame.Tasks.Count}    Progress: {_minigame.CompletionRatio:P0}");
        GUILayout.Label($"Mistakes: {_minigame.Mistakes}    Quality: {_minigame.PreviewRepairQuality()}");

        if (_minigame.Tasks.Count > 0)
        {
            CleaningMinigame.CleaningTask task = _minigame.Tasks[_selectedTaskIndex];
            GUILayout.Space(4f);
            GUILayout.Label($"Task #{_selectedTaskIndex + 1}/{_minigame.Tasks.Count}: {task.FaultId}");
            GUILayout.Label($"Type: {task.TaskType}    Task Progress: {task.Progress:P0}    Done: {task.Completed}");

            if (task.TaskType == CleaningMinigame.CleaningTaskType.LooseScrew)
            {
                GUILayout.Label($"Action: TIGHTEN SCREW ({task.RequiredDirection})");
            }
            else
            {
                GUILayout.Label("Action: CLEAN");
            }
        }

        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("RESTART", GUILayout.Height(30f)))
        {
            RestartTest();
        }

        if (GUILayout.Button("PREV", GUILayout.Height(30f)))
        {
            SelectPreviousTask();
        }

        if (GUILayout.Button("NEXT", GUILayout.Height(30f)))
        {
            SelectNextTask();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("QUICK", GUILayout.Height(30f)))
        {
            _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Quick);
            Debug.Log("[CleaningTester] Mode: Quick");
        }

        if (GUILayout.Button("THOROUGH", GUILayout.Height(30f)))
        {
            _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
            Debug.Log("[CleaningTester] Mode: Thorough");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CLEAN", GUILayout.Height(38f)))
        {
            bool success = _minigame.CleanTask(_selectedTaskIndex, cleanInputAmount);
            LogActionResult("Clean", success);
        }

        if (GUILayout.Button("TIGHTEN", GUILayout.Height(38f)))
        {
            TightenSelectedScrewWithCorrectDirection();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("MISTAKE", GUILayout.Height(30f)))
        {
            _minigame.RegisterMistake();
            Debug.Log("[CleaningTester] Forced mistake.");
            LogSelectedTask();
        }

        if (GUILayout.Button("FORCE END", GUILayout.Height(30f)))
        {
            RepairQuality quality = _minigame.EndMinigame();
            Debug.Log($"[CleaningTester] Manual end. Quality: {quality}");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        GUILayout.Label("Keys: N next | Space clean | T tighten | R restart");

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
        _minigame.Initialize(_sampleFaults, difficultyLevel);
        _minigame.SetCleaningMode(CleaningMinigame.CleaningMode.Thorough);
        _minigame.StartMinigame();

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
        LogSelectedTask();
    }

    private void TightenSelectedScrewWithCorrectDirection()
    {
        if (_selectedTaskIndex < 0 || _selectedTaskIndex >= _minigame.Tasks.Count)
        {
            return;
        }

        CleaningMinigame.CleaningTask task = _minigame.Tasks[_selectedTaskIndex];
        bool success = _minigame.TightenScrew(_selectedTaskIndex, task.RequiredDirection, screwInputAmount);
        LogActionResult("Tighten", success);
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
        Debug.Log($"[CleaningTester] Completed. Final quality: {quality}");
    }
}
