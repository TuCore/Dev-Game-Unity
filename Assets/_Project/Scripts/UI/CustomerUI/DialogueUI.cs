using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class DialogueUI : MonoBehaviour
{
    private static DialogueUI _instance;
    public static DialogueUI Instance 
    { 
        get 
        { 
            if (_instance == null) 
            {
                // Tìm kiếm trong Scene, kể cả object bị ẩn (inactive), KHÔNG DÙNG Resources vì nó sẽ lôi nhầm Prefab ra
                _instance = FindObjectOfType<DialogueUI>(true);
            }
            return _instance; 
        } 
    }

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public Button continueButton;
    public Button secondaryButton; // Optional for choices like "Refuse" or "Negotiate"
    
    private Action onPrimaryAction;
    private Action onSecondaryAction;
    private Coroutine typingCoroutine;
    private float typingSpeed = 0.03f;
    private string fullText;
    private bool isTyping;

    private bool isDialogueActive = false;
    private CustomerController activeSpeaker;

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    private void Start()
    {
        if (dialoguePanel != null && !isDialogueActive) dialoguePanel.SetActive(false);
        
        if (continueButton != null) continueButton.onClick.AddListener(OnPrimaryClicked);
        if (secondaryButton != null) secondaryButton.onClick.AddListener(OnSecondaryClicked);
    }

    public void ShowDialogue(string npcName, string text, Action onPrimary = null, Action onSecondary = null, string primaryText = "Tiếp tục", string secondaryText = "", CustomerController speaker = null)
    {
        if (dialoguePanel == null) return;
        
        isDialogueActive = true;
        
        onPrimaryAction = onPrimary;
        onSecondaryAction = onSecondary;

        if (activeSpeaker != null && activeSpeaker != speaker)
        {
            activeSpeaker.SetDialoguePaused(false);
        }

        activeSpeaker = speaker;
        if (activeSpeaker != null)
        {
            activeSpeaker.SetDialoguePaused(true);
        }
        
        if (npcNameText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(npcName) ? "Khách hàng" : npcName;
            npcNameText.gameObject.SetActive(true);
            npcNameText.text = displayName;
            npcNameText.color = new Color(1f, 0.86f, 0.34f, 1f);
            npcNameText.fontStyle = FontStyles.Bold;
            npcNameText.alignment = TextAlignmentOptions.Left;
            npcNameText.textWrappingMode = TextWrappingModes.NoWrap;
            npcNameText.overflowMode = TextOverflowModes.Ellipsis;
            npcNameText.enableAutoSizing = true;
            npcNameText.fontSizeMin = 14f;
            npcNameText.fontSizeMax = Mathf.Max(npcNameText.fontSizeMax, 20f);
            npcNameText.margin = new Vector4(10f, 0f, 10f, 0f);
        }
        fullText = text;
        dialoguePanel.SetActive(true);
        
        if (continueButton != null)
        {
            var tmp = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = primaryText;
        }

        if (secondaryButton != null)
        {
            if (!string.IsNullOrEmpty(secondaryText))
            {
                secondaryButton.gameObject.SetActive(true);
                var tmp = secondaryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = secondaryText;
            }
            else
            {
                secondaryButton.gameObject.SetActive(false);
            }
        }

        
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText());
        
        // Block player movement/interaction while talking
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = false;

        PlayerCamera cam = FindAnyObjectByType<PlayerCamera>();
        if (cam != null) cam.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in fullText.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    private void OnPrimaryClicked()
    {
        if (isTyping)
        {
            // Skip typing and show full text
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = fullText;
            isTyping = false;
        }
        else
        {
            Action temp = onPrimaryAction;
            CloseDialogue();
            temp?.Invoke();
        }
    }

    private void OnSecondaryClicked()
    {
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = fullText;
            isTyping = false;
            return;
        }

        Action temp = onSecondaryAction;
        CloseDialogue();
        temp?.Invoke();
    }

    public void CloseDialogue()
    {
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (activeSpeaker != null)
        {
            activeSpeaker.SetDialoguePaused(false);
            activeSpeaker = null;
        }
        
        // Restore player movement
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) player.enabled = true;

        PlayerCamera cam = FindObjectOfType<PlayerCamera>();
        if (cam != null) cam.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
