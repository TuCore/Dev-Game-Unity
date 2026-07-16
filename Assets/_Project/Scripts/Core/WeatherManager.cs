using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    [Header("Cấu hình Lịch trình Mưa")]
    [Tooltip("Ngày sẽ xảy ra trời mưa bão")]
    public int rainyDay = 2;
    [Tooltip("Thời điểm bắt đầu mưa (Giờ trong game, VD: 10f là 10:00 sáng)")]
    public float rainStartHour = 8f;
    [Tooltip("Thời gian kéo dài cơn mưa bão (Tính bằng Giờ trong game, VD: 4 tiếng). Nhập 0 để mưa cả ngày.")]
    public float rainDurationHours = 0f;

    [Header("Cấu hình Cường độ Mưa")]
    [Range(0.1f, 10f)]
    [Tooltip("Cường độ mưa: Số càng to mưa càng dày hạt và gió rít to hơn.")]
    public float rainIntensity = 1f;

    [Header("Tham chiếu Hệ thống (Tự động gán)")]
    public ParticleSystem rainParticles;
    public AudioSource rainAudio;
    public AudioSource thunderAudio;
    public Light directionalLight; // Sun_DirectionalLight

    [Header("Cấu hình Sấm Chớp")]
    public float minLightningInterval = 5f;
    public float maxLightningInterval = 15f;

    private Coroutine _lightningCoroutine;
    private Color _originalLightColor;
    private float _originalLightIntensity;
    private bool _isRaining = false;
    private ParticleSystem.EmissionModule _rainEmission;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (rainParticles != null)
        {
            _rainEmission = rainParticles.emission;
        }

        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged += CheckWeatherConditions;
            // Kiểm tra luôn lúc mới load scene
            CheckWeatherConditions(DayClock.Instance.CurrentHour);
        }
    }

    private void OnDestroy()
    {
        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged -= CheckWeatherConditions;
        }
    }

    private void CheckWeatherConditions(float currentHour)
    {
        int currentDay = DayClock.Instance.CurrentDay;

        // Nếu đúng ngày mưa
        if (currentDay == rainyDay)
        {
            bool shouldRain = false;

            if (rainDurationHours <= 0)
            {
                // Mưa cả ngày
                shouldRain = true;
            }
            else
            {
                // Mưa trong khung giờ chỉ định
                if (currentHour >= rainStartHour && currentHour <= rainStartHour + rainDurationHours)
                {
                    shouldRain = true;
                }
            }

            if (shouldRain && !_isRaining)
            {
                StartRainAndThunder();
            }
            else if (!shouldRain && _isRaining)
            {
                StopWeather();
            }
        }
        else
        {
            if (_isRaining)
            {
                StopWeather();
            }
        }
    }

    public void StartRainAndThunder()
    {
        _isRaining = true;

        if (rainParticles != null)
        {
            // Cường độ mưa: Nhân tốc độ hạt rơi và số lượng hạt
            _rainEmission.rateOverTime = 1000f * rainIntensity;
            
            var mainModule = rainParticles.main;
            mainModule.startSpeed = new ParticleSystem.MinMaxCurve(20f * Mathf.Clamp(rainIntensity, 0.5f, 2f));
            
            if (!rainParticles.isPlaying) rainParticles.Play();
        }
        
        if (rainAudio != null)
        {
            rainAudio.volume = Mathf.Clamp(0.3f * rainIntensity, 0.1f, 1f);
            if (!rainAudio.isPlaying) rainAudio.Play();
        }

        if (_lightningCoroutine == null)
        {
            _lightningCoroutine = StartCoroutine(LightningRoutine());
        }
    }

    public void StopWeather()
    {
        _isRaining = false;

        if (rainParticles != null) rainParticles.Stop();
        if (rainAudio != null) rainAudio.Stop();

        if (_lightningCoroutine != null)
        {
            StopCoroutine(_lightningCoroutine);
            _lightningCoroutine = null;
        }

        // Khôi phục sáng nếu đang chớp
        if (directionalLight != null && _originalLightIntensity > 0)
        {
            // SetupDayNightLighting sẽ tự động update đè lại màu ở khung hình sau, không lo bị kẹt màu trắng
        }
    }

    private IEnumerator LightningRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minLightningInterval, maxLightningInterval) / Mathf.Clamp(rainIntensity, 1f, 5f);
            yield return new WaitForSeconds(waitTime);

            // Chớp chớp ánh sáng (nếu có Directional Light)
            if (directionalLight != null)
            {
                _originalLightColor = directionalLight.color;
                _originalLightIntensity = directionalLight.intensity;

                // Chớp 1
                directionalLight.color = Color.white;
                directionalLight.intensity = _originalLightIntensity * 3f;
                yield return new WaitForSeconds(0.05f);
                directionalLight.intensity = _originalLightIntensity * 0.5f;
                yield return new WaitForSeconds(0.05f);

                // Chớp 2
                directionalLight.color = Color.cyan;
                directionalLight.intensity = _originalLightIntensity * 4f;
                yield return new WaitForSeconds(0.1f);
                
                directionalLight.color = _originalLightColor;
                directionalLight.intensity = _originalLightIntensity;
            }

            // Tiếng sấm (delay một chút để giống thật - chớp trước sấm sau)
            yield return new WaitForSeconds(Random.Range(0.2f, 0.8f));
            if (thunderAudio != null && thunderAudio.clip != null)
            {
                thunderAudio.volume = Mathf.Clamp(0.5f * rainIntensity, 0.2f, 1f);
                thunderAudio.PlayOneShot(thunderAudio.clip);
            }
        }
    }
}
