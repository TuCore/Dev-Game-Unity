using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý các nhu cầu sống cơ bản của người chơi: mệt mỏi, đói, khát.
/// Các giá trị dùng thang 0-100 và giảm dần theo thời gian chơi.
/// </summary>
public class PlayerNeeds : MonoBehaviour
{
    public static PlayerNeeds Instance { get; private set; }

    [Header("Giá trị tối đa")]
    [SerializeField] private float maxNeedValue = 100f;
    [SerializeField] private float startingFatigue = 100f;
    [SerializeField] private float startingHunger = 100f;
    [SerializeField] private float startingThirst = 100f;

    [Header("Giảm theo thời gian (điểm / phút thật)")]
    [SerializeField] private float fatigueDrainPerMinute = 4.5f;
    [SerializeField] private float hungerDrainPerMinute = 2.4f;
    [SerializeField] private float thirstDrainPerMinute = 3.6f;
    [SerializeField] private float walkingFatigueMultiplier = 1.25f;
    [SerializeField] private float walkingThirstMultiplier = 1.1f;
    [SerializeField] private float runningFatigueMultiplier = 2.1f;
    [SerializeField] private float runningHungerMultiplier = 1.12f;
    [SerializeField] private float runningThirstMultiplier = 1.55f;
    [SerializeField] private bool drainOnlyInGameplayScenes = true;
    [SerializeField] private bool pauseDrainDuringMinigame = true;

    [Header("Buff tạm thời")]
    [SerializeField] [Range(0.2f, 1f)] private float temporaryFatigueDrainMinMultiplier = 0.35f;

    [Header("Qua ngày mới")]
    [SerializeField] private float overnightFatigueRecovery = 100f;
    [SerializeField] private float overnightHungerLoss = 8f;
    [SerializeField] private float overnightThirstLoss = 12f;

    [Header("Cảnh báo")]
    [SerializeField] private float warningThreshold = 25f;
    [SerializeField] private float warningCooldown = 35f;

    private float _currentFatigue;
    private float _currentHunger;
    private float _currentThirst;
    private float _nextWarningTime;
    private float _temporaryFatigueDrainMultiplier = 1f;
    private float _temporaryFatigueDrainUntil;

    public float MaxNeedValue => maxNeedValue;
    public float CurrentFatigue => _currentFatigue;
    public float CurrentHunger => _currentHunger;
    public float CurrentThirst => _currentThirst;
    public float FatiguePercent => GetPercent(_currentFatigue);
    public float HungerPercent => GetPercent(_currentHunger);
    public float ThirstPercent => GetPercent(_currentThirst);

