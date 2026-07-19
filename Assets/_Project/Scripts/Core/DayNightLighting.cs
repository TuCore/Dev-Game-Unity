using UnityEngine;

[RequireComponent(typeof(Light))]
public class DayNightLighting : MonoBehaviour
{
    [Header("Lighting Settings")]
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    [Header("Sun Rotation")]
    [Tooltip("Lowest sun pitch at morning/evening.")]
    public float minRotation = 10f;
    [Tooltip("Highest sun pitch at noon.")]
    public float maxRotation = 90f;

    [Header("Performance/Stability")]
    [Tooltip("Realtime shadows are expensive if the sun rotates every frame.")]
    public float lightingUpdateInterval = 0.2f;
    [Tooltip("Keeps noon away from a perfectly vertical 90-degree sun to reduce shadow jitter.")]
    public float maxStableNoonRotation = 82f;
    [Tooltip("How much the sun yaw moves from morning to evening.")]
    public float yawSweep = 70f;
    public bool preferHardShadowsForPerformance = true;
    [Range(0f, 1f)] public float normalShadowStrength = 0.55f;
    [Range(0f, 1f)] public float noonShadowStrength = 0.35f;

    private Light _directionalLight;
    private float _baseYaw;
    private float _baseRoll;
    private float _nextAllowedUpdateTime;

    private void Awake()
    {
        _directionalLight = GetComponent<Light>();
        Vector3 baseEuler = transform.localEulerAngles;
        _baseYaw = baseEuler.y;
        _baseRoll = baseEuler.z;

        if (_directionalLight.type != LightType.Directional)
        {
            Debug.LogWarning("[DayNightLighting] This script should be attached to a Directional Light.");
        }

        ApplyShadowPerformanceSettings();
    }

    private void OnEnable()
    {
        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged += UpdateLighting;
        }
    }

    private void OnDisable()
    {
        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged -= UpdateLighting;
        }
    }

    private void Start()
    {
        if (DayClock.Instance != null)
        {
            UpdateLighting(DayClock.Instance.CurrentHour, true);
        }
    }

    private void UpdateLighting(float currentHour)
    {
        UpdateLighting(currentHour, false);
    }

    private void UpdateLighting(float currentHour, bool force)
    {
        if (_directionalLight == null)
        {
            return;
        }

        float minInterval = Mathf.Max(0.02f, lightingUpdateInterval);
        if (!force && Application.isPlaying && Time.time < _nextAllowedUpdateTime)
        {
            return;
        }

        _nextAllowedUpdateTime = Time.time + minInterval;

        float t = Mathf.Clamp01((currentHour - 8f) / (20f - 8f));

        if (lightColor != null)
        {
            _directionalLight.color = lightColor.Evaluate(t);
        }

        if (lightIntensity != null)
        {
            _directionalLight.intensity = lightIntensity.Evaluate(t);
        }

        float noonRotation = Mathf.Clamp(Mathf.Min(maxRotation, maxStableNoonRotation), minRotation, 89f);
        float sunHeight = Mathf.Sin(t * Mathf.PI);
        float pitch = Mathf.Lerp(minRotation, noonRotation, sunHeight);
        float yaw = _baseYaw + Mathf.Lerp(-yawSweep * 0.5f, yawSweep * 0.5f, t);

        transform.localRotation = Quaternion.Euler(pitch, yaw, _baseRoll);
        UpdateShadowStrength(t);
    }

    private void ApplyShadowPerformanceSettings()
    {
        if (_directionalLight == null)
        {
            return;
        }

        if (preferHardShadowsForPerformance && _directionalLight.shadows == LightShadows.Soft)
        {
            _directionalLight.shadows = LightShadows.Hard;
        }
    }

    private void UpdateShadowStrength(float normalizedDayTime)
    {
        if (_directionalLight == null || _directionalLight.shadows == LightShadows.None)
        {
            return;
        }

        float noonFactor = 1f - Mathf.Clamp01(Mathf.Abs(normalizedDayTime - 0.5f) * 2f);
        _directionalLight.shadowStrength = Mathf.Lerp(normalShadowStrength, noonShadowStrength, noonFactor);
    }
}
