using UnityEngine;
using UnityEditor;

public class SetupWeatherTool
{
    [MenuItem("Tools/Môi trường/Thiết lập Thời tiết (Mưa \u0026 Sấm)")]
    public static void SetupWeather()
    {
        // 1. Tìm hoặc tạo WeatherManager
        WeatherManager weatherManager = Object.FindFirstObjectByType<WeatherManager>();
        if (weatherManager == null)
        {
            GameObject weatherObj = new GameObject("WeatherManager");
            weatherManager = weatherObj.AddComponent<WeatherManager>();
        }
        else
        {
            weatherManager.gameObject.name = "WeatherManager";
        }

        // 2. Tạo Particle System Mưa (nếu chưa có)
        if (weatherManager.rainParticles == null)
        {
            Transform existingRain = weatherManager.transform.Find("RainParticles");
            if (existingRain != null)
            {
                weatherManager.rainParticles = existingRain.GetComponent<ParticleSystem>();
            }
            else
            {
                GameObject rainObj = new GameObject("RainParticles");
                rainObj.transform.SetParent(weatherManager.transform);
                // Mưa rơi từ trên cao xuống
                rainObj.transform.localPosition = new Vector3(0, 30f, 0);
                rainObj.transform.localRotation = Quaternion.Euler(90f, 0, 0); // Xoay chúc xuống dưới

                ParticleSystem ps = rainObj.AddComponent<ParticleSystem>();
                ParticleSystemRenderer psRenderer = rainObj.GetComponent<ParticleSystemRenderer>();

                // Cấu hình cơ bản cho hạt mưa
                var main = ps.main;
                main.duration = 5f;
                main.loop = true;
                main.startLifetime = 2f;
                main.startSpeed = 20f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
                main.startColor = new Color(0.8f, 0.9f, 1f, 0.6f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 5000;

                var emission = ps.emission;
                emission.rateOverTime = 1000f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(100f, 100f, 1f); // Vùng bao phủ rộng

                // Vẽ hạt mưa thon dài (nếu dùng material mặc định)
                psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                psRenderer.cameraVelocityScale = 0f;
                psRenderer.velocityScale = 0.1f;
                psRenderer.lengthScale = 2f;
                psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

                weatherManager.rainParticles = ps;
            }
        }

        // Tắt mưa mặc định lúc mới vào
        weatherManager.rainParticles.Stop();

        // 3. Tạo AudioSource Tiếng mưa
        if (weatherManager.rainAudio == null)
        {
            Transform existingRainAudio = weatherManager.transform.Find("RainAudio");
            if (existingRainAudio != null)
            {
                weatherManager.rainAudio = existingRainAudio.GetComponent<AudioSource>();
            }
            else
            {
                GameObject rainAudioObj = new GameObject("RainAudio");
                rainAudioObj.transform.SetParent(weatherManager.transform);
                AudioSource src = rainAudioObj.AddComponent<AudioSource>();
                src.loop = true;
                src.playOnAwake = false;
                src.volume = 0.5f;
                weatherManager.rainAudio = src;
            }
        }

        // 4. Tạo AudioSource Tiếng sấm
        if (weatherManager.thunderAudio == null)
        {
            Transform existingThunderAudio = weatherManager.transform.Find("ThunderAudio");
            if (existingThunderAudio != null)
            {
                weatherManager.thunderAudio = existingThunderAudio.GetComponent<AudioSource>();
            }
            else
            {
                GameObject thunderAudioObj = new GameObject("ThunderAudio");
                thunderAudioObj.transform.SetParent(weatherManager.transform);
                AudioSource src = thunderAudioObj.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;
                src.volume = 0.8f;
                weatherManager.thunderAudio = src;
            }
        }

        // 5. Gán Sun_DirectionalLight
        if (weatherManager.directionalLight == null)
        {
            Light[] lights = Object.FindObjectsOfType<Light>(true);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.name == "Sun_DirectionalLight")
                {
                    weatherManager.directionalLight = l;
                    break;
                }
            }
        }

        // Đánh dấu là đã thay đổi để Unity lưu lại
        EditorUtility.SetDirty(weatherManager);
        if (weatherManager.gameObject.scene != null && weatherManager.gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(weatherManager.gameObject.scene);
        }

        Debug.Log("Đã thiết lập WeatherManager thành công! Vui lòng tự kéo thả file âm thanh (nếu có) vào các AudioSource trong WeatherManager.");
    }
}
