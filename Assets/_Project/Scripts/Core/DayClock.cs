using UnityEngine;

/// <summary>
/// Quản lý chu kỳ ngày trong game (7AM mở cửa, chiều tối quản lý, đêm nghỉ ngơi).
/// Điều khiển thời gian thực trong ngày, deadline đơn khách.
/// </summary>
public class DayClock : MonoBehaviour
{
    [Header("Cấu hình thời gian")]
    [SerializeField] private float dayDurationInSeconds = 600f; // Tổng thời gian 1 ngày game
    [SerializeField] private float startHour = 7f;              // Giờ bắt đầu ngày

    private float _currentTime;
    private int _currentDay = 1;
    private bool _isRunning;

    public float CurrentHour => startHour + (_currentTime / dayDurationInSeconds) * 24f;
    public int CurrentDay => _currentDay;
    public bool IsRunning => _isRunning;

    // Events
    public System.Action<float> OnTimeChanged;   // Truyền giờ hiện tại
    public System.Action OnDayStarted;
    public System.Action OnDayEnded;
    public System.Action<int> OnNewDay;           // Truyền số ngày mới

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
        _currentDay++;
        OnDayEnded?.Invoke();
        OnNewDay?.Invoke(_currentDay);
    }

    public void PauseTime() => _isRunning = false;
    public void ResumeTime() => _isRunning = true;
}
