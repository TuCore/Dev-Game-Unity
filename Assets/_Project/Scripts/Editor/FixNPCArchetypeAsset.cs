using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FixNPCArchetypeAsset
{
    static FixNPCArchetypeAsset()
    {
        EditorApplication.delayCall += () =>
        {
            NormalizeAllNPCArchetypes();
        };
    }

    [MenuItem("Tools/Chuẩn Hóa NPCArchetype Assets (Cách 1)")]
    public static void NormalizeAllNPCArchetypes()
    {
        string[] guids = AssetDatabase.FindAssets("t:NPCArchetype");
        int count = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<NPCArchetype>(path);
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
                count++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[FixNPCArchetypeAsset] Đã chuẩn hóa lại định dạng YAML cho {count} file NPCArchetype asset (Bao gồm KhachHao.asset)!");
    }
}