    public event Action<PlayerNeeds> OnNeedsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        EnsureInstance();
    }

    public static PlayerNeeds EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindFirstObjectByType<PlayerNeeds>();
        if (Instance != null)
        {
            return Instance;
        }

        GameObject needsObject = new GameObject("PlayerNeeds");
        Instance = needsObject.AddComponent<PlayerNeeds>();
        DontDestroyOnLoad(needsObject);
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
        ResetNeeds();
    }

    private void Update()
    {
        if (!ShouldDrain())
        {
            return;
        }

        float minuteDelta = Time.deltaTime / 60f;
        GetActivityMultipliers(out float fatigueMultiplier, out float hungerMultiplier, out float thirstMultiplier);

        DrainNeeds(
            fatigueDrainPerMinute * fatigueMultiplier * GetTemporaryFatigueDrainMultiplier() * minuteDelta,
            hungerDrainPerMinute * hungerMultiplier * minuteDelta,
            thirstDrainPerMinute * thirstMultiplier * minuteDelta);

        TryShowWarning();
    }

    public void DrainNeeds(float fatigueAmount, float hungerAmount, float thirstAmount)
    {
        SetNeeds(
            _currentFatigue - Mathf.Max(0f, fatigueAmount),
            _currentHunger - Mathf.Max(0f, hungerAmount),
            _currentThirst - Mathf.Max(0f, thirstAmount));
    }

    public void RecoverNeeds(float fatigueAmount, float hungerAmount, float thirstAmount)
    {
        SetNeeds(
            _currentFatigue + Mathf.Max(0f, fatigueAmount),
            _currentHunger + Mathf.Max(0f, hungerAmount),
            _currentThirst + Mathf.Max(0f, thirstAmount));
    }

    public bool WouldRecoverAny(float fatigueAmount, float hungerAmount, float thirstAmount)
    {
        return (fatigueAmount > 0f && _currentFatigue < maxNeedValue)
            || (hungerAmount > 0f && _currentHunger < maxNeedValue)
            || (thirstAmount > 0f && _currentThirst < maxNeedValue);
    }

    public void RestOvernight()
    {
        ClearTemporaryFatigueDrainMultiplier();
        SetNeeds(
            _currentFatigue + overnightFatigueRecovery,
            _currentHunger - Mathf.Max(0f, overnightHungerLoss),
            _currentThirst - Mathf.Max(0f, overnightThirstLoss));
    }

    public void ResetNeeds()
    {
        ClearTemporaryFatigueDrainMultiplier();
        SetNeeds(startingFatigue, startingHunger, startingThirst);
    }

    public void ApplyTemporaryFatigueDrainMultiplier(float multiplier, float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return;
        }

        float clampedMultiplier = Mathf.Clamp(multiplier, temporaryFatigueDrainMinMultiplier, 1f);
        float newUntil = Time.time + durationSeconds;
        if (newUntil >= _temporaryFatigueDrainUntil || clampedMultiplier <= _temporaryFatigueDrainMultiplier)
        {
            _temporaryFatigueDrainMultiplier = clampedMultiplier;
            _temporaryFatigueDrainUntil = newUntil;
        }
    }

    public float GetTemporaryFatigueDrainRemaining()
    {
        return Mathf.Max(0f, _temporaryFatigueDrainUntil - Time.time);
    }

    private float GetTemporaryFatigueDrainMultiplier()
    {
        if (Time.time >= _temporaryFatigueDrainUntil)
        {
            ClearTemporaryFatigueDrainMultiplier();
        }

        return Mathf.Clamp(_temporaryFatigueDrainMultiplier, temporaryFatigueDrainMinMultiplier, 1f);
    }

    private void ClearTemporaryFatigueDrainMultiplier()
    {
        _temporaryFatigueDrainMultiplier = 1f;
        _temporaryFatigueDrainUntil = 0f;
    }

    private void SetNeeds(float fatigue, float hunger, float thirst)
    {
        float clampedFatigue = Mathf.Clamp(fatigue, 0f, maxNeedValue);
        float clampedHunger = Mathf.Clamp(hunger, 0f, maxNeedValue);
        float clampedThirst = Mathf.Clamp(thirst, 0f, maxNeedValue);

        bool changed = !Mathf.Approximately(_currentFatigue, clampedFatigue)
            || !Mathf.Approximately(_currentHunger, clampedHunger)
            || !Mathf.Approximately(_currentThirst, clampedThirst);

        _currentFatigue = clampedFatigue;
        _currentHunger = clampedHunger;
        _currentThirst = clampedThirst;

        if (changed)
        {
            OnNeedsChanged?.Invoke(this);
        }
    }

    private bool ShouldDrain()
    {
        if (Time.timeScale <= 0f)
        {
            return false;
        }

        if (drainOnlyInGameplayScenes && !IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return false;
        }

        if (pauseDrainDuringMinigame && IsMinigameActive())
        {
            return false;
        }

        return true;
    }

    private void GetActivityMultipliers(out float fatigueMultiplier, out float hungerMultiplier, out float thirstMultiplier)
    {
        fatigueMultiplier = 1f;
        hungerMultiplier = 1f;
        thirstMultiplier = 1f;

        bool isMoving = Mathf.Abs(CustomInputManager.GetAxisHorizontal()) > 0.01f || Mathf.Abs(CustomInputManager.GetAxisVertical()) > 0.01f;
        if (!isMoving)
        {
            return;
        }

        bool isRunning = CustomInputManager.GetKey("Run");
        if (isRunning)
        {
            fatigueMultiplier = Mathf.Max(1f, runningFatigueMultiplier);
            hungerMultiplier = Mathf.Max(1f, runningHungerMultiplier);
            thirstMultiplier = Mathf.Max(1f, runningThirstMultiplier);
            return;
        }

        fatigueMultiplier = Mathf.Max(1f, walkingFatigueMultiplier);
        thirstMultiplier = Mathf.Max(1f, walkingThirstMultiplier);
    }

    private bool IsGameplayScene(string sceneName)
    {
        return sceneName == "Shop_Main" || sceneName == "VietnamStreet";
    }

    private bool IsMinigameActive()
    {
        if (MinigameManager.Instance != null && MinigameManager.Instance.IsMinigameActive)
        {
            return true;
        }

        MinigameManager manager = FindFirstObjectByType<MinigameManager>();
        return manager != null && manager.IsMinigameActive;
    }

    private float GetPercent(float value)
    {
        return maxNeedValue <= 0f ? 0f : Mathf.Clamp01(value / maxNeedValue);
    }

    private void TryShowWarning()
    {
        if (Time.time < _nextWarningTime)
        {
            return;
        }

        string message = "";
        if (_currentFatigue <= warningThreshold)
        {
            message = "Bạn đang mệt, nên nghỉ hoặc ăn uống một chút.";
        }
        else if (_currentThirst <= warningThreshold)
        {
            message = "Bạn đang khát, kiếm trà uống cho tỉnh lại.";
        }
        else if (_currentHunger <= warningThreshold)
        {
            message = "Bạn đang đói, mua bánh mì ăn lót dạ đi.";
        }

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        _nextWarningTime = Time.time + warningCooldown;
        if (ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast(message, 3f);
        }
    }
}
