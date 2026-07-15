using UnityEngine;
using UnityEditor;

public class PackShopPrefabEditor
{
    [MenuItem("Tools/6. Đóng gói Hệ thống Tiệm vào Prefab")]
    public static void PackToPrefab()
    {
        // 1. Tạo thư mục Prefabs nếu chưa có
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }

        // 2. Tìm các thành phần cần thiết
        GameObject gameManager = GameObject.Find("GameManager");
        GameObject acModel = GameObject.Find("AirConditioner_Model");
        GameObject speakerModel = GameObject.Find("Speaker_Model");
        
        if (gameManager == null)
        {
            Debug.LogError("Không tìm thấy GameManager trong Scene!");
            return;
        }

        // 3. Tạo một Object tổng để chứa
        GameObject root = new GameObject("RepairShop_System_Prefab");

        // 4. Di chuyển các object vào làm con của Object tổng
        gameManager.transform.SetParent(root.transform);
        if (acModel != null) acModel.transform.SetParent(root.transform);
        if (speakerModel != null) speakerModel.transform.SetParent(root.transform);

        // 5. Lưu thành Prefab
        string localPath = "Assets/_Project/Prefabs/RepairShop_System_Prefab.prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        bool prefabSuccess;
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, localPath, InteractionMode.UserAction, out prefabSuccess);

        if (prefabSuccess)
        {
            Debug.Log($"[Thành công] Đã đóng gói và lưu Prefab tại: {localPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(localPath));
        }
        else
        {
            Debug.LogError("Có lỗi khi lưu Prefab!");
        }
    }
}
