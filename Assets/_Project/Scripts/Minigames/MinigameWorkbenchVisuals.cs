using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum MinigameWorkbenchStyle
{
    Diagnosis,
    Wiring,
    Cleaning,
    Replacement
}

public static class MinigameWorkbenchVisuals
{
    public static void Install(GameObject root, MinigameWorkbenchStyle style, Color accentColor)
    {
        if (root == null)
        {
            return;
        }

        MinigameWorkbenchRig rig = root.GetComponent<MinigameWorkbenchRig>();
        if (rig == null)
        {
            rig = root.AddComponent<MinigameWorkbenchRig>();
        }

        rig.Configure(style, accentColor);
    }
}

public sealed class MinigameWorkbenchRig : MonoBehaviour
{
    private const string BackdropName = "Workbench_BackdropRoot";
    private MinigameWorkbenchStyle style;
    private Color accentColor = Color.cyan;
    private bool configured;
    private bool decorated;
    private Sprite solidSprite;
    private Sprite softSprite;
    private Sprite circleSprite;

    public void Configure(MinigameWorkbenchStyle newStyle, Color newAccentColor)
    {
        style = newStyle;
        accentColor = newAccentColor;
        configured = true;

        if (isActiveAndEnabled && !decorated)
        {
            StartCoroutine(DecorateNextFrame());
        }
    }

    private void OnEnable()
    {
        if (configured && !decorated)
        {
            StartCoroutine(DecorateNextFrame());
        }
    }

    private IEnumerator DecorateNextFrame()
    {
        yield return null;
        yield return null;

        if (decorated || !configured || gameObject == null)
        {
            yield break;
        }

        EnsureSprites();
        CreateBackdrop();
        DecorateExistingUi();
        decorated = true;
    }

    private void EnsureSprites()
    {
        if (solidSprite == null)
        {
            solidSprite = MinigameUiKit.CreateSolidSprite(Color.white);
        }

        if (softSprite == null)
        {
            softSprite = MinigameUiKit.CreateRoundedRectSprite(128, 128, 20, Color.white, new Color(1f, 1f, 1f, 0.12f));
        }

        if (circleSprite == null)
        {
            circleSprite = MinigameUiKit.CreateCircleSprite(32, Color.white, Color.clear, 1);
        }
    }

    private void CreateBackdrop()
    {
        if (transform.Find(BackdropName) != null)
        {
            return;
        }

        Transform overlay = transform.Find("BackgroundOverlay");
        GameObject root = MinigameUiKit.CreateUIObject(BackdropName, transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        MinigameUiKit.Stretch(rootRect);
        if (overlay != null)
        {
            root.transform.SetSiblingIndex(overlay.GetSiblingIndex() + 1);
        }
        else
        {
            root.transform.SetAsFirstSibling();
        }

        Image surface = MinigameUiKit.CreateImage(root.transform, "Workbench_Surface", softSprite, SurfaceColor(), false);
        RectTransform surfaceRect = surface.rectTransform;
        surfaceRect.anchorMin = new Vector2(-0.05f, 0f);
        surfaceRect.anchorMax = new Vector2(1.05f, 0f);
        surfaceRect.pivot = new Vector2(0.5f, 0f);
        surfaceRect.anchoredPosition = new Vector2(0f, -52f);
        surfaceRect.sizeDelta = new Vector2(0f, 500f);
        surfaceRect.localRotation = Quaternion.Euler(0f, 0f, -1.35f);

        Image rim = MinigameUiKit.CreateImage(root.transform, "Workbench_AccentRim", solidSprite, WithAlpha(accentColor, 0.22f), false);
        MinigameUiKit.SetAnchored(rim.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 366f), new Vector2(0f, 3f));

        Image leftGlow = MinigameUiKit.CreateImage(root.transform, "Workbench_LeftGlow", softSprite, WithAlpha(accentColor, 0.075f), false);
        MinigameUiKit.SetAnchored(leftGlow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(54f, -12f), new Vector2(108f, 740f));

        Image rightGlow = MinigameUiKit.CreateImage(root.transform, "Workbench_RightGlow", softSprite, WithAlpha(accentColor, 0.055f), false);
        MinigameUiKit.SetAnchored(rightGlow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-54f, -12f), new Vector2(108f, 740f));

