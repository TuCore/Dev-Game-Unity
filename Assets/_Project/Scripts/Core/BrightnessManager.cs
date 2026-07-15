using UnityEngine;
using UnityEngine.UI;

public class BrightnessManager : MonoBehaviour
{
    private static BrightnessManager _instance;
    public static BrightnessManager Instance => _instance;
    private Image overlayImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("BrightnessManager");
            _instance = go.AddComponent<BrightnessManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Create Canvas overlay
        GameObject canvasGo = new GameObject("BrightnessCanvas");
        canvasGo.transform.SetParent(transform);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Always on top

        GameObject imageGo = new GameObject("BrightnessOverlay");
        imageGo.transform.SetParent(canvas.transform, false);
        overlayImage = imageGo.AddComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = false;

        RectTransform rect = overlayImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        UpdateBrightness();
    }

    public void UpdateBrightness()
    {
        float brightness = PlayerPrefs.GetFloat("Brightness", 1f);
        // brightness 1 = full bright (0 alpha)
        // brightness 0 = dark (0.8 alpha)
        float alpha = Mathf.Lerp(0.85f, 0f, brightness);
        
        if (overlayImage != null)
        {
            overlayImage.color = new Color(0, 0, 0, alpha);
        }
    }
}
