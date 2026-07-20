using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class TobaccoPipeStation : MonoBehaviour, IInteractable
{
    private const string AreaName = "TobaccoPipe_InteractionArea";
    private const string VisualName = "TobaccoPipe_Visual";
    private const float MeterWidth = 650f;

    private enum PipeResult { Perfect, Good, Failed, Cancelled }

    [Header("Điểm nghỉ")]
    [SerializeField] private string stationName = "Điếu cày";
    [SerializeField] private float price = 5000f;
    [SerializeField] private bool useCooldown = false;
    [SerializeField] private float cooldownSeconds = 180f;
    [SerializeField] private bool spendCashOnStart = true;
    [SerializeField] private bool disablePlayerControlDuringMinigame = true;

    [Header("Nhịp lấy hơi")]
    [SerializeField] private float chargeSpeed = 0.43f;
    [SerializeField] private float minChargeToRelease = 0.16f;
    [SerializeField] private float targetCenterMin = 0.55f;
    [SerializeField] private float targetCenterMax = 0.82f;
    [SerializeField] private float goodZoneWidth = 0.22f;
    [SerializeField] private float perfectZoneWidth = 0.075f;
    [SerializeField] private float maxRoundSeconds = 9f;

    [Header("Kết quả")]
    [SerializeField] private float perfectFatigueRecovery = 30f;
    [SerializeField] private float goodFatigueRecovery = 17f;
    [SerializeField] private float failFatiguePenalty = 6f;
    [SerializeField] private float perfectThirstPenalty = 8f;
    [SerializeField] private float goodThirstPenalty = 13f;
    [SerializeField] private float failThirstPenalty = 24f;
    [SerializeField] private float perfectFocusDuration = 180f;
    [SerializeField] [Range(0.35f, 1f)] private float perfectFatigueDrainMultiplier = 0.72f;
    [SerializeField] private float goodFocusDuration = 75f;
    [SerializeField] [Range(0.35f, 1f)] private float goodFatigueDrainMultiplier = 0.88f;
    [SerializeField] private float coughLockDuration = 1.45f;
    [SerializeField] private int easyUseCount = 2;
    [SerializeField] private float overuseZoneShrink = 0.025f;

    [Header("Vùng tương tác")]
    [SerializeField] private Vector3 interactionAreaSize = new Vector3(3.2f, 1.7f, 2.6f);
    [SerializeField] private Vector3 interactionAreaCenter = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private float aimAssistRadius = 1.8f;
    [SerializeField] private bool enableDirectInteractFallback = true;
    [SerializeField] private float directInteractRange = 6.5f;
    [SerializeField] [Range(0.08f, 0.5f)] private float directInteractViewportRadius = 0.46f;
    [SerializeField] private bool showTransparentInteractionArea = true;
    [SerializeField] private bool showInteractionAreaWhilePlaying = false;
    [SerializeField] [Range(0f, 0.35f)] private float interactionAreaAlpha = 0.11f;
    [SerializeField] private Color interactionAreaColor = new Color(0.8f, 0.92f, 0.55f, 1f);

    [Header("Model test tự tạo")]
    [SerializeField] private bool createSimplePipeVisual = false;
    [SerializeField] private bool createWorldLabel = false;

    private float _nextUseTime;
    private int _useCount;
    private bool _isPlaying;
    private bool _isHolding;
    private bool _roundResolved;
    private float _charge;
    private float _roundTime;
    private float _targetCenter;
    private float _goodWidth;
    private float _perfectWidth;
    private GameObject _uiRoot;
    private Image _fillImage;
    private Image _needleImage;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _hintText;
    private TextMeshProUGUI _resultText;
    private PlayerController _lockedPlayerController;
    private bool _playerControllerWasEnabled;
    private static int s_playSessionVersion;
    private int _seenPlaySessionVersion = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void MarkNewPlaySession()
    {
        s_playSessionVersion++;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void PrepareLoadedSceneStations()
    {
        TobaccoPipeStation[] stations = Resources.FindObjectsOfTypeAll<TobaccoPipeStation>();
        for (int i = 0; i < stations.Length; i++)
        {
            TobaccoPipeStation station = stations[i];
            if (station == null || !station.gameObject.scene.IsValid() || !station.gameObject.scene.isLoaded)
            {
                continue;
            }

            station.enabled = true;
            station.ForceInteractionReady();
        }
    }



    public void ConfigureStation(string newStationName, float newPrice, float newCooldownSeconds)
    {
        stationName = string.IsNullOrWhiteSpace(newStationName) ? stationName : newStationName;
        price = Mathf.Max(0f, newPrice);
        cooldownSeconds = Mathf.Max(0f, newCooldownSeconds);
    }

    public void ForceInteractionReady()
    {
        useCooldown = false;
        enableDirectInteractFallback = true;
        EnsureInteractionArea();
    }

    public void ConfigureInteractionArea(Vector3 areaSize, Vector3 areaCenter, Color areaColor, float alpha)
    {
        interactionAreaSize = new Vector3(Mathf.Max(0.2f, areaSize.x), Mathf.Max(0.2f, areaSize.y), Mathf.Max(0.2f, areaSize.z));
        interactionAreaCenter = areaCenter;
        interactionAreaColor = areaColor;
        interactionAreaAlpha = Mathf.Clamp(alpha, 0f, 0.35f);
        showTransparentInteractionArea = true;
        EnsureInteractionArea();
    }

    public float AimAssistRadius => Mathf.Max(0.25f, Mathf.Max(aimAssistRadius, interactionAreaSize.magnitude * 0.5f));

    public Vector3 GetAimAssistPoint()
    {
        Transform area = transform.Find(AreaName);
        if (area != null)
        {
            Collider areaCollider = area.GetComponent<Collider>();
            if (areaCollider != null && areaCollider.enabled)
            {
                return areaCollider.bounds.center;
            }

            return area.position;
        }

        Vector3 point = transform.TransformPoint(interactionAreaCenter);
        point.y = Mathf.Max(point.y, transform.position.y + Mathf.Max(0.55f, interactionAreaCenter.y));
        return point;
    }

    public string GetInteractionPrompt()
    {
        if (_isPlaying)
        {
            return "Đang làm thuốc lào...";
        }

        if (IsCoolingDown(out float remaining))
        {
            return $"{stationName} còn nghỉ {Mathf.CeilToInt(remaining)}s";
        }

        string priceText = price > 0f ? $" ({price:N0} VNĐ)" : "";
        return $"Nhấn [E] làm một bi thuốc lào{priceText}\n<color=#D7F8FF>Hồi năng lượng, nhưng khát hơn. Giữ SPACE rồi thả đúng vùng xanh.</color>";
    }

    public void Interact()
    {
        if (_isPlaying)
        {
            return;
        }

        if (IsCoolingDown(out float remaining))
        {
            ShowToast($"Đợi thêm {Mathf.CeilToInt(remaining)} giây nữa rồi làm tiếp.");
            return;
        }

        if (spendCashOnStart && price > 0f)
        {
            EconomyManager economy = EconomyManager.Instance != null ? EconomyManager.Instance : FindFirstObjectByType<EconomyManager>();
            if (economy == null)
            {
                ShowToast("Không tìm thấy hệ thống tiền.");
                return;
            }

            if (!economy.SpendCash(price))
            {
                ShowToast($"Không đủ tiền. Cần {price:N0} VNĐ.");
                return;
            }
        }

        StartRound();
    }

    private void Reset()
    {
        EnsureInteractionArea();
        EnsureVisual();
    }

    private void Awake()
    {
        // Tự động thu nhỏ vùng chọn của tẩu thuốc vì mô hình tẩu thuốc rất nhỏ
        interactionAreaSize = new Vector3(0.5f, 0.5f, 0.5f);
        interactionAreaCenter = new Vector3(0f, 0.1f, 0f);
        aimAssistRadius = 0.5f;
        directInteractViewportRadius = 0.15f;

        ForceInteractionReady();
        EnsureInteractionArea();
        EnsureVisual();
    }

    private void OnEnable()
    {
        ForceInteractionReady();
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
        StopAllCoroutines();
        CleanupUi();
        UnlockPlayerControl();

        _nextUseTime = 0f;
        _useCount = 0;
        _isPlaying = false;
        _isHolding = false;
        _roundResolved = false;
        _charge = 0f;
        _roundTime = 0f;
    }

    private void OnValidate()
    {
        goodZoneWidth = Mathf.Clamp(goodZoneWidth, 0.08f, 0.65f);
        perfectZoneWidth = Mathf.Clamp(perfectZoneWidth, 0.03f, goodZoneWidth);
        targetCenterMin = Mathf.Clamp01(targetCenterMin);
        targetCenterMax = Mathf.Clamp(targetCenterMax, targetCenterMin, 1f);
        aimAssistRadius = Mathf.Max(0.25f, aimAssistRadius);
        directInteractRange = Mathf.Max(0.5f, directInteractRange);
        enableDirectInteractFallback = true;
    }

    private void Update()
    {
        EnsureFreshPlaySession();

        if (!_isPlaying)
        {
            HandleDirectInteractFallback();
            return;
        }

        if (_roundResolved)
        {
            return;
        }

        _roundTime += Time.unscaledDeltaTime;
        if (_roundTime >= maxRoundSeconds)
        {
            ResolveRound(PipeResult.Failed, "Để lâu quá, hụt hơi rồi.");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResolveRound(PipeResult.Cancelled, "Đã dừng lại.");
            return;
        }

        bool pressDown = CustomInputManager.GetKeyDown("Jump") || Input.GetMouseButtonDown(0);
        bool pressed = CustomInputManager.GetKey("Jump") || Input.GetMouseButton(0);
        bool pressUp = CustomInputManager.GetKeyUp("Jump") || Input.GetMouseButtonUp(0);

        if (pressDown)
        {
            _isHolding = true;
            PlayImportedSfx("tieng_hut_thuoc_lao", TobaccoPipeSfxCue.Inhale, 0.82f, 1f, true);
            TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Bubble, 0.44f, 1f);
        }

        if (_isHolding && pressed)
        {
            _charge += chargeSpeed * Time.unscaledDeltaTime;
            if (_charge >= 1f)
            {
                _charge = 1f;
                RefreshMeter();
                ResolveRound(PipeResult.Failed, "Gắt quá, bị sặc.");
                return;
            }
        }

        if (_isHolding && pressUp)
        {
            EvaluateRelease();
            return;
        }

        RefreshMeter();
        RefreshStatus();
    }

    private void HandleDirectInteractFallback()
    {
        if (!enableDirectInteractFallback || (!Input.GetKeyDown(KeyCode.E) && !CustomInputManager.GetKeyDown("Interact")))
        {
            return;
        }

        if (IsCameraLookingAtStation())
        {
            Interact();
        }
    }

    private bool IsCameraLookingAtStation()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = FindFirstObjectByType<Camera>();
        }

        if (camera == null)
        {
            return false;
        }

        Vector3 aimPoint = GetAimAssistPoint();
        Vector3 fromCamera = aimPoint - camera.transform.position;
        float distance = fromCamera.magnitude;
        float range = Mathf.Max(directInteractRange, AimAssistRadius);
        if (distance <= 0.01f || distance > range)
        {
            return false;
        }

        if (distance <= Mathf.Max(2.2f, AimAssistRadius))
        {
            return true;
        }

        float forwardDistance = Vector3.Dot(fromCamera, camera.transform.forward);
        if (forwardDistance <= 0f)
        {
            return false;
        }

        float distanceFromViewRay = (fromCamera - camera.transform.forward * forwardDistance).magnitude;
        if (distanceFromViewRay <= AimAssistRadius)
        {
            return true;
        }

        Vector3 viewportPoint = camera.WorldToViewportPoint(aimPoint);
        if (viewportPoint.z <= 0f)
        {
            return false;
        }

        Vector2 viewportOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
        return viewportOffset.magnitude <= directInteractViewportRadius;
    }

    private void StartRound()
    {
        _isPlaying = true;
        _isHolding = false;
        _roundResolved = false;
        _charge = 0f;
        _roundTime = 0f;

        float overusePenalty = Mathf.Max(0, _useCount - easyUseCount) * overuseZoneShrink;
        _goodWidth = Mathf.Clamp(goodZoneWidth - overusePenalty, 0.09f, 0.65f);
        _perfectWidth = Mathf.Clamp(perfectZoneWidth - overusePenalty * 0.5f, 0.035f, _goodWidth * 0.55f);
        _targetCenter = Random.Range(targetCenterMin, targetCenterMax);
        _targetCenter = Mathf.Clamp(_targetCenter, _goodWidth * 0.5f + 0.02f, 1f - _goodWidth * 0.5f - 0.02f);

        LockPlayerControl();
        BuildUi();
        TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Open, 0.44f, 1f);
        ShowToast("Giữ SPACE lấy hơi, thả vào vùng xanh.");
    }

    private void EvaluateRelease()
    {
        if (_charge < minChargeToRelease)
        {
            ResolveRound(PipeResult.Failed, "Chưa đủ hơi.");
            return;
        }

        float distance = Mathf.Abs(_charge - _targetCenter);
        if (distance <= _perfectWidth * 0.5f)
        {
            ResolveRound(PipeResult.Perfect, "PERFECT - tỉnh hẳn người.");
            return;
        }

        if (distance <= _goodWidth * 0.5f)
        {
            ResolveRound(PipeResult.Good, "GOOD - được một hơi ổn.");
            return;
        }

        ResolveRound(PipeResult.Failed, _charge > _targetCenter ? "Quá tay, bị sặc." : "Non hơi, chưa đã.");
    }

    private void ResolveRound(PipeResult result, string message)
    {
        if (_roundResolved)
        {
            return;
        }

        _roundResolved = true;
        _nextUseTime = useCooldown && cooldownSeconds > 0f ? Time.time + cooldownSeconds : 0f;
        _useCount++;
        ApplyResult(result, message);
        RefreshMeter();
        StartCoroutine(FinishAfterDelay(result));
    }

    private bool IsCoolingDown(out float remaining)
    {
        remaining = useCooldown && cooldownSeconds > 0f ? _nextUseTime - Time.time : 0f;
        return remaining > 0f;
    }

    private void ApplyResult(PipeResult result, string message)
    {
        PlayerNeeds needs = PlayerNeeds.EnsureInstance();
        switch (result)
        {
            case PipeResult.Perfect:
                needs.RecoverNeeds(perfectFatigueRecovery, 0f, 0f);
                needs.DrainNeeds(0f, 0f, perfectThirstPenalty);
                needs.ApplyTemporaryFatigueDrainMultiplier(perfectFatigueDrainMultiplier, perfectFocusDuration);
                TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Exhale, 0.5f, 1.02f);
                TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Perfect, 0.72f, 1f);
                ShowToast($"{message} Năng lượng +{Mathf.RoundToInt(perfectFatigueRecovery)}, khát -{Mathf.RoundToInt(perfectThirstPenalty)}.");
                break;
            case PipeResult.Good:
                needs.RecoverNeeds(goodFatigueRecovery, 0f, 0f);
                needs.DrainNeeds(0f, 0f, goodThirstPenalty);
                needs.ApplyTemporaryFatigueDrainMultiplier(goodFatigueDrainMultiplier, goodFocusDuration);
                TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Exhale, 0.42f, 0.94f);
                TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Good, 0.6f, 1f);
                ShowToast($"{message} Năng lượng +{Mathf.RoundToInt(goodFatigueRecovery)}, khát -{Mathf.RoundToInt(goodThirstPenalty)}.");
                break;
            case PipeResult.Failed:
                needs.DrainNeeds(failFatiguePenalty, 0f, failThirstPenalty);
                PlayImportedSfx("tieng_ho", TobaccoPipeSfxCue.Cough, 0.9f, Random.Range(0.93f, 1.06f), true);
                TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Fail, 0.54f, 1f);
                ShowToast($"{message} Bị khát và khựng lại một chút.");
                break;
            case PipeResult.Cancelled:
                TobaccoPipeSfxKit.Play(TobaccoPipeSfxCue.Fail, 0.35f, 0.85f);
                ShowToast(message);
                break;
        }

        if (_resultText != null)
        {
            _resultText.text = result == PipeResult.Perfect ? "PERFECT" : result == PipeResult.Good ? "GOOD" : result == PipeResult.Failed ? "SẶC RỒI" : message;
            _resultText.color = result == PipeResult.Perfect ? new Color(0.46f, 1f, 0.62f, 1f) : result == PipeResult.Good ? new Color(1f, 0.82f, 0.36f, 1f) : new Color(1f, 0.46f, 0.38f, 1f);
        }

        if (_hintText != null)
        {
            _hintText.text = result == PipeResult.Failed ? "Ho xong rồi hãy di chuyển tiếp." : "Nghỉ xong rồi quay lại làm việc.";
        }
    }

    private IEnumerator FinishAfterDelay(PipeResult result)
    {
        float delay = result == PipeResult.Failed ? Mathf.Max(0.6f, coughLockDuration) : 0.9f;
        yield return new WaitForSecondsRealtime(delay);
        CleanupUi();
        _isPlaying = false;
        _isHolding = false;
        UnlockPlayerControl();
    }

    private void LockPlayerControl()
    {
        if (!disablePlayerControlDuringMinigame)
        {
            return;
        }

        _lockedPlayerController = FindFirstObjectByType<PlayerController>();
        if (_lockedPlayerController != null)
        {
            _playerControllerWasEnabled = _lockedPlayerController.enabled;
            _lockedPlayerController.enabled = false;
        }
    }

    private void UnlockPlayerControl()
    {
        if (_lockedPlayerController != null)
        {
            _lockedPlayerController.enabled = _playerControllerWasEnabled;
            _lockedPlayerController = null;
        }
    }

    private void BuildUi()
    {
        CleanupUi();

        Sprite panelSprite = MinigameUiKit.CreateRoundedRectSprite(64, 64, 16, new Color(0.1f, 0.13f, 0.1f, 0.92f), new Color(0.95f, 0.78f, 0.38f, 0.32f));
        Sprite softSprite = MinigameUiKit.CreateRoundedRectSprite(64, 64, 18, Color.white, new Color(1f, 1f, 1f, 0.18f));
        Sprite circleSprite = MinigameUiKit.CreateCircleSprite(48, Color.white, new Color(1f, 1f, 1f, 0.75f), 3);

        _uiRoot = MinigameUiKit.CreateCanvasRoot("TobaccoPipe_UI", null, 520);
        Image dim = MinigameUiKit.CreateImage(_uiRoot.transform, "Dim", MinigameUiKit.CreateSolidSprite(Color.white), new Color(0.03f, 0.04f, 0.035f, 0.36f), false);
        MinigameUiKit.Stretch(dim.rectTransform);

        Image panel = MinigameUiKit.CreatePanel(_uiRoot.transform, "Panel", panelSprite, new Color(0.09f, 0.12f, 0.1f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 360f));
        MinigameUiKit.AddChrome(panel.transform, MinigameUiKit.CreateSolidSprite(Color.white), new Color(0.95f, 0.77f, 0.32f, 1f));

        TextMeshProUGUI title = MinigameUiKit.CreateText(panel.transform, "Title", "Làm một bi thuốc lào", 34, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.92f, 0.68f, 1f));
        MinigameUiKit.SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -48f), new Vector2(-48f, 52f));

        TextMeshProUGUI subtitle = MinigameUiKit.CreateText(panel.transform, "Subtitle", $"Giá {price:N0} VNĐ  •  Giữ SPACE lấy hơi, thả trong vùng xanh", 18, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.77f, 0.86f, 0.78f, 1f));
        MinigameUiKit.SetAnchored(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -88f), new Vector2(-48f, 38f));

        Image track = MinigameUiKit.CreatePanel(panel.transform, "MeterTrack", softSprite, new Color(0.03f, 0.06f, 0.05f, 0.86f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(MeterWidth, 54f));
        Image goodZone = MinigameUiKit.CreateImage(track.transform, "GoodZone", softSprite, new Color(0.96f, 0.74f, 0.24f, 0.52f), false);
        Image perfectZone = MinigameUiKit.CreateImage(track.transform, "PerfectZone", softSprite, new Color(0.28f, 1f, 0.55f, 0.76f), false);
        SetZone(goodZone.rectTransform, _targetCenter, _goodWidth);
        SetZone(perfectZone.rectTransform, _targetCenter, _perfectWidth);

        _fillImage = MinigameUiKit.CreateImage(track.transform, "BreathFill", softSprite, new Color(0.52f, 0.92f, 1f, 0.78f), false);
        RectTransform fillRt = _fillImage.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = new Vector2(0f, -10f);

        _needleImage = MinigameUiKit.CreateImage(track.transform, "Needle", circleSprite, new Color(0.78f, 0.96f, 1f, 1f), false);
        MinigameUiKit.SetAnchored(_needleImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-MeterWidth * 0.5f, 0f), new Vector2(28f, 72f));

        _statusText = MinigameUiKit.CreateText(panel.transform, "Status", "Sẵn sàng", 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        MinigameUiKit.SetAnchored(_statusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(680f, 40f));

        _hintText = MinigameUiKit.CreateText(panel.transform, "Hint", "Giữ SPACE hoặc chuột trái để lấy hơi. Thả ra đúng vùng xanh là Perfect.", 18, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.82f, 0.9f, 0.83f, 1f));
        MinigameUiKit.SetAnchored(_hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(720f, 48f));

        _resultText = MinigameUiKit.CreateText(panel.transform, "Result", "", 28, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        MinigameUiKit.SetAnchored(_resultText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(720f, 42f));

        MinigameUiKit.CreateButton(panel.transform, "Cancel", "DỪNG", panelSprite, new Color(0.32f, 0.16f, 0.1f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-82f, 45f), new Vector2(132f, 50f), () => ResolveRound(PipeResult.Cancelled, "Đã dừng lại."));
        RefreshMeter();
        RefreshStatus();
    }

    private void SetZone(RectTransform rect, float center, float width)
    {
        float half = width * 0.5f;
        rect.anchorMin = new Vector2(Mathf.Clamp01(center - half), 0f);
        rect.anchorMax = new Vector2(Mathf.Clamp01(center + half), 1f);
        rect.offsetMin = new Vector2(0f, 7f);
        rect.offsetMax = new Vector2(0f, -7f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private void RefreshMeter()
    {
        if (_fillImage != null)
        {
            _fillImage.rectTransform.sizeDelta = new Vector2(MeterWidth * Mathf.Clamp01(_charge), -10f);
        }

        if (_needleImage != null)
        {
            float x = Mathf.Lerp(-MeterWidth * 0.5f, MeterWidth * 0.5f, Mathf.Clamp01(_charge));
            _needleImage.rectTransform.anchoredPosition = new Vector2(x, 0f);
        }
    }

    private void RefreshStatus()
    {
        if (_statusText == null)
        {
            return;
        }

        if (!_isHolding)
        {
            _statusText.text = "Giữ SPACE để lấy hơi";
            _statusText.color = new Color(0.92f, 0.95f, 0.88f, 1f);
            return;
        }

        float distance = Mathf.Abs(_charge - _targetCenter);
        if (distance <= _perfectWidth * 0.5f)
        {
            _statusText.text = "PERFECT - thả ra!";
            _statusText.color = new Color(0.46f, 1f, 0.62f, 1f);
        }
        else if (distance <= _goodWidth * 0.5f)
        {
            _statusText.text = "GOOD - thả được rồi";
            _statusText.color = new Color(1f, 0.82f, 0.36f, 1f);
        }
        else
        {
            _statusText.text = _charge < _targetCenter ? "Còn non hơi..." : "Quá tay rồi...";
            _statusText.color = new Color(0.95f, 0.72f, 0.58f, 1f);
        }
    }

    private void CleanupUi()
    {
        if (_uiRoot != null)
        {
            Destroy(_uiRoot);
            _uiRoot = null;
        }
    }

    private void EnsureInteractionArea()
    {
        GameObject area = GetOrCreateArea();
        area.layer = gameObject.layer;

        // Tính toán localScale và localPosition dựa trên lossyScale của cha để giữ kích thước thực tế trong thế giới luôn chuẩn (ví dụ: 0.5m)
        Vector3 lossy = transform.lossyScale;
        float lx = Mathf.Max(0.001f, Mathf.Abs(lossy.x));
        float ly = Mathf.Max(0.001f, Mathf.Abs(lossy.y));
        float lz = Mathf.Max(0.001f, Mathf.Abs(lossy.z));

        area.transform.localPosition = new Vector3(
            interactionAreaCenter.x / lx,
            interactionAreaCenter.y / ly,
            interactionAreaCenter.z / lz);

        area.transform.localRotation = Quaternion.identity;

        area.transform.localScale = new Vector3(
            Mathf.Max(0.2f, interactionAreaSize.x) / lx,
            Mathf.Max(0.2f, interactionAreaSize.y) / ly,
            Mathf.Max(0.2f, interactionAreaSize.z) / lz);

        BoxCollider box = area.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = area.AddComponent<BoxCollider>();
        }

        box.isTrigger = true;
        box.size = Vector3.one;
        box.center = Vector3.zero;

        Renderer renderer = area.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = showTransparentInteractionArea && (!Application.isPlaying || showInteractionAreaWhilePlaying);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreateTransparentMaterial(interactionAreaColor, interactionAreaAlpha);
        }
    }

    private GameObject GetOrCreateArea()
    {
        Transform existing = transform.Find(AreaName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject area = GameObject.CreatePrimitive(PrimitiveType.Cube);
        area.name = AreaName;
        area.transform.SetParent(transform, false);
        return area;
    }

    private void EnsureVisual()
    {
        if (!createSimplePipeVisual || transform.Find(VisualName) != null)
        {
            return;
        }

        Transform root = new GameObject(VisualName).transform;
        root.SetParent(transform, false);

        Material bamboo = CreateOpaqueMaterial(new Color(0.64f, 0.45f, 0.2f, 1f));
        Material dark = CreateOpaqueMaterial(new Color(0.05f, 0.045f, 0.038f, 1f));
        Material metal = CreateOpaqueMaterial(new Color(0.78f, 0.82f, 0.76f, 1f));
        Material water = CreateOpaqueMaterial(new Color(0.2f, 0.48f, 0.38f, 1f));

        CreateVisualPrimitive(root, PrimitiveType.Cylinder, "PipeBody", bamboo, new Vector3(0f, 0.86f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.12f, 0.86f, 0.12f));
        CreateVisualPrimitive(root, PrimitiveType.Cylinder, "MouthPiece", metal, new Vector3(0.95f, 0.86f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.075f, 0.18f, 0.075f));
        CreateVisualPrimitive(root, PrimitiveType.Cylinder, "Bowl", dark, new Vector3(-0.72f, 1.02f, 0f), Quaternion.identity, new Vector3(0.16f, 0.16f, 0.16f));
        CreateVisualPrimitive(root, PrimitiveType.Cylinder, "WaterCup", water, new Vector3(-0.34f, 0.69f, 0f), Quaternion.identity, new Vector3(0.18f, 0.12f, 0.18f));

        if (createWorldLabel)
        {
            GameObject label = new GameObject("Label");
            label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.transform.localScale = Vector3.one * 0.045f;

            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = "THUỐC LÀO";
            textMesh.fontSize = 34;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(1f, 0.86f, 0.48f, 1f);
        }
    }

    private GameObject CreateVisualPrimitive(Transform parent, PrimitiveType type, string objectName, Material material, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = objectName;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = localRotation;
        obj.transform.localScale = localScale;

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }

        return obj;
    }

    private Material CreateOpaqueMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        return material;
    }

    private Material CreateTransparentMaterial(Color baseColor, float alpha)
    {
        Material material = CreateOpaqueMaterial(baseColor);
        Color color = baseColor;
        color.a = alpha;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        return material;
    }

    private void ShowToast(string message)
    {
        if (ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast(message, 2.8f);
        }
        else
        {
            Debug.Log("[TobaccoPipeStation] " + message);
        }
    }

    private void PlayImportedSfx(string soundName, TobaccoPipeSfxCue fallbackCue, float volume, float pitch, bool stopPrevious = false)
    {
        if (AudioManager.Instance != null)
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/" + soundName);
            if (clip == null)
            {
                clip = Resources.Load<AudioClip>(soundName);
            }

            if (clip != null)
            {
                AudioManager.Instance.PlaySFX(clip, volume, pitch, stopPrevious);
                return;
            }
        }

        TobaccoPipeSfxKit.Play(fallbackCue, volume, pitch);
    }

    private void OnDisable()
    {
        CleanupUi();
        UnlockPlayerControl();
        _isPlaying = false;
    }
}

