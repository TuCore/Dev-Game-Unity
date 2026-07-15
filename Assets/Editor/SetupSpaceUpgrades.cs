using UnityEngine;
using UnityEditor;
using System.Linq;

public class SetupSpaceUpgrades : EditorWindow
{
    [MenuItem("Tools/5. Tự động gắn Model Nâng cấp (AC & Speaker)")]
    public static void AutoSetupUpgrades()
    {
        // 1. Tìm hoặc tạo SpaceUpgradeSystem
        SpaceUpgradeSystem spaceSystem = FindFirstObjectByType<SpaceUpgradeSystem>();
        if (spaceSystem == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm == null) gm = new GameObject("GameManager");
            spaceSystem = gm.AddComponent<SpaceUpgradeSystem>();
        }

        // 1.5 Xóa các model cũ (Cubes hoặc model cũ đã tạo trước đó)
        GameObject oldAc = GameObject.Find("AirConditioner_Model");
        if (oldAc != null) DestroyImmediate(oldAc);

        GameObject oldSpeaker = GameObject.Find("Speaker_Model");
        if (oldSpeaker != null) DestroyImmediate(oldSpeaker);

        spaceSystem.airConditionerModel = null;
        spaceSystem.speakerModel = null;

        // 2. Load các model 3D từ thư mục AC_Speaker
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new string[] { "Assets/Models/AC_Speaker" });
        GameObject acPrefab = null;
        GameObject speakerPrefab = null;

        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("air") || path.ToLower().Contains("conditioner"))
            {
                acPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            else if (path.ToLower().Contains("speaker"))
            {
                speakerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        // 3. Kéo vào Scene
        if (spaceSystem.airConditionerModel == null)
        {
            GameObject acObj;
            if (acPrefab != null) 
            {
                acObj = (GameObject)PrefabUtility.InstantiatePrefab(acPrefab);
            }
            else 
            {
                acObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                acObj.transform.localScale = new Vector3(2f, 0.5f, 0.5f);
            }
            
            acObj.name = "AirConditioner_Model";
            acObj.transform.position = new Vector3(0, 3f, 2f); // Chỉnh tọa độ tạm
            acObj.SetActive(false); // Ẩn đi chờ mua
            spaceSystem.airConditionerModel = acObj;
        }

        if (spaceSystem.speakerModel == null)
        {
            GameObject speakerObj;
            if (speakerPrefab != null)
            {
                speakerObj = (GameObject)PrefabUtility.InstantiatePrefab(speakerPrefab);
            }
            else
            {
                speakerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                speakerObj.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            }
            
            speakerObj.name = "Speaker_Model";
            speakerObj.transform.position = new Vector3(-2f, 0f, 2f); // Chỉnh tọa độ tạm
            speakerObj.SetActive(false); // Ẩn đi chờ mua
            spaceSystem.speakerModel = speakerObj;
        }

        EditorUtility.SetDirty(spaceSystem);
        Debug.Log("==== ĐÃ TỰ ĐỘNG GẮN MODEL VÀO SPACE UPGRADE SYSTEM ====");
    }
}
