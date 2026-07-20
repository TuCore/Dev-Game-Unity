using UnityEditor;
using UnityEngine;

public class AudioDatabaseBuilder
{
    [MenuItem("Tools/Audio/Rebuild Audio Database")]
    public static void Build()
    {
        string resourcesPath = "Assets/_Project/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Resources");
        }

        string assetPath = resourcesPath + "/AudioDatabase.asset";
        AudioDatabase db = AssetDatabase.LoadAssetAtPath<AudioDatabase>(assetPath);
        
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<AudioDatabase>();
            AssetDatabase.CreateAsset(db, assetPath);
        }

        db.mappings.Clear();
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                // Sử dụng clip.name (tên asset trong Unity) để map, bỏ qua lỗi filename bị lỗi font tiếng Việt
                db.mappings.Add(new AudioDatabase.AudioMapping { key = clip.name, clip = clip });
            }
        }
        
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("[AudioDatabaseBuilder] Đã tự động đồng bộ " + db.mappings.Count + " audio clips vào AudioDatabase.");
    }
}
