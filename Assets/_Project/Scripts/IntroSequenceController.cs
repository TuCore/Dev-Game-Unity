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
    public string nextSceneName = "VietnamStreet";

    [Header("Dialogue Content")]
    public DialogueLine[] lines;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private AudioSource audioSource;
    private Coroutine typingCoroutine;

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
                // Force finish typing immediately
                StopCoroutine(typingCoroutine);
                dialogueText.text = lines[currentLineIndex].text;
                isTyping = false;
            }
            else
            {
                // Move to next line
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

        // Play Audio
        audioSource.Stop();
        if (line.voiceClip != null)
        {
            audioSource.PlayOneShot(line.voiceClip);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(line.text));
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        isTyping = true;
        
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
        
        isTyping = false;
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
