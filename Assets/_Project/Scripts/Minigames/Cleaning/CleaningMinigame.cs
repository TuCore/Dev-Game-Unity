using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minigame 4: Lap rap & ve sinh.
/// Source-only prototype: UI/Unity input can call CleanTask/TightenScrew later.
/// </summary>
public class CleaningMinigame : MonoBehaviour, IMinigame
{
    public enum CleaningMode
    {
        Quick,
        Thorough
    }

    public enum CleaningTaskType
    {
        Dirt,
        Rust,
        LooseScrew
    }

    public enum ScrewTurnDirection
    {
        Clockwise = 1,
        CounterClockwise = -1
    }

    [Serializable]
    public class CleaningTask
    {
        [SerializeField] private string faultId;
        [SerializeField] private CleaningTaskType taskType;
        [SerializeField] private Vector2 normalizedPosition;
        [SerializeField] private float requiredWork;
        [SerializeField] private float completedWork;
        [SerializeField] private ScrewTurnDirection requiredDirection;
        [SerializeField] private bool completed;

        public string FaultId => faultId;
        public CleaningTaskType TaskType => taskType;
        public Vector2 NormalizedPosition => normalizedPosition;
        public float RequiredWork => requiredWork;
        public float CompletedWork => completedWork;
        public ScrewTurnDirection RequiredDirection => requiredDirection;
        public bool Completed => completed;
        public float Progress => requiredWork <= 0f ? 1f : Mathf.Clamp01(completedWork / requiredWork);

        public CleaningTask(
            string faultId,
            CleaningTaskType taskType,
            Vector2 normalizedPosition,
            float requiredWork,
            ScrewTurnDirection requiredDirection)
        {
            this.faultId = faultId;
            this.taskType = taskType;
            this.normalizedPosition = normalizedPosition;
            this.requiredWork = Mathf.Max(0.1f, requiredWork);
            this.requiredDirection = requiredDirection;
            completedWork = 0f;
            completed = false;
        }

        public void AddWork(float amount)
        {
            if (completed) return;

            completedWork = Mathf.Clamp(completedWork + Mathf.Max(0f, amount), 0f, requiredWork);
            completed = completedWork >= requiredWork;
        }

        public bool TryTurnScrew(ScrewTurnDirection direction, float workAmount)
        {
            if (completed || taskType != CleaningTaskType.LooseScrew)
            {
                return false;
            }

            if (direction != requiredDirection)
            {
                return false;
            }

            AddWork(workAmount);
            return true;
        }
    }

    [Header("Cleaning Rules")]
    [SerializeField] private CleaningMode cleaningMode = CleaningMode.Thorough;
    [SerializeField] private int fallbackTaskCount = 3;
    [SerializeField] private float baseDirtWork = 1f;
    [SerializeField] private float baseRustWork = 1.5f;
    [SerializeField] private float baseScrewWork = 1f;
    [SerializeField] private float quickWorkMultiplier = 1.6f;
    [SerializeField] private float thoroughWorkMultiplier = 1f;
    [SerializeField] private float quickQualityPenalty = 0.18f;
    [SerializeField] private float perfectCompletionThreshold = 0.98f;
    [SerializeField] private float goodCompletionThreshold = 0.82f;
    [SerializeField] private float passableCompletionThreshold = 0.55f;

    private readonly List<CleaningTask> _tasks = new List<CleaningTask>();
    private int _difficultyLevel = 1;
    private int _mistakes;
    private bool _isActive;
    private bool _hasReportedCompletion;
    private float _startedAt;

    public string MinigameName => "Lap rap & Ve sinh";
    public bool IsActive => _isActive;
    public CleaningMode CurrentMode => cleaningMode;
    public int Mistakes => _mistakes;
    public float ElapsedTime => _isActive ? Time.time - _startedAt : 0f;
    public IReadOnlyList<CleaningTask> Tasks => _tasks;
    public float CompletionRatio => CalculateCompletionRatio();

    public event Action<RepairQuality> OnMinigameCompleted;

    public void Initialize(List<string> faults, int difficultyLevel)
    {
        _tasks.Clear();
        _difficultyLevel = Mathf.Max(1, difficultyLevel);
        _mistakes = 0;
        _isActive = false;
        _hasReportedCompletion = false;

        if (faults == null || faults.Count == 0)
        {
            CreateFallbackTasks();
            return;
        }

        for (int i = 0; i < faults.Count; i++)
        {
            _tasks.Add(CreateTaskFromFault(faults[i], i));
        }
    }

    public void StartMinigame()
    {
        if (_tasks.Count == 0)
        {
            CreateFallbackTasks();
        }

        _startedAt = Time.time;
        _isActive = true;
        _hasReportedCompletion = false;
    }

    public RepairQuality EndMinigame()
    {
        RepairQuality quality = CalculateRepairQuality();

        if (!_hasReportedCompletion)
        {
            _hasReportedCompletion = true;
            OnMinigameCompleted?.Invoke(quality);
        }

        _isActive = false;
        return quality;
    }

    public void AbortMinigame()
    {
        _isActive = false;
        _hasReportedCompletion = true;
    }

    public void SetCleaningMode(CleaningMode mode)
    {
        cleaningMode = mode;
    }

