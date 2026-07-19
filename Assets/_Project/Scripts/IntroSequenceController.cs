using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public struct DialogueLine
{
    public string characterName;
    [TextArea(3, 10)]
    public string text;
    public AudioClip voiceClip;
}

[RequireComponent(typeof(AudioSource))]
public class IntroSequenceController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI promptText;

    [Header("Settings")]
    public float typeSpeed = 0.03f;
    public string nextSceneName = "Shop_Main";

    [Header("Voice Synchronization")]
    public bool syncTextToVoice = true;
    public bool autoAdvance = false;
    [Min(0f)] public float delayBetweenLines = 0.35f;

    [Header("Dialogue Content")]
    public DialogueLine[] lines;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private AudioSource audioSource;
    private Coroutine dialogueCoroutine;
    private bool revealCurrentLine;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (lines == null || lines.Length == 0)
        {
            EndSequence();
            return;
        }

        StartDialogueLine(currentLineIndex);
    }

    void Update()
    {
        // Skip entirely
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            EndSequence();
            return;
        }

        // Progress or speed up typing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // Reveal the current sentence immediately. The voice keeps playing,
                // and auto-advance still waits for it to finish.
                revealCurrentLine = true;
                dialogueText.text = lines[currentLineIndex].text;
                isTyping = false;
            }
            else
            {
                AdvanceLine();
            }
        }
    }

    void StartDialogueLine(int index)
    {
        DialogueLine line = lines[index];
        
        if (string.IsNullOrEmpty(line.characterName))
        {
            speakerNameText.text = "";
        }
        else
        {
            speakerNameText.text = line.characterName + ":";
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        audioSource.Stop();
        dialogueCoroutine = StartCoroutine(PlayDialogueLine(index));
    }

    IEnumerator PlayDialogueLine(int index)
    {
        DialogueLine line = lines[index];
        revealCurrentLine = false;

        if (line.voiceClip != null)
        {
            audioSource.clip = line.voiceClip;
            audioSource.Play();
        }

        if (syncTextToVoice && line.voiceClip != null && !string.IsNullOrEmpty(line.text))
        {
            yield return TypeSentenceSyncedToVoice(line.text, line.voiceClip.length);
        }
        else
        {
            yield return TypeSentence(line.text, typeSpeed);
        }

        if (!autoAdvance)
        {
            yield break;
        }

        if (line.voiceClip != null)
        {
            while (audioSource.isPlaying && currentLineIndex == index)
            {
                yield return null;
            }
        }

        if (delayBetweenLines > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBetweenLines);
        }

        if (currentLineIndex == index)
        {
            dialogueCoroutine = null;
            AdvanceLine();
        }
    }

    IEnumerator TypeSentenceSyncedToVoice(string sentence, float clipLength)
    {
        dialogueText.text = "";
        isTyping = true;
        int visibleCharacterCount = 0;

        while (audioSource.isPlaying && !revealCurrentLine)
        {
            float progress = clipLength > 0f
                ? Mathf.Clamp01(audioSource.time / clipLength)
                : 1f;
            int targetCharacterCount = Mathf.Clamp(
                Mathf.FloorToInt(progress * sentence.Length),
                0,
                sentence.Length);

            if (targetCharacterCount != visibleCharacterCount)
            {
                visibleCharacterCount = targetCharacterCount;
                dialogueText.text = sentence.Substring(0, visibleCharacterCount);
            }

            yield return null;
        }

        dialogueText.text = sentence;
        isTyping = false;
    }

    IEnumerator TypeSentence(string sentence, float secondsPerCharacter)
    {
        dialogueText.text = "";
        isTyping = true;

        foreach (char letter in sentence.ToCharArray())
        {
            if (revealCurrentLine)
            {
                break;
            }

            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(secondsPerCharacter);
        }

        dialogueText.text = sentence;
        isTyping = false;
    }

    void AdvanceLine()
    {
        currentLineIndex++;
        if (currentLineIndex < lines.Length)
        {
            StartDialogueLine(currentLineIndex);
        }
        else
        {
            EndSequence();
        }
    }

    void EndSequence()
    {
        // Load the next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next scene name is empty. Cannot transition.");
        }
    }
}
