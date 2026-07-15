using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject định nghĩa kiểu mẫu NPC khách hàng.
/// Có field riêng cho lời thoại đặc trưng, không dùng template chung (GDD mục 2.4).
/// </summary>
public enum CustomerPersonality
{
    Easygoing, // Dễ tính
    Strict     // Khó tính
}

[CreateAssetMenu(fileName = "New NPC Archetype", menuName = "Anh Tho Dien/NPC Archetype")]
public class NPCArchetype : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string archetypeName;          // VD: "Bà chủ trọ", "Sinh viên", "Khách đại gia"
    [TextArea(2, 4)]
    public string description;

    [Header("Yêu cầu mở khóa")]
    public int requiredReputation = 0;    // Cần bao nhiêu danh tiếng để khách loại này xuất hiện

    [Header("Đặc trưng giao tiếp")]
    [TextArea(1, 3)]
    public List<string> greetingDialogues;    // Lời chào đặc trưng
    public List<AudioClip> greetingAudioClips;

    [TextArea(1, 3)]
    public List<string> satisfiedDialogues;   // Khi hài lòng
    public List<AudioClip> satisfiedAudioClips;

    [TextArea(1, 3)]
    public List<string> unsatisfiedDialogues; // Khi không hài lòng
    public List<AudioClip> unsatisfiedAudioClips;

    [TextArea(1, 3)]
    public List<string> leavingDialogues;     // Khi bỏ đi (hết deadline)
    public List<AudioClip> leavingAudioClips;

    [Header("Hành vi đặc biệt")]
    public CustomerPersonality personality = CustomerPersonality.Strict;
    public bool canNegotiatePrice = false;    // VD: Sinh viên xin giảm giá
    public float tipMultiplier = 1f;          // Khách đại gia tip nhiều hơn
    public float patienceMultiplier = 1f;     // Hệ số kiên nhẫn (deadline dài/ngắn hơn)

    [Header("Đồ vật thường mang đến sửa")]
    public List<string> preferredItems;       // VD: ["Quạt bàn", "Nồi cơm điện"]

    public string GetRandomGreeting(out AudioClip clip)
    {
        return GetRandomDialogueAndClip(greetingDialogues, greetingAudioClips, "Tôi có món đồ bị hỏng, sửa giúp tôi nhé!", out clip);
    }

    public string GetRandomSatisfied(out AudioClip clip)
    {
        return GetRandomDialogueAndClip(satisfiedDialogues, satisfiedAudioClips, "Cảm ơn cậu nhé, đồ sửa tốt lắm!", out clip);
    }

    public string GetRandomUnsatisfied(out AudioClip clip)
    {
        return GetRandomDialogueAndClip(unsatisfiedDialogues, unsatisfiedAudioClips, "Làm ăn chậm chạp quá, tôi lấy lại đồ!", out clip);
    }

    public string GetRandomLeaving(out AudioClip clip)
    {
        return GetRandomDialogueAndClip(leavingDialogues, leavingAudioClips, "Thế thì thôi vậy, tôi mang ra tiệm khác!", out clip);
    }

    private string GetRandomDialogueAndClip(List<string> dialogues, List<AudioClip> clips, string fallbackText, out AudioClip clip)
    {
        clip = null;
        if (dialogues == null || dialogues.Count == 0) return fallbackText;
        int idx = Random.Range(0, dialogues.Count);
        string text = dialogues[idx];
        if (clips != null && idx < clips.Count && clips[idx] != null)
        {
            clip = clips[idx];
        }
        else
        {
            clip = FindClipByDialogueText(text);
        }
        return text;
    }

    private AudioClip FindClipByDialogueText(string dialogueText)
    {
        if (string.IsNullOrWhiteSpace(dialogueText)) return null;
        string cleanText = NormalizeString(dialogueText);

        AudioClip[] allClips = Resources.LoadAll<AudioClip>("Audio");
        foreach (var c in allClips)
        {
            if (c == null) continue;
            string cleanClipName = NormalizeString(c.name);
            if (cleanClipName == cleanText || cleanClipName.Contains(cleanText) || cleanText.Contains(cleanClipName) || IsPrefixMatch(cleanText, cleanClipName))
            {
                return c;
            }
        }
        return null;
    }

    private bool IsPrefixMatch(string a, string b)
    {
        if (a.Length < 10 || b.Length < 10) return false;
        int checkLen = Mathf.Min(25, Mathf.Min(a.Length, b.Length));
        return a.Substring(0, checkLen) == b.Substring(0, checkLen);
    }

    private string NormalizeString(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        char[] chars = s.ToCharArray();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in chars)
        {
            if (!char.IsPunctuation(c) && c != '\r' && c != '\n')
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }
        return sb.ToString().Replace(" ", "");
    }
}
