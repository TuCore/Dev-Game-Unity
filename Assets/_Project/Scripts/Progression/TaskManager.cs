using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    public const string OpenShopTaskId = "open_shop";
    public const string ReceiveOrderTaskId = "receive_first_order";
    public const string RepairItemTaskId = "repair_first_item";
    public const string ReturnOrderTaskId = "return_first_order";

    [Header("Default Tasks")]
    [SerializeField] private bool createDefaultTasks = true;

    private readonly List<GameTask> tasks = new List<GameTask>();
    private readonly Dictionary<string, GameTask> taskLookup = new Dictionary<string, GameTask>();

    public event Action OnTasksChanged;
    public event Action<GameTask> OnTaskCompleted;

    public IReadOnlyList<GameTask> Tasks => tasks;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        EnsureInstance();
    }

    public static TaskManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindFirstObjectByType<TaskManager>();
        if (Instance != null)
        {
            return Instance;
        }

        GameObject taskManagerObject = new GameObject("TaskManager");
        Instance = taskManagerObject.AddComponent<TaskManager>();
        DontDestroyOnLoad(taskManagerObject);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (createDefaultTasks)
        {
            RegisterDefaultTasks();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "VietnamStreet")
        {
            CompleteTask(OpenShopTaskId);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "VietnamStreet")
        {
            CompleteTask(OpenShopTaskId);
        }
    }

    private void RegisterDefaultTasks()
    {
        RegisterTask(OpenShopTaskId, "M\u1edf ti\u1ec7m s\u1eeda ch\u1eefa", "B\u1eaft \u0111\u1ea7u ng\u00e0y l\u00e0m vi\u1ec7c trong ti\u1ec7m.", 1);
        RegisterTask(ReceiveOrderTaskId, "Nh\u1eadn \u0111\u01a1n \u0111\u1ea7u ti\u00ean", "N\u00f3i chuy\u1ec7n v\u1edbi kh\u00e1ch v\u00e0 nh\u1eadn m\u1ed9t m\u00f3n \u0111\u1ed3 c\u1ea7n s\u1eeda.", 1);
        RegisterTask(RepairItemTaskId, "S\u1eeda xong m\u00f3n \u0111\u1ea7u ti\u00ean", "Ho\u00e0n th\u00e0nh m\u1ed9t minigame s\u1eeda ch\u1eefa v\u1edbi k\u1ebft qu\u1ea3 kh\u00f4ng h\u1ecfng.", 1);
        RegisterTask(ReturnOrderTaskId, "Tr\u1ea3 \u0111\u1ed3 cho kh\u00e1ch", "Giao l\u1ea1i m\u00f3n \u0111\u1ed3 \u0111\u00e3 s\u1eeda v\u00e0 nh\u1eadn ti\u1ec1n c\u00f4ng.", 1);
    }

    public GameTask RegisterTask(string id, string title, string description, int targetProgress = 1)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("[TaskManager] Cannot register task with empty id.");
            return null;
        }

        if (taskLookup.TryGetValue(id, out GameTask existingTask))
        {
            return existingTask;
        }

        GameTask task = new GameTask(id, title, description, targetProgress);
        tasks.Add(task);
        taskLookup.Add(id, task);
        OnTasksChanged?.Invoke();
        return task;
    }

    public bool AddProgress(string id, int amount = 1)
    {
        if (!taskLookup.TryGetValue(id, out GameTask task))
        {
            return false;
        }

        bool wasCompleted = task.Completed;
        bool changed = task.AddProgress(amount);
        if (!changed)
        {
            return false;
        }

        NotifyTaskChanged(task, wasCompleted);
        return true;
    }

    public bool CompleteTask(string id)
    {
        if (!taskLookup.TryGetValue(id, out GameTask task))
        {
            return false;
        }

        bool wasCompleted = task.Completed;
        bool changed = task.Complete();
        if (!changed)
        {
            return false;
        }

        NotifyTaskChanged(task, wasCompleted);
        return true;
    }

    public void NotifyOrderAccepted(CustomerOrder order)
    {
        AddProgress(ReceiveOrderTaskId);
    }

    public void NotifyRepairCompleted(RepairQuality quality)
    {
        if (quality != RepairQuality.Broken)
        {
            AddProgress(RepairItemTaskId);
        }
    }

    public void NotifyOrderReturned(CustomerOrder order)
    {
        AddProgress(ReturnOrderTaskId);
    }

    public string GetPhoneTaskText()
    {
        if (tasks.Count == 0)
        {
            return "Ch\u01b0a c\u00f3 nhi\u1ec7m v\u1ee5 n\u00e0o.";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < tasks.Count; i++)
        {
            GameTask task = tasks[i];
            string check = task.Completed ? "[x]" : "[ ]";
            string progress = task.TargetProgress > 1 ? $" ({task.CurrentProgress}/{task.TargetProgress})" : "";
            builder.AppendLine($"{check} {task.Title}{progress}");
            if (!task.Completed && !string.IsNullOrWhiteSpace(task.Description))
            {
                builder.AppendLine($"    {task.Description}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    public List<GameTask> GetVisibleTasks(int maxCount)
    {
        List<GameTask> visibleTasks = new List<GameTask>();

        for (int i = 0; i < tasks.Count && visibleTasks.Count < maxCount; i++)
        {
            if (!tasks[i].Completed)
            {
                visibleTasks.Add(tasks[i]);
            }
        }

        if (visibleTasks.Count == 0)
        {
            for (int i = 0; i < tasks.Count && visibleTasks.Count < maxCount; i++)
            {
                visibleTasks.Add(tasks[i]);
            }
        }

        return visibleTasks;
    }

    private void NotifyTaskChanged(GameTask task, bool wasCompleted)
    {
        OnTasksChanged?.Invoke();

        if (!wasCompleted && task.Completed)
        {
            OnTaskCompleted?.Invoke(task);
            if (ToastNotificationManager.Instance != null)
            {
                ToastNotificationManager.Instance.ShowToast($"Ho\u00e0n th\u00e0nh nhi\u1ec7m v\u1ee5: {task.Title}", 2.5f);
            }
        }
    }
}
