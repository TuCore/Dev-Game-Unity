using UnityEngine;

/// <summary>
/// Quản lý chu kỳ ngày trong game (08:00 mở cửa, 20:00 đóng cửa).
/// Tự động sinh ra nếu chưa có trên Scene.
/// </summary>
public class DayClock : MonoBehaviour
{
    private static DayClock _instance;
    public static DayClock Instance
    {
        get
        {
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

    public float CurrentHour => Mathf.Lerp(startHour, endHour, _currentTime / dayDurationInSeconds);
    public int CurrentDay => _currentDay;
    public bool IsRunning => _isRunning;

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
        // Bắt đầu ngày ngay khi vào game
        StartDay();
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
        _isRunning = true;
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
