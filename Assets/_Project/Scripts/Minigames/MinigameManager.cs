using System.Collections.Generic;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FaultRandomizer faultRandomizer;

    private IMinigame _activeMinigame;
    private static MinigameManager _instance;
    private static int s_playSessionVersion;
    private int _seenPlaySessionVersion = -1;

    public static MinigameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MinigameManager>();
            }

            return _instance;
        }
        private set => _instance = value;
    }

    public bool IsMinigameActive
    {
        get
        {
            EnsureFreshPlaySession();
            return IsActiveMinigameLive();
        }
    }

    public System.Action<IMinigame> OnMinigameStarted;
    public System.Action<RepairQuality> OnMinigameCompleted;

    public static event System.Action<IMinigame> OnMinigameStartedGlobal;
    public static event System.Action<RepairQuality> OnMinigameCompletedGlobal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void MarkNewPlaySession()
    {
        s_playSessionVersion++;
        _instance = null;
    }

    private void Awake()
    {
        if (_instance == null || _instance == this)
        {
            Instance = this;
            EnsureFreshPlaySession();
            return;
        }

        Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (_instance == null)
        {
            Instance = this;
        }

        EnsureFreshPlaySession();
    }

    private void EnsureFreshPlaySession()
    {
        if (!Application.isPlaying || _seenPlaySessionVersion == s_playSessionVersion)
        {
            return;
        }

        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        _seenPlaySessionVersion = s_playSessionVersion;
        CleanupMinigame();
    }

    public void StartMinigame(IMinigame minigame, List<string> faultPool, int difficultyLevel)
    {
        EnsureFreshPlaySession();

        if (IsActiveMinigameLive())
        {
            Debug.LogWarning("[MinigameManager] Minigame is already running.");
            return;
        }

        if (minigame == null)
        {
            Debug.LogWarning("[MinigameManager] Tried to start a null minigame.");
            return;
        }

        List<string> selectedFaults = faultRandomizer != null
            ? faultRandomizer.RandomizeFaults(faultPool, difficultyLevel)
            : new List<string>(faultPool ?? new List<string>());

        _activeMinigame = minigame;
        _activeMinigame.OnMinigameCompleted += HandleMinigameCompleted;
        _activeMinigame.Initialize(selectedFaults, difficultyLevel);
        _activeMinigame.StartMinigame();

        OnMinigameStarted?.Invoke(minigame);
        OnMinigameStartedGlobal?.Invoke(minigame);
    }

    public void AbortCurrentMinigame()
    {
        if (!IsActiveMinigameLive())
        {
            return;
        }

        _activeMinigame.AbortMinigame();
        OnMinigameCompleted?.Invoke(RepairQuality.Broken);
        OnMinigameCompletedGlobal?.Invoke(RepairQuality.Broken);
        CleanupMinigame();
    }

    private void HandleMinigameCompleted(RepairQuality quality)
    {
        OnMinigameCompleted?.Invoke(quality);
        OnMinigameCompletedGlobal?.Invoke(quality);
        CleanupMinigame();
    }

    private void CleanupMinigame()
    {
        if (_activeMinigame == null)
        {
            return;
        }

        if (!(_activeMinigame is UnityEngine.Object activeObject) || activeObject != null)
        {
            _activeMinigame.OnMinigameCompleted -= HandleMinigameCompleted;
        }

        _activeMinigame = null;
    }

    private bool IsActiveMinigameLive()
    {
        if (_activeMinigame == null)
        {
            return false;
        }

        if (_activeMinigame is UnityEngine.Object activeObject && activeObject == null)
        {
            _activeMinigame = null;
            return false;
        }

        return _activeMinigame.IsActive;
    }
}
