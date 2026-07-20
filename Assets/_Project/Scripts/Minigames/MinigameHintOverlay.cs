using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MinigameHintOverlay : MonoBehaviour
{
    private Func<string> hintProvider;
    private GameObject popupRoot;
    private TextMeshProUGUI bodyText;

    public static MinigameHintOverlay Attach(Transform root, Sprite panelSprite, Sprite solidSprite, string title, Func<string> provider, Color accentColor)
    {
        GameObject holder = MinigameUiKit.CreateUIObject("HintSystem", root);
        MinigameUiKit.Stretch(holder.GetComponent<RectTransform>());
        MinigameHintOverlay overlay = holder.AddComponent<MinigameHintOverlay>();
        overlay.Build(panelSprite, solidSprite, title, provider, accentColor);
        return overlay;
    }

    private void Build(Sprite panelSprite, Sprite solidSprite, string title, Func<string> provider, Color accentColor)
    {
        hintProvider = provider;

        popupRoot = MinigameUiKit.CreateUIObject("HintPopup", transform);
        MinigameUiKit.Stretch(popupRoot.GetComponent<RectTransform>());

        Image blocker = MinigameUiKit.CreateImage(popupRoot.transform, "Blocker", solidSprite, new Color(0f, 0f, 0f, 0.66f), true);
        MinigameUiKit.Stretch(blocker.rectTransform);
        Button blockerButton = blocker.gameObject.AddComponent<Button>();
        blockerButton.transition = Selectable.Transition.None;
        blockerButton.onClick.AddListener(Hide);

        Image panel = MinigameUiKit.CreatePanel(popupRoot.transform, "Panel", panelSprite, new Color(0.028f, 0.034f, 0.04f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 510f));
        MinigameUiKit.AddChrome(panel.transform, solidSprite, accentColor);

        TextMeshProUGUI titleText = MinigameUiKit.CreateText(panel.transform, "Title", title, 30, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.96f, 0.99f, 1f, 1f));
        MinigameUiKit.SetAnchored(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(410f, -50f), new Vector2(760f, 52f));

        Image bodyPanel = MinigameUiKit.CreatePanel(panel.transform, "BodyPanel", panelSprite, new Color(0.012f, 0.016f, 0.019f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(760f, 340f));
        bodyText = MinigameUiKit.CreateText(bodyPanel.transform, "Body", "", 21, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.96f, 0.95f, 1f));
        bodyText.margin = new Vector4(24f, 20f, 24f, 20f);
        bodyText.lineSpacing = 8f;
        MinigameUiKit.Stretch(bodyText.rectTransform);

        MinigameUiKit.CreateButton(panel.transform, "CloseButton", "ĐÃ HIỂU", panelSprite, new Color(0.13f, 0.42f, 0.3f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-134f, 48f), new Vector2(220f, 56f), Hide);
        popupRoot.SetActive(false);
    }

    public void Show()
    {
        if (popupRoot == null)
        {
            return;
        }

        if (bodyText != null)
        {
            string hint = hintProvider != null ? hintProvider.Invoke() : "Chua co goi y cho man nay.";
            bodyText.text = string.IsNullOrWhiteSpace(hint) ? "Chua co goi y cho man nay." : hint;
        }

        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();
        MinigameSfxKit.Play(MinigameSfxCue.Button, 0.32f);
    }

    private void Hide()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }
}