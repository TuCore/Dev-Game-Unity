using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MinigameUiKit
{
    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    public static GameObject CreateCanvasRoot(string name, Transform parent, int sortingOrder)
    {
        EnsureEventSystem();

        GameObject root = CreateUIObject(name, parent);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        root.AddComponent<MinigameUiCanvasIntro>();
        Stretch(root.GetComponent<RectTransform>());
        return root;
    }

    public static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool raycastTarget = true)
    {
        GameObject obj = CreateUIObject(name, parent);
        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static Image CreatePanel(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        Image image = CreateImage(parent, name, sprite, color);
        image.type = Image.Type.Sliced;
        SetAnchored(image.rectTransform, anchorMin, anchorMax, anchoredPosition, size);
        if (size.x >= 80f && size.y >= 42f)
        {
            ApplyPanelDepth(image);
        }

        return image;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    public static Button CreateButton(Transform parent, string name, string label, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreatePanel(parent, name, sprite, color, anchorMin, anchorMax, anchoredPosition, size);
        Button button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(() => MinigameSfxKit.Play(MinigameSfxCue.Button, 0.28f));
        button.onClick.AddListener(onClick);
        ConfigureButtonColors(button, color);
        image.gameObject.AddComponent<MinigameUiButtonMotion>();

        TextMeshProUGUI text = CreateText(image.transform, "Label", label, 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return button;
    }

    public static void AddChrome(Transform parent, Sprite sprite, Color accentColor)
    {
        Image topLine = CreateImage(parent, "Chrome_AccentLine", sprite, accentColor, false);
        SetAnchored(topLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 4f));
        topLine.transform.SetAsFirstSibling();

        Image highlight = CreateImage(parent, "Chrome_Highlight", sprite, new Color(1f, 1f, 1f, 0.035f), false);
        SetAnchored(highlight.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(0f, 44f));
        highlight.transform.SetAsFirstSibling();

        Image bottomShade = CreateImage(parent, "Chrome_BottomShade", sprite, new Color(0f, 0f, 0f, 0.16f), false);
        SetAnchored(bottomShade.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 12f), new Vector2(0f, 24f));
        bottomShade.transform.SetAsFirstSibling();
    }

    public static void ConfigureButtonColors(Button button, Color baseColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
        colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.12f);
        colors.disabledColor = new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, baseColor.b * 0.5f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void ApplyPanelDepth(Image image)
    {
        Shadow shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        shadow.effectDistance = new Vector2(0f, -7f);

        UnityEngine.UI.Outline outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.055f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    public static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    public static Image CreateLine(Transform parent, string name, Sprite sprite, Color color, Vector2 from, Vector2 to, float width)
    {
        Image line = CreateImage(parent, name, sprite, color, false);
        SetLine(line.rectTransform, from, to, width);
        return line;
    }

    public static void SetLine(RectTransform rt, Vector2 from, Vector2 to, float width)
    {
        Vector2 diff = to - from;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = from;
        rt.sizeDelta = new Vector2(diff.magnitude, width);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
        rt.localScale = Vector3.one;
    }

    public static Sprite CreateSolidSprite(Color color)
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Sprite CreateRoundedRectSprite(int width, int height, int radius, Color fill, Color outline)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Clear(texture, new Color(0f, 0f, 0f, 0f));
        FillRounded(texture, 0, 0, width, height, radius, fill, outline);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    public static Sprite CreateCircleSprite(int size, Color fill, Color outline, int outlineWidth)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Clear(texture, new Color(0f, 0f, 0f, 0f));
        int center = size / 2;
        FillCircle(texture, center, center, center - 1, outline);
        FillCircle(texture, center, center, Mathf.Max(1, center - outlineWidth), fill);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public static void Clear(Texture2D texture, Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    public static void FillRect(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
    {
        int minX = Mathf.Clamp(Mathf.Min(x0, x1), 0, texture.width);
        int maxX = Mathf.Clamp(Mathf.Max(x0, x1), 0, texture.width);
        int minY = Mathf.Clamp(Mathf.Min(y0, y1), 0, texture.height);
        int maxY = Mathf.Clamp(Mathf.Max(y0, y1), 0, texture.height);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    public static void FillCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            if (y < 0 || y >= texture.height)
            {
                continue;
            }

            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= texture.width)
                {
                    continue;
                }

                int dx = x - cx;
                int dy = y - cy;
                if ((dx * dx) + (dy * dy) <= r2)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    public static void FillRounded(Texture2D texture, int x0, int y0, int x1, int y1, int radius, Color fill, Color outline)
    {
        int minX = Mathf.Clamp(Mathf.Min(x0, x1), 0, texture.width);
        int maxX = Mathf.Clamp(Mathf.Max(x0, x1), 0, texture.width);
        int minY = Mathf.Clamp(Mathf.Min(y0, y1), 0, texture.height);
        int maxY = Mathf.Clamp(Mathf.Max(y0, y1), 0, texture.height);
        int r = Mathf.Max(1, radius);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                int dx = x < minX + r ? (minX + r) - x : x > maxX - r ? x - (maxX - r) : 0;
                int dy = y < minY + r ? (minY + r) - y : y > maxY - r ? y - (maxY - r) : 0;
                if ((dx * dx) + (dy * dy) > r * r)
                {
                    continue;
                }

                bool border = x < minX + 2 || x >= maxX - 2 || y < minY + 2 || y >= maxY - 2;
                texture.SetPixel(x, y, border ? outline : fill);
            }
        }
    }

    public static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int width)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int x = x0;
        int y = y0;

        while (true)
        {
            FillCircle(texture, x, y, Mathf.Max(1, width / 2), color);
            if (x == x1 && y == y1)
            {
                break;
            }

            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
}

public sealed class MinigameUiButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float HoverScale = 1.025f;
    private const float PressedScale = 0.975f;
    private const float Smooth = 16f;

    private RectTransform _rectTransform;
    private float _targetScale = 1f;
    private bool _hovered;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        _targetScale = 1f;
        if (_rectTransform != null)
        {
            _rectTransform.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        if (_rectTransform == null)
        {
            return;
        }

        float current = _rectTransform.localScale.x;
        float next = Mathf.Lerp(current, _targetScale, 1f - Mathf.Exp(-Smooth * Time.unscaledDeltaTime));
        _rectTransform.localScale = new Vector3(next, next, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        _targetScale = HoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        _targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = PressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _targetScale = _hovered ? HoverScale : 1f;
    }
}

public sealed class MinigameUiCanvasIntro : MonoBehaviour
{
    private const float Smooth = 13f;

    private CanvasGroup _group;
    private RectTransform _rectTransform;
    private float _alphaTarget = 1f;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_group != null)
        {
            _group.alpha = 0f;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = new Vector3(0.985f, 0.985f, 1f);
        }
    }

    private void Update()
    {
        float blend = 1f - Mathf.Exp(-Smooth * Time.unscaledDeltaTime);

        if (_group != null)
        {
            _group.alpha = Mathf.Lerp(_group.alpha, _alphaTarget, blend);
        }

        if (_rectTransform != null)
        {
            float next = Mathf.Lerp(_rectTransform.localScale.x, 1f, blend);
            _rectTransform.localScale = new Vector3(next, next, 1f);
        }
    }
}

