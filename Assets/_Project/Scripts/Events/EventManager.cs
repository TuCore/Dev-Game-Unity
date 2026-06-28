using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Trigger sự kiện ngẫu nhiên/theo mùa (cúp điện, mưa ngập, Tết).
/// Mỗi sự kiện là 1 GameEventData (ScriptableObject) khai báo điều kiện trigger + hiệu ứng.
/// Tránh hardcode if-else event trong code (GDD mục 13).
/// </summary>
public class EventManager : MonoBehaviour
{
    [Header("Cấu hình")]
    [SerializeField] private float eventCheckInterval = 60f;  // Kiểm tra sự kiện mỗi 60s
    [SerializeField] private float baseEventChance = 0.1f;    // 10% xảy ra sự kiện mỗi lần check

    [Header("Pool sự kiện")]
    [SerializeField] private List<string> randomEventPool;    // Danh sách sự kiện ngẫu nhiên
    [SerializeField] private List<string> seasonalEventPool;  // Danh sách sự kiện theo mùa

    private float _eventTimer;
    private string _activeEvent;

    public bool HasActiveEvent => !string.IsNullOrEmpty(_activeEvent);
    public string ActiveEvent => _activeEvent;

    // Events
    public System.Action<string> OnEventStarted;    // Truyền tên sự kiện
    public System.Action<string> OnEventEnded;      // Truyền tên sự kiện kết thúc

    private void Update()
    {
        _eventTimer += Time.deltaTime;
        if (_eventTimer >= eventCheckInterval)
        {
            _eventTimer = 0f;
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        if (HasActiveEvent) return; // Chỉ 1 sự kiện tại 1 thời điểm

        if (Random.value <= baseEventChance && randomEventPool.Count > 0)
        {
            int index = Random.Range(0, randomEventPool.Count);
            StartEvent(randomEventPool[index]);
        }
    }

    /// <summary>
    /// Kích hoạt sự kiện cụ thể.
    /// </summary>
    public void StartEvent(string eventName)
    {
        _activeEvent = eventName;
        OnEventStarted?.Invoke(eventName);
        Debug.Log($"[EventManager] Sự kiện: {eventName}");
    }

    /// <summary>
    /// Kết thúc sự kiện hiện tại.
    /// </summary>
    public void EndActiveEvent()
    {
        if (!HasActiveEvent) return;

        string ended = _activeEvent;
        _activeEvent = null;
        OnEventEnded?.Invoke(ended);
    }

    /// <summary>
    /// Kích hoạt sự kiện theo mùa (gọi từ DayClock khi đến mốc thời gian).
    /// </summary>
    public void TriggerSeasonalEvent(string eventName)
    {
        if (seasonalEventPool.Contains(eventName))
        {
            StartEvent(eventName);
        }
    }
}
