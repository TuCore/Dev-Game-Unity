using UnityEngine;
using UnityEditor;

public class SetupDoorEffect : EditorWindow
{
    [MenuItem("Tools/3. Auto Setup Rolling Door Effect")]
    public static void ApplyDoorEffect()
    {
        // Lấy object mà người dùng đang chọn
        GameObject shop = Selection.activeGameObject;
        if (shop == null)
        {
            Debug.LogError("Vui lòng click chọn một cánh cửa hoặc tòa nhà trong Scene trước khi chạy công cụ này!");
            return;
        }

        // Get Renderer
        MeshRenderer rend = shop.GetComponentInChildren<MeshRenderer>();
        if (rend == null)
        {
            Debug.LogError("Không tìm thấy MeshRenderer trên tiệm sửa xe!");
            return;
        }

        // Change Shader to RollingDoor
        Material mat = rend.sharedMaterial;
        Shader rollingShader = Shader.Find("Custom/RollingDoor");
        if (rollingShader != null && mat != null)
        {
            mat.shader = rollingShader;
            
            // Calculate door bounds based on the object's real bounding box
            Bounds bounds = rend.bounds;
            
            // Cửa chiếm khoảng 80% chiều rộng ở giữa, và 65% chiều cao tính từ dưới lên
            float xMin = bounds.min.x + (bounds.size.x * 0.1f);
            float xMax = bounds.max.x - (bounds.size.x * 0.1f);
            float yMin = bounds.min.y - 1f; // Trừ hao xuống dưới một chút
            float yMax = bounds.min.y + (bounds.size.y * 0.65f);

            mat.SetFloat("_DoorWorldXMin", xMin);
            mat.SetFloat("_DoorWorldXMax", xMax);
            mat.SetFloat("_DoorWorldYMin", yMin);
            mat.SetFloat("_DoorWorldYMax", yMax);
            mat.SetFloat("_DoorOpenAmount", 0f); // Khởi tạo đóng cửa

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"Đã cập nhật Shader! Bounds cửa: X({xMin:F1} -> {xMax:F1}), Y({yMin:F1} -> {yMax:F1})");
        }
        else
        {
            Debug.LogError("Không tìm thấy file shader 'Custom/RollingDoor' hoặc Material!");
        }

        // Đã xóa script cũ ShopDoorController nên không add nữa

        // Mark scene dirty to save changes
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shop.scene);
        
        Selection.activeGameObject = shop;
        Debug.Log("==== HOÀN TẤT THIẾT LẬP CỬA CUỐN TỰ ĐỘNG! ====");
    }
}