        CreateGrid(root.transform);
        CreateParticles(root.transform);
        CreateSpecularSweep(root.transform);
    }

    private void CreateGrid(Transform parent)
    {
        Color gridColor = WithAlpha(Color.Lerp(accentColor, Color.white, 0.35f), 0.055f);
        for (int i = 0; i < 10; i++)
        {
            float y = 100f + i * 38f;
            Image line = MinigameUiKit.CreateImage(parent, "Workbench_GridH_" + i, solidSprite, gridColor, false);
            MinigameUiKit.SetAnchored(line.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, y), new Vector2(0f, 1.5f));
        }

        for (int i = 0; i < 14; i++)
        {
            float x = -820f + i * 126f;
            Image line = MinigameUiKit.CreateImage(parent, "Workbench_GridV_" + i, solidSprite, gridColor, false);
            MinigameUiKit.SetAnchored(line.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 266f), new Vector2(1.5f, 410f));
        }
    }

    private void CreateParticles(Transform parent)
    {
        for (int i = 0; i < 24; i++)
        {
            Image particle = MinigameUiKit.CreateImage(parent, "Workbench_Particle_" + i, circleSprite, WithAlpha(ParticleColor(i), 0.14f), false);
            RectTransform rect = particle.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * (3f + (i % 4));
            rect.anchoredPosition = ParticlePosition(i);
        }
    }

    private void CreateSpecularSweep(Transform parent)
    {
        Image sweep = MinigameUiKit.CreateImage(parent, "Workbench_SpecularSweep", solidSprite, WithAlpha(Color.white, 0.045f), false);
        RectTransform rect = sweep.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(96f, 1250f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -24f);
    }

private void DecorateExistingUi()
    {
        RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null || rect.transform == transform || IsWorkbenchObject(rect))
            {
                continue;
            }

            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            string objectName = rect.name;
            Vector2 size = rect.rect.size;
            bool isLargePanel = size.x >= 300f && size.y >= 80f && IsPanelLike(objectName);
            bool isTool = IsToolLike(objectName);
            bool isInteractiveMark = IsInteractiveMark(objectName, size);

            if (isLargePanel)
            {
                AddInnerGlow(rect, image, 0.045f);
            }

            if (isTool)
            {
                AddInnerGlow(rect, image, 0.055f);
            }

            if (isInteractiveMark)
            {
                AddPulseGlow(rect, size);
            }
        }
    }

    private void AddInnerGlow(RectTransform target, Image image, float alpha)
    {
        if (target.Find("Workbench_InnerGlow") != null)
        {
            return;
        }

        Image glow = MinigameUiKit.CreateImage(target, "Workbench_InnerGlow", image.sprite != null ? image.sprite : softSprite, WithAlpha(accentColor, alpha), false);
        glow.type = image.type;
        MinigameUiKit.Stretch(glow.rectTransform);
        glow.transform.SetAsFirstSibling();
    }

private void AddPulseGlow(RectTransform target, Vector2 size)
    {
        if (target.Find("Workbench_TargetGlow") != null)
        {
            return;
        }

        Sprite sprite = size.x <= 90f && size.y <= 90f ? circleSprite : softSprite;
        Image glow = MinigameUiKit.CreateImage(target, "Workbench_TargetGlow", sprite, WithAlpha(accentColor, 0.14f), false);
        RectTransform rect = glow.rectTransform;
        MinigameUiKit.Stretch(rect);
        rect.offsetMin = new Vector2(-6f, -6f);
        rect.offsetMax = new Vector2(6f, 6f);
        glow.transform.SetAsFirstSibling();
    }

private bool IsWorkbenchObject(RectTransform rect)
    {
        return rect.name.StartsWith("Workbench_");
    }

    private static bool IsPanelLike(string name)
    {
        return name.Contains("Panel") || name.Contains("Board") || name.Contains("Header") || name.Contains("Device") || name.Contains("Meter") || name.Contains("Body");
    }

private static bool IsToolLike(string name)
    {
        if (name.Contains("ProbeMarker"))
        {
            return false;
        }

        return name.Contains("Tool_") || name.Contains("Probe") || name.Contains("Brush") || name.Contains("Pump") || name.Contains("Iron") || name.Contains("Tweezers") || name.Contains("WirePlug") || name.Contains("MultimeterSprite") || name.Contains("RedProbe") || name.Contains("BlackProbe");
    }

    private static bool IsInteractiveMark(string name, Vector2 size)
    {
        if (size.x > 260f || size.y > 260f)
        {
            return false;
        }

        return name.StartsWith("TP_") || name.StartsWith("CMP_") || name.Contains("Terminal") || name.Contains("Contact") || name.Contains("Patch") || name.Contains("Pad") || name.Contains("Socket") || name.Contains("Component") || name.Contains("Pin");
    }

    private Color SurfaceColor()
    {
        switch (style)
        {
            case MinigameWorkbenchStyle.Cleaning:
                return new Color(0.05f, 0.045f, 0.032f, 0.68f);
            case MinigameWorkbenchStyle.Replacement:
                return new Color(0.052f, 0.035f, 0.03f, 0.7f);
            case MinigameWorkbenchStyle.Wiring:
                return new Color(0.028f, 0.044f, 0.052f, 0.68f);
            default:
                return new Color(0.028f, 0.04f, 0.048f, 0.68f);
        }
    }

    private Color ParticleColor(int index)
    {
        if (index % 5 == 0)
        {
            return Color.white;
        }

        return Color.Lerp(accentColor, new Color(1f, 0.78f, 0.35f, 1f), index % 3 == 0 ? 0.35f : 0.08f);
    }

    private static Vector2 ParticlePosition(int index)
    {
        float x = -820f + (index * 137f) % 1640f;
        float y = -335f + (index * 83f) % 720f;
        return new Vector2(x, y);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}


