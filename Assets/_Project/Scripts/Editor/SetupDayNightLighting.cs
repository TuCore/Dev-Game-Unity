using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupDayNightLighting
{
    [MenuItem("Tools/AI/Setup Day Night Lighting")]
    public static void Setup()
    {
        // 1. Tìm hoặc tạo Sun_DirectionalLight
        Light[] lights = Object.FindObjectsOfType<Light>(true);
        Light sunLight = null;
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional && l.name == "Sun_DirectionalLight")
            {
                sunLight = l;
                break;
            }
        }

        if (sunLight == null)
        {
            // Tìm Directional Light nào đó khác an toàn hơn (không gắn vào Camera hay Player)
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional && l.GetComponent<Camera>() == null && l.GetComponentInParent<Camera>() == null)
                {
                    sunLight = l;
                    sunLight.name = "Sun_DirectionalLight";
                    break;
                }
            }
        }

        if (sunLight == null)
        {
            GameObject sunObj = new GameObject("Sun_DirectionalLight");
            sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.shadows = LightShadows.Hard;
            // Xoay 50 độ trên trục Y để có bóng đẹp, X sẽ do script lo
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 2. Thêm DayNightLighting script
        DayNightLighting dayNight = sunLight.GetComponent<DayNightLighting>();
        if (dayNight == null)
        {
            dayNight = sunLight.gameObject.AddComponent<DayNightLighting>();
        }

        // 3. Setup Gradient (Sáng, Trưa, Chiều, Tối)
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.0f); // Sáng: Vàng nhạt (8:00)
        colorKeys[1] = new GradientColorKey(new Color(1f, 1f, 1f), 0.33f);    // Trưa: Trắng sáng (12:00)
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0.66f); // Chiều: Cam ấm (16:00)
        colorKeys[3] = new GradientColorKey(new Color(0.2f, 0.3f, 0.5f), 1.0f); // Tối: Xanh đậm/hoàng hôn (20:00)
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        
        colorGradient.SetKeys(colorKeys, alphaKeys);
        dayNight.lightColor = colorGradient;

        // 4. Setup Intensity Curve
        AnimationCurve intensityCurve = new AnimationCurve();
        intensityCurve.AddKey(new Keyframe(0.0f, 0.8f));   // Sáng cường độ vừa
        intensityCurve.AddKey(new Keyframe(0.33f, 1.2f));  // Trưa cường độ mạnh
        intensityCurve.AddKey(new Keyframe(0.66f, 0.9f));  // Chiều cường độ trung bình
        intensityCurve.AddKey(new Keyframe(1.0f, 0.2f));   // Tối cường độ thấp
        
        dayNight.lightIntensity = intensityCurve;
        dayNight.minRotation = 10f;
        dayNight.maxRotation = 90f;
        dayNight.lightingUpdateInterval = 0.2f;
        dayNight.maxStableNoonRotation = 82f;
        dayNight.yawSweep = 70f;
        dayNight.preferHardShadowsForPerformance = true;
        dayNight.normalShadowStrength = 0.55f;
        dayNight.noonShadowStrength = 0.35f;

        // Hard shadows are much cheaper and avoid the noon soft-shadow jitter in this scene.
        sunLight.shadows = LightShadows.Hard;
        sunLight.shadowStrength = dayNight.normalShadowStrength;
        
        // Đánh dấu Scene đã thay đổi để có thể Save
        EditorSceneManager.MarkSceneDirty(sunLight.gameObject.scene);

        Debug.Log("Đã tạo và setup thành công Sun_DirectionalLight cùng hệ thống DayNightLighting!");
    }
}