    public bool CleanTask(int taskIndex, float inputAmount)
    {
        if (!CanInteractWithTask(taskIndex))
        {
            return false;
        }

        CleaningTask task = _tasks[taskIndex];
        if (task.TaskType == CleaningTaskType.LooseScrew)
        {
            RegisterMistake();
            return false;
        }

        task.AddWork(inputAmount * GetWorkMultiplier());
        CompleteIfAllTasksDone();
        return true;
    }

    public bool TightenScrew(int taskIndex, ScrewTurnDirection direction, float inputAmount = 1f)
    {
        if (!CanInteractWithTask(taskIndex))
        {
            return false;
        }

        CleaningTask task = _tasks[taskIndex];
        if (task.TaskType != CleaningTaskType.LooseScrew)
        {
            RegisterMistake();
            return false;
        }

        bool success = task.TryTurnScrew(direction, inputAmount * GetWorkMultiplier());
        if (!success)
        {
            RegisterMistake();
            return false;
        }

        CompleteIfAllTasksDone();
        return true;
    }

    public void RegisterMistake()
    {
        if (_isActive)
        {
            _mistakes++;
        }
    }

    public RepairQuality PreviewRepairQuality()
    {
        return CalculateRepairQuality();
    }

    private bool CanInteractWithTask(int taskIndex)
    {
        return _isActive && taskIndex >= 0 && taskIndex < _tasks.Count && !_tasks[taskIndex].Completed;
    }

    private void CompleteIfAllTasksDone()
    {
        if (!_isActive) return;

        for (int i = 0; i < _tasks.Count; i++)
        {
            if (!_tasks[i].Completed)
            {
                return;
            }
        }

        EndMinigame();
    }

    private CleaningTask CreateTaskFromFault(string fault, int index)
    {
        string normalizedFault = string.IsNullOrWhiteSpace(fault) ? $"cleaning_fault_{index}" : fault;
        CleaningTaskType taskType = GuessTaskType(normalizedFault, index);
        float requiredWork = GetBaseWork(taskType) + (_difficultyLevel - 1) * 0.35f;
        Vector2 position = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
        ScrewTurnDirection direction = UnityEngine.Random.value > 0.5f
            ? ScrewTurnDirection.Clockwise
            : ScrewTurnDirection.CounterClockwise;

        return new CleaningTask(normalizedFault, taskType, position, requiredWork, direction);
    }

    private CleaningTaskType GuessTaskType(string fault, int index)
    {
        string lowerFault = fault.ToLowerInvariant();

        if (lowerFault.Contains("screw") || lowerFault.Contains("loose") || lowerFault.Contains("oc"))
        {
            return CleaningTaskType.LooseScrew;
        }

        if (lowerFault.Contains("rust") || lowerFault.Contains("ri"))
        {
            return CleaningTaskType.Rust;
        }

        if (lowerFault.Contains("dirt") || lowerFault.Contains("dust") || lowerFault.Contains("dirty") || lowerFault.Contains("ban"))
        {
            return CleaningTaskType.Dirt;
        }

        CleaningTaskType[] cycle =
        {
            CleaningTaskType.Dirt,
            CleaningTaskType.Rust,
            CleaningTaskType.LooseScrew
        };

        return cycle[index % cycle.Length];
    }

    private void CreateFallbackTasks()
    {
        int taskCount = Mathf.Max(1, fallbackTaskCount + _difficultyLevel - 1);

        for (int i = 0; i < taskCount; i++)
        {
            _tasks.Add(CreateTaskFromFault($"cleaning_fault_{i + 1}", i));
        }
    }

    private float GetBaseWork(CleaningTaskType taskType)
    {
        return taskType switch
        {
            CleaningTaskType.Rust => baseRustWork,
            CleaningTaskType.LooseScrew => baseScrewWork,
            _ => baseDirtWork
        };
    }

    private float GetWorkMultiplier()
    {
        return cleaningMode == CleaningMode.Quick ? quickWorkMultiplier : thoroughWorkMultiplier;
    }

    private float CalculateCompletionRatio()
    {
        if (_tasks.Count == 0)
        {
            return 0f;
        }

        float totalProgress = 0f;
        for (int i = 0; i < _tasks.Count; i++)
        {
            totalProgress += _tasks[i].Progress;
        }

        return Mathf.Clamp01(totalProgress / _tasks.Count);
    }

    private RepairQuality CalculateRepairQuality()
    {
        float score = CompletionRatio;

        if (cleaningMode == CleaningMode.Quick)
        {
            score -= quickQualityPenalty;
        }

        score = Mathf.Clamp01(score);
        RepairQuality quality = GetQualityForScore(score);

        for (int i = 0; i < _mistakes; i++)
        {
            quality = DowngradeQuality(quality);
        }

        return quality;
    }

    private RepairQuality GetQualityForScore(float score)
    {
        if (score >= perfectCompletionThreshold)
        {
            return RepairQuality.Perfect;
        }

        if (score >= goodCompletionThreshold)
        {
            return RepairQuality.Good;
        }

        if (score >= passableCompletionThreshold)
        {
            return RepairQuality.Passable;
        }

        return RepairQuality.Broken;
    }

    private RepairQuality DowngradeQuality(RepairQuality quality)
    {
        return quality switch
        {
            RepairQuality.Perfect => RepairQuality.Good,
            RepairQuality.Good => RepairQuality.Passable,
            RepairQuality.Passable => RepairQuality.Broken,
            _ => RepairQuality.Broken
        };
    }
}
