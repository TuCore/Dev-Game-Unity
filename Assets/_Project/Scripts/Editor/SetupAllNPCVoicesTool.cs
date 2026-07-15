using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public static class SetupAllNPCVoicesTool
{
    static SetupAllNPCVoicesTool()
    {
        EditorApplication.delayCall += () =>
        {
            AssignAllAudioClipsToArchetypes();
        };
    }

    [MenuItem("Tools/Tự Động Gán Giọng Đọc Cho Tất Cả NPC")]
    public static void AssignAllAudioClipsToArchetypes()
    {
        // 1. Refresh AssetDatabase để chắc chắn toàn bộ file mp3 trong Resources/Audio/Voice đã được Unity import thành AudioClip
        AssetDatabase.Refresh();

        // 2. Load tất cả các AudioClip trong thư mục Resources/Audio
        AudioClip[] allClips = Resources.LoadAll<AudioClip>("Audio");
        if (allClips == null || allClips.Length == 0)
        {
            Debug.LogWarning("[SetupAllNPCVoicesTool] Không tìm thấy AudioClip nào trong Resources/Audio!");
            return;
        }

        // 3. Tìm toàn bộ các file NPCArchetype (.asset)
        string[] guids = AssetDatabase.FindAssets("t:NPCArchetype");
        int totalUpdatedNPCs = 0;
        int totalAssignedClips = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var archetype = AssetDatabase.LoadAssetAtPath<NPCArchetype>(path);
            if (archetype == null) continue;

            bool updated = false;

            // Xử lý Greeting
            if (archetype.greetingDialogues != null)
            {
                if (archetype.greetingAudioClips == null) archetype.greetingAudioClips = new List<AudioClip>();
                while (archetype.greetingAudioClips.Count < archetype.greetingDialogues.Count) archetype.greetingAudioClips.Add(null);

                for (int i = 0; i < archetype.greetingDialogues.Count; i++)
                {
                    AudioClip clip = FindMatchingClip(archetype.greetingDialogues[i], allClips, archetype.name, 0);
                    if (clip != null && archetype.greetingAudioClips[i] != clip)
                    {
                        archetype.greetingAudioClips[i] = clip;
                        updated = true;
                        totalAssignedClips++;
                    }
                }
            }

            // Xử lý Satisfied
            if (archetype.satisfiedDialogues != null)
            {
                if (archetype.satisfiedAudioClips == null) archetype.satisfiedAudioClips = new List<AudioClip>();
                while (archetype.satisfiedAudioClips.Count < archetype.satisfiedDialogues.Count) archetype.satisfiedAudioClips.Add(null);

                for (int i = 0; i < archetype.satisfiedDialogues.Count; i++)
                {
                    AudioClip clip = FindMatchingClip(archetype.satisfiedDialogues[i], allClips, archetype.name, 1);
                    if (clip != null && archetype.satisfiedAudioClips[i] != clip)
                    {
                        archetype.satisfiedAudioClips[i] = clip;
                        updated = true;
                        totalAssignedClips++;
                    }
                }
            }

            // Xử lý Unsatisfied
            if (archetype.unsatisfiedDialogues != null)
            {
                if (archetype.unsatisfiedAudioClips == null) archetype.unsatisfiedAudioClips = new List<AudioClip>();
                while (archetype.unsatisfiedAudioClips.Count < archetype.unsatisfiedDialogues.Count) archetype.unsatisfiedAudioClips.Add(null);

                for (int i = 0; i < archetype.unsatisfiedDialogues.Count; i++)
                {
                    AudioClip clip = FindMatchingClip(archetype.unsatisfiedDialogues[i], allClips, archetype.name, 2);
                    if (clip != null && archetype.unsatisfiedAudioClips[i] != clip)
                    {
                        archetype.unsatisfiedAudioClips[i] = clip;
                        updated = true;
                        totalAssignedClips++;
                    }
                }
            }

            // Xử lý Leaving
            if (archetype.leavingDialogues != null)
            {
                if (archetype.leavingAudioClips == null) archetype.leavingAudioClips = new List<AudioClip>();
                while (archetype.leavingAudioClips.Count < archetype.leavingDialogues.Count) archetype.leavingAudioClips.Add(null);

                for (int i = 0; i < archetype.leavingDialogues.Count; i++)
                {
                    AudioClip clip = FindMatchingClip(archetype.leavingDialogues[i], allClips, archetype.name, 3);
                    if (clip != null && archetype.leavingAudioClips[i] != clip)
                    {
                        archetype.leavingAudioClips[i] = clip;
                        updated = true;
                        totalAssignedClips++;
                    }
                }
            }

            if (updated)
            {
                EditorUtility.SetDirty(archetype);
                totalUpdatedNPCs++;
            }
        }

        if (totalUpdatedNPCs > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupAllNPCVoicesTool] 🎉 Đã tự động gán thành công {totalAssignedClips} clip giọng đọc vào Inspector cho {totalUpdatedNPCs} file NPCArchetype!");
        }
        else
        {
            Debug.Log("[SetupAllNPCVoicesTool] Tất cả các NPC đã được kiểm tra và chuẩn hóa định dạng Audio Clips.");
        }
    }

    private static AudioClip FindMatchingClip(string dialogueText, AudioClip[] allClips, string archetypeName, int categoryIdx)
    {
        if (string.IsNullOrWhiteSpace(dialogueText)) return null;
        string cleanText = NormalizeString(dialogueText);

        // 0. Ưu tiên tìm trong thư mục riêng của NPC đó (VD: Resources/Audio/Voice/KhachNPC02)
        if (!string.IsNullOrEmpty(archetypeName))
        {
            AudioClip[] npcSpecificClips = Resources.LoadAll<AudioClip>("Audio/Voice/" + archetypeName);
            if (npcSpecificClips != null && npcSpecificClips.Length > 0)
            {
                foreach (var c in npcSpecificClips)
                {
                    if (c == null) continue;
                    string cleanClipName = NormalizeString(c.name);
                    if (cleanClipName == cleanText || cleanClipName.Contains(cleanText) || cleanText.Contains(cleanClipName) || IsPrefixMatch(cleanText, cleanClipName))
                    {
                        return c;
                    }
                }

                // Khớp thông minh từ khóa (VD: "Sửa máy giúp em" vs "Sửa giúp em cái máy")
                AudioClip bestClip = null;
                int maxScore = 0;
                foreach (var c in npcSpecificClips)
                {
                    if (c == null) continue;
                    int score = CountSharedWords(dialogueText, c.name);
                    if (score > maxScore && score >= 3)
                    {
                        maxScore = score;
                        bestClip = c;
                    }
                }
                if (bestClip != null) return bestClip;

                // Khớp theo chỉ số loại hội thoại nếu thư mục NPC đó có đúng 4 file chuẩn (Greeting=0, Satisfied=1, Unsatisfied=2, Leaving=3)
                foreach (var c in npcSpecificClips)
                {
                    if (c == null) continue;
                    string lower = c.name.ToLower();
                    if (categoryIdx == 0 && (lower.Contains("sửa") || lower.Contains("xem") || lower.Contains("bắt") || lower.Contains("tháo") || lower.Contains("ném"))) return c;
                    if (categoryIdx == 1 && (lower.Contains("uy tín") || lower.Contains("êm ru") || lower.Contains("ngon lành") || lower.Contains("tuyệt vời") || lower.Contains("mới luôn") || lower.Contains("xong rồi"))) return c;
                    if (categoryIdx == 2 && (lower.Contains("lề mề") || lower.Contains("chán quá") || lower.Contains("nản ghê") || lower.Contains("thợ với thuyền") || lower.Contains("lâu thế"))) return c;
                    if (categoryIdx == 3 && (lower.Contains("tiền công") || lower.Contains("báo giá") || lower.Contains("chảnh") || lower.Contains("sinh viên") || lower.Contains("lặt vặt"))) return c;
                }
            }
        }

        // 1. Tìm trong toàn bộ danh sách clip
        foreach (var c in allClips)
        {
            if (c == null) continue;
            string cleanClipName = NormalizeString(c.name);
            if (cleanClipName == cleanText || cleanClipName.Contains(cleanText) || cleanText.Contains(cleanClipName) || IsPrefixMatch(cleanText, cleanClipName))
            {
                return c;
            }
        }

        // 2. Khớp từ khóa chung
        AudioClip bestGlobalClip = null;
        int maxGlobalScore = 0;
        foreach (var c in allClips)
        {
            if (c == null) continue;
            int score = CountSharedWords(dialogueText, c.name);
            if (score > maxGlobalScore && score >= 4)
            {
                maxGlobalScore = score;
                bestGlobalClip = c;
            }
        }
        return bestGlobalClip;
    }

    private static int CountSharedWords(string textA, string textB)
    {
        if (string.IsNullOrEmpty(textA) || string.IsNullOrEmpty(textB)) return 0;
        char[] sep = new char[] { ' ', '.', ',', '!', '?', '-', '\r', '\n', ';' };
        string[] wordsA = textA.ToLower().Split(sep, System.StringSplitOptions.RemoveEmptyEntries);
        string[] wordsB = textB.ToLower().Split(sep, System.StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> setB = new HashSet<string>(wordsB);
        int count = 0;
        foreach (var w in wordsA)
        {
            if (w.Length >= 2 && setB.Contains(w)) count++;
        }
        return count;
    }

    private static bool IsPrefixMatch(string a, string b)
    {
        if (a.Length < 10 || b.Length < 10) return false;
        int checkLen = Mathf.Min(25, Mathf.Min(a.Length, b.Length));
        return a.Substring(0, checkLen) == b.Substring(0, checkLen);
    }

    private static string NormalizeString(string s)
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
