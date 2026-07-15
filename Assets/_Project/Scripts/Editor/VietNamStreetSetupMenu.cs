using UnityEngine;
using UnityEditor;

public class VietNamStreetSetupMenu
{
    [MenuItem("AnhThoDien/Tạo cửa chuyển cảnh (Vào Shop_Main)")]
    public static void CreateDoorToShop()
    {
        // Kiểm tra xem đã có cửa chưa để tránh tạo trùng
        if (GameObject.Find("Door_To_Shop") != null)
        {
            Debug.LogWarning("[AnhThoDien] Đã có Door_To_Shop trong Scene rồi!");
            return;
        }

        GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObj.name = "Door_To_Shop";
        
        // Đặt vị trí gần chỗ electrical store (dựa theo tọa độ bạn cung cấp)
        doorObj.transform.position = new Vector3(119.4f, 3f, 11.4f); 
        doorObj.transform.localScale = new Vector3(2f, 3f, 1f);
        
        // Làm tàng hình
        doorObj.GetComponent<MeshRenderer>().enabled = false; 
        
        // Cài đặt Collider là Trigger
        Collider doorCol = doorObj.GetComponent<Collider>();
        if (doorCol != null) doorCol.isTrigger = true;

        // Gắn script SceneTransitionDoor
        SceneTransitionDoor transition = doorObj.AddComponent<SceneTransitionDoor>();
        UnityEditor.SerializedObject serializedTransition = new UnityEditor.SerializedObject(transition);
        serializedTransition.FindProperty("targetSceneName").stringValue = "Shop_Main";
        serializedTransition.FindProperty("interactionPrompt").stringValue = "Vào Tiệm Điện";
        serializedTransition.ApplyModifiedProperties();

        // Focus vào cánh cửa vừa tạo để người dùng dễ nhìn thấy và di chuyển
        Selection.activeGameObject = doorObj;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("[AnhThoDien] Đã tạo thành công Door_To_Shop! Bạn có thể dùng phím W (công cụ di chuyển) để dịch chuyển cánh cửa cho khớp lối vào nhé.");
    }
}