public enum TobaccoPipeSfxCue
{
    Open,
    Inhale,
    Bubble,
    Exhale,
    Cough,
    Good,
    Perfect,
    Fail
}

public static class TobaccoPipeSfxKit
{
    private const int SampleRate = 44100;
    private static readonly System.Collections.Generic.Dictionary<TobaccoPipeSfxCue, AudioClip> Clips = new System.Collections.Generic.Dictionary<TobaccoPipeSfxCue, AudioClip>();

    public static void Play(TobaccoPipeSfxCue cue, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = GetClip(cue);
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, Mathf.Clamp01(volume), Mathf.Clamp(pitch * Random.Range(0.96f, 1.04f), 0.65f, 1.45f));
        }
    }

    private static AudioClip GetClip(TobaccoPipeSfxCue cue)
    {
        if (Clips.TryGetValue(cue, out AudioClip clip) && clip != null)
        {
            return clip;
        }

        clip = CreateClip(cue);
        Clips[cue] = clip;
        return clip;
    }

    private static AudioClip CreateClip(TobaccoPipeSfxCue cue)
    {
        switch (cue)
        {
            case TobaccoPipeSfxCue.Inhale: return CreateAir("TobaccoPipe_Inhale", 0.72f, true);
            case TobaccoPipeSfxCue.Bubble: return CreateBubble("TobaccoPipe_Bubble");
            case TobaccoPipeSfxCue.Exhale: return CreateAir("TobaccoPipe_Exhale", 0.8f, false);
            case TobaccoPipeSfxCue.Cough: return CreateCough("TobaccoPipe_Cough");
            case TobaccoPipeSfxCue.Good: return CreateTone("TobaccoPipe_Good", 0.22f, 520f, 690f, 0.28f);
            case TobaccoPipeSfxCue.Perfect: return CreatePerfect("TobaccoPipe_Perfect");
            case TobaccoPipeSfxCue.Fail: return CreateFail("TobaccoPipe_Fail");
            default: return CreateTone("TobaccoPipe_Open", 0.18f, 250f, 520f, 0.23f);
        }
    }

    private static AudioClip CreateClipData(string name, float duration, System.Func<float, int, float> sample)
    {
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
        float[] data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            data[i] = Mathf.Clamp(sample(t, i), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateAir(string name, float duration, bool inhale)
    {
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float env = inhale ? Mathf.Sin(p * Mathf.PI) : Mathf.Pow(1f - p, 0.75f);
            float hz = Mathf.Lerp(inhale ? 110f : 92f, inhale ? 78f : 55f, p);
            float low = Mathf.Sin(Mathf.PI * 2f * hz * t) * 0.16f;
            float air = HashNoise(i, inhale ? 211 : 223) * (inhale ? 0.2f : 0.26f);
            return (low + air) * env * 0.65f;
        });
    }

    private static AudioClip CreateBubble(string name)
    {
        const float duration = 0.42f;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float pulse = Mathf.Sin(Mathf.PI * 2f * 8f * t) > 0.3f ? 1f : 0f;
            float pop = Mathf.Sin(Mathf.PI * 2f * Mathf.Lerp(120f, 70f, p) * t) * 0.24f * pulse;
            float water = HashNoise(i, 241) * 0.13f;
            return (pop + water) * Envelope(t, duration, 0.012f, 0.13f);
        });
    }

    private static AudioClip CreateCough(string name)
    {
        const float duration = 0.78f;
        return CreateClipData(name, duration, (t, i) => Burst(t, 0.02f, 0.2f, 85f, i, 271) + Burst(t, 0.28f, 0.22f, 72f, i, 283) * 0.9f);
    }

    private static AudioClip CreateTone(string name, float duration, float hzA, float hzB, float volume)
    {
        return CreateClipData(name, duration, (t, i) =>
        {
            float env = Envelope(t, duration, 0.006f, 0.08f);
            return ((Mathf.Sin(Mathf.PI * 2f * hzA * t) + Mathf.Sin(Mathf.PI * 2f * hzB * t)) * 0.5f) * env * volume;
        });
    }

    private static AudioClip CreatePerfect(string name)
    {
        const float duration = 0.45f;
        return CreateClipData(name, duration, (t, i) => Note(t, 0f, 0.16f, 660f, 0.2f) + Note(t, 0.1f, 0.18f, 880f, 0.22f) + Note(t, 0.22f, 0.2f, 1320f, 0.17f));
    }

    private static AudioClip CreateFail(string name)
    {
        const float duration = 0.34f;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float tone = Mathf.Sin(Mathf.PI * 2f * Mathf.Lerp(190f, 75f, p) * t) * 0.34f;
            return (tone + HashNoise(i, 331) * 0.1f) * Envelope(t, duration, 0.01f, 0.14f);
        });
    }

    private static float Burst(float t, float start, float length, float hz, int i, int seed)
    {
        if (t < start || t > start + length)
        {
            return 0f;
        }

        float local = t - start;
        float p = Mathf.Clamp01(local / length);
        float chest = Mathf.Sin(Mathf.PI * 2f * hz * local) * Mathf.Exp(-p * 4.2f) * 0.34f;
        float rasp = HashNoise(i, seed) * Mathf.Exp(-p * 3.1f) * 0.26f;
        return (chest + rasp) * Envelope(local, length, 0.006f, 0.11f);
    }

    private static float Note(float t, float start, float length, float hz, float volume)
    {
        if (t < start || t > start + length)
        {
            return 0f;
        }

        float local = t - start;
        return Mathf.Sin(Mathf.PI * 2f * hz * local) * Envelope(local, length, 0.006f, length * 0.55f) * volume;
    }

    private static float Envelope(float t, float duration, float attack, float release)
    {
        float a = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
        float r = release <= 0f ? 1f : Mathf.Clamp01((duration - t) / release);
        return Mathf.Min(a, r);
    }

    private static float HashNoise(int index, int seed)
    {
        int n = index + (seed * 374761393);
        n = (n << 13) ^ n;
        int value = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
        return 1f - (value / 1073741824f);
    }
}
