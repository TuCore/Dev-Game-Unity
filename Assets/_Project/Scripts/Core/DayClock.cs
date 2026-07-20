using UnityEngine;

/// <summary>
/// Quản lý chu kỳ ngày trong game (08:00 mở cửa, 20:00 đóng cửa).
/// Tự động sinh ra nếu chưa có trên Scene.
/// </summary>
public class DayClock : MonoBehaviour
{
    private static DayClock _instance;
    private static bool _isShuttingDown;

    /// <summary>
    /// Returns the current clock without creating one. Use this from OnDestroy/OnDisable.
    /// </summary>
    public static DayClock ExistingInstance => _instance;

    public static DayClock Instance
    {
        get
        {
            if (_isShuttingDown)
            {
                return null;
            }

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DayClock>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DayClock");
                    _instance = go.AddComponent<DayClock>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isShuttingDown = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        // Gọi Instance để kích hoạt tự động sinh GameObject khi vừa vào game
        var init = Instance;
    }

    [Header("Cấu hình thời gian")]
    [SerializeField] private float dayDurationInSeconds = 300f; // 5 phút ngoài đời = 1 ngày game
    [SerializeField] private float startHour = 8f;              // Mở cửa lúc 08:00
    [SerializeField] private float endHour = 20f;               // Đóng cửa lúc 20:00

    private float _currentTime;
    private int _currentDay = 1;
    private bool _isRunning;

    public float CurrentHour => Mathf.Lerp(startHour, endHour, Mathf.Clamp01(_currentTime / DayDurationInSeconds));
    public int CurrentDay => _currentDay;
    public bool IsRunning => _isRunning;
    public float DayDurationInSeconds => Mathf.Max(0.01f, dayDurationInSeconds);
    public float StartHour => startHour;
    public float EndHour => endHour;
    public float GameMinutesPerRealSecond => Mathf.Max(0f, endHour - startHour) * 60f / DayDurationInSeconds;

    // Events
    public System.Action<float> OnTimeChanged;   
    public System.Action OnDayStarted;
    public System.Action OnDayEnded;
    public System.Action<int> OnNewDay;           

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        
        // Tự động gắn UI
        if (GetComponent<ClockUI>() == null)
        {
            gameObject.AddComponent<ClockUI>();
        }
        if (GetComponent<DaySummaryUI>() == null)
        {
            gameObject.AddComponent<DaySummaryUI>();
        }
    }

    private void Start()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        MinigameManager.OnMinigameStartedGlobal += HandleMinigameStarted;
        MinigameManager.OnMinigameCompletedGlobal += HandleMinigameCompleted;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VietnamStreet")
        {
            ResumeTime();
        }
    }

    private void HandleMinigameStarted(IMinigame minigame)
    {
        PauseTime();
    }

    private void HandleMinigameCompleted(RepairQuality quality)
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VietnamStreet")
        {
            ResumeTime();
        }
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        MinigameManager.OnMinigameStartedGlobal -= HandleMinigameStarted;
        MinigameManager.OnMinigameCompletedGlobal -= HandleMinigameCompleted;

        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "VietnamStreet")
        {
            BedInteraction.hasVisitedStreetFirstTime = true;
            ResumeTime();
        }
        else
        {
            PauseTime();
        }
    }

    private void Update()
    {
        if (!_isRunning) return;

        _currentTime += Time.deltaTime;

        OnTimeChanged?.Invoke(CurrentHour);

        if (_currentTime >= dayDurationInSeconds)
        {
            EndDay();
        }
    }

    public void StartDay()
    {
        _currentTime = 0f;
        
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VietnamStreet")
        {
            _isRunning = true;
        }
        else
        {
            _isRunning = false;
        }

        OnDayStarted?.Invoke();
    }

    public void EndDay()
    {
        _isRunning = false;
        OnDayEnded?.Invoke();
    }

    public void StartNextDay()
    {
        _currentDay++;
        OnNewDay?.Invoke(_currentDay);
        StartDay();
    }

    public void PauseTime() => _isRunning = false;
    public void ResumeTime() => _isRunning = true;
}