public enum MinigameSfxCue
{
    Open,
    Button,
    Select,
    WirePick,
    WireConnect,
    Error,
    Success,
    Failure,
    ScrubSoft,
    ScrubRough,
    Probe,
    Beep,
    Solder,
    Pump,
    Tweezers,
    PlacePart,
    Rotate
}

public static class MinigameSfxKit
{
    private const int SampleRate = 44100;

    private static readonly Dictionary<MinigameSfxCue, AudioClip> Clips = new Dictionary<MinigameSfxCue, AudioClip>();
    private static readonly Dictionary<MinigameSfxCue, float> LastPlayedAt = new Dictionary<MinigameSfxCue, float>();

    public static void Play(MinigameSfxCue cue, float volume = 1f, float pitch = 1f)
    {
        float cooldown = GetCooldown(cue);
        if (cooldown > 0f && LastPlayedAt.TryGetValue(cue, out float lastPlayed) && Time.unscaledTime - lastPlayed < cooldown)
        {
            return;
        }

        LastPlayedAt[cue] = Time.unscaledTime;
        AudioClip clip = GetClip(cue);
        float jitter = GetPitchJitter(cue);
        float finalPitch = Mathf.Clamp(pitch * Random.Range(1f - jitter, 1f + jitter), 0.55f, 1.65f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, Mathf.Clamp01(volume), finalPitch);
        }
    }

    private static AudioClip GetClip(MinigameSfxCue cue)
    {
        if (Clips.TryGetValue(cue, out AudioClip clip) && clip != null)
        {
            return clip;
        }

        clip = CreateClip(cue);
        Clips[cue] = clip;
        return clip;
    }

    private static AudioClip CreateClip(MinigameSfxCue cue)
    {
        switch (cue)
        {
            case MinigameSfxCue.Open: return CreateSweep("MG_Open", 0.22f, 210f, 640f, 0.32f);
            case MinigameSfxCue.Button: return CreateClick("MG_Button", 0.055f, 1180f, 0.34f, 12);
            case MinigameSfxCue.Select: return CreateToneStack("MG_Select", 0.09f, 0.28f, 580f, 820f);
            case MinigameSfxCue.WirePick: return CreateClick("MG_WirePick", 0.075f, 520f, 0.45f, 23);
            case MinigameSfxCue.WireConnect: return CreateConnect("MG_WireConnect");
            case MinigameSfxCue.Error: return CreateError("MG_Error");
            case MinigameSfxCue.Success: return CreateSuccess("MG_Success");
            case MinigameSfxCue.Failure: return CreateFailure("MG_Failure");
            case MinigameSfxCue.ScrubSoft: return CreateScrub("MG_ScrubSoft", false);
            case MinigameSfxCue.ScrubRough: return CreateScrub("MG_ScrubRough", true);
            case MinigameSfxCue.Probe: return CreateClick("MG_Probe", 0.06f, 1520f, 0.38f, 41);
            case MinigameSfxCue.Beep: return CreateToneStack("MG_Beep", 0.12f, 0.32f, 1080f);
            case MinigameSfxCue.Solder: return CreateSolder("MG_Solder");
            case MinigameSfxCue.Pump: return CreatePump("MG_Pump");
            case MinigameSfxCue.Tweezers: return CreateClick("MG_Tweezers", 0.07f, 1760f, 0.32f, 53);
            case MinigameSfxCue.PlacePart: return CreateClick("MG_PlacePart", 0.09f, 360f, 0.42f, 61);
            case MinigameSfxCue.Rotate: return CreateRotate("MG_Rotate");
            default: return CreateClick("MG_Default", 0.06f, 800f, 0.3f, 7);
        }
    }

    private static float GetCooldown(MinigameSfxCue cue)
    {
        switch (cue)
        {
            case MinigameSfxCue.ScrubSoft:
            case MinigameSfxCue.ScrubRough:
                return 0.055f;
            case MinigameSfxCue.Probe:
                return 0.03f;
            default:
                return 0f;
        }
    }

    private static float GetPitchJitter(MinigameSfxCue cue)
    {
        switch (cue)
        {
            case MinigameSfxCue.ScrubSoft:
            case MinigameSfxCue.ScrubRough:
            case MinigameSfxCue.Solder:
            case MinigameSfxCue.Pump:
            case MinigameSfxCue.Tweezers:
            case MinigameSfxCue.PlacePart:
                return 0.08f;
            default:
                return 0.035f;
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

    private static AudioClip CreateToneStack(string name, float duration, float volume, params float[] frequencies)
    {
        return CreateClipData(name, duration, (t, i) =>
        {
            float env = Envelope(t, duration, 0.006f, 0.055f);
            float sample = 0f;
            for (int f = 0; f < frequencies.Length; f++)
            {
                sample += Mathf.Sin(Mathf.PI * 2f * frequencies[f] * t) / frequencies.Length;
            }

            return sample * env * volume;
        });
    }

    private static AudioClip CreateSweep(string name, float duration, float fromHz, float toHz, float volume)
    {
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float hz = Mathf.Lerp(fromHz, toHz, p);
            float tone = Mathf.Sin(Mathf.PI * 2f * hz * t);
            float air = HashNoise(i, 19) * 0.08f * (1f - p);
            return (tone * 0.78f + air) * Envelope(t, duration, 0.006f, 0.11f) * volume;
        });
    }

    private static AudioClip CreateClick(string name, float duration, float baseHz, float volume, int seed)
    {
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float env = Mathf.Exp(-p * 11f);
            float tone = Mathf.Sin(Mathf.PI * 2f * baseHz * t) * 0.62f;
            float noise = HashNoise(i, seed) * 0.38f;
            return (tone + noise) * env * volume;
        });
    }

    private static AudioClip CreateConnect(string name)
    {
        const float duration = 0.16f;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float snap = Mathf.Sin(Mathf.PI * 2f * 240f * t) * Mathf.Exp(-p * 9f) * 0.42f;
            float metal = Mathf.Sin(Mathf.PI * 2f * 1280f * t) * Mathf.Exp(-p * 20f) * 0.18f;
            float noise = HashNoise(i, 31) * Mathf.Exp(-p * 16f) * 0.18f;
            return snap + metal + noise;
        });
    }

    private static AudioClip CreateError(string name)
    {
        const float duration = 0.18f;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float hz = Mathf.Lerp(210f, 82f, p);
            float buzz = Mathf.Sign(Mathf.Sin(Mathf.PI * 2f * hz * t)) * 0.38f;
            float grind = HashNoise(i, 83) * 0.14f;
            return (buzz + grind) * Envelope(t, duration, 0.006f, 0.06f);
        });
    }

    private static AudioClip CreateSuccess(string name)
    {
        const float duration = 0.42f;
        return CreateClipData(name, duration, (t, i) =>
        {
            return Note(t, 0f, 0.15f, 660f, 0.22f)
                + Note(t, 0.11f, 0.16f, 880f, 0.22f)
                + Note(t, 0.22f, 0.18f, 1320f, 0.18f);
        });
    }

    private static AudioClip CreateFailure(string name)
    {
        const float duration = 0.36f;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float hz = Mathf.Lerp(170f, 62f, p);
            float tone = Mathf.Sin(Mathf.PI * 2f * hz * t) * 0.34f;
            float wobble = Mathf.Sin(Mathf.PI * 2f * 13f * t) * 0.16f;
            return (tone + wobble + HashNoise(i, 71) * 0.06f) * Envelope(t, duration, 0.008f, 0.12f);
        });
    }

    private static AudioClip CreateScrub(string name, bool rough)
    {
        float duration = rough ? 0.13f : 0.105f;
        int seed = rough ? 101 : 97;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float rasp = HashNoise(i, seed);
            float grain = HashNoise(i * 3, seed + 11) * Mathf.Sin(Mathf.PI * 2f * (rough ? 95f : 62f) * t);
            float scrape = (rasp * (rough ? 0.38f : 0.22f)) + (grain * (rough ? 0.24f : 0.13f));
            return scrape * Envelope(t, duration, 0.003f, 0.04f) * (1f - p * 0.25f);
        });
    }

    private static AudioClip CreateSolder(string name)
    {
        const float duration = 0.19f;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float hiss = HashNoise(i, 117) * 0.22f;
            float fizz = Mathf.Sin(Mathf.PI * 2f * 2350f * t) * 0.08f * Mathf.Sin(Mathf.PI * 2f * 37f * t);
            return (hiss + fizz) * Envelope(t, duration, 0.008f, 0.08f) * (1f - p * 0.35f);
        });
    }

    private static AudioClip CreatePump(string name)
    {
        const float duration = 0.18f;
        return CreateClipData(name, duration, (t, i) =>
        {
            float p = Mathf.Clamp01(t / duration);
            float pop = Mathf.Sin(Mathf.PI * 2f * 115f * t) * Mathf.Exp(-p * 12f) * 0.42f;
            float air = HashNoise(i, 131) * Mathf.Exp(-p * 5.5f) * 0.2f;
            return pop + air;
        });
    }

    private static AudioClip CreateRotate(string name)
    {
        const float duration = 0.16f;
        return CreateClipData(name, duration, (t, i) =>
        {
            return Note(t, 0f, 0.055f, 620f, 0.2f)
                + Note(t, 0.075f, 0.055f, 760f, 0.18f)
                + (HashNoise(i, 147) * Envelope(t, duration, 0.003f, 0.05f) * 0.04f);
        });
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
