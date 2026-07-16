using UnityEngine;

[RequireComponent(typeof(Light))]
public class DayNightLighting : MonoBehaviour
{
    [Header("Lighting Settings")]
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    [Header("Sun Rotation")]
    [Tooltip("Góc thấp nhất khi sáng sớm hoặc chiều tối")]
    public float minRotation = 0f;
    [Tooltip("Góc cao nhất khi giữa trưa")]
    public float maxRotation = 90f;

    private Light _directionalLight;

    private void Awake()
    {
        _directionalLight = GetComponent<Light>();
        if (_directionalLight.type != LightType.Directional)
        {
            Debug.LogWarning("[DayNightLighting] Script này nên được gắn vào Directional Light!");
        }
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
        // Try to update it immediately based on current hour if DayClock is active
        if (DayClock.Instance != null)
        {
            UpdateLighting(DayClock.Instance.CurrentHour);
        }
    }

    private void UpdateLighting(float currentHour)
    {
        if (_directionalLight == null) return;

        // Giả sử giờ mở cửa là 8 và đóng cửa là 20. 
        // Normalize time: 8 = 0.0, 20 = 1.0
        float t = Mathf.Clamp01((currentHour - 8f) / (20f - 8f));

        // Update Color
        if (lightColor != null)
        {
            _directionalLight.color = lightColor.Evaluate(t);
        }

        // Update Intensity
        if (lightIntensity != null)
        {
            _directionalLight.intensity = lightIntensity.Evaluate(t);
        }

        // Update Rotation (Sun arc)
        // Buổi sáng (0.0): góc nghiêng thấp
        // Buổi trưa (0.5): góc đứng (max)
        // Buổi chiều (1.0): góc nghiêng ngược lại (thấp)
        
        // Dùng đường cong parabol ngược để xoay.
        // t = 0 => 0 độ, t = 0.5 => 90 độ, t = 1 => 180 độ
        float angle = Mathf.Lerp(minRotation, 180f, t);
        
        // Xoay theo trục X
        transform.localRotation = Quaternion.Euler(angle, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z);
    }
}
