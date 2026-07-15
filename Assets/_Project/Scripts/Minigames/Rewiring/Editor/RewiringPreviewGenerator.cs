#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Công cụ Editor hỗ trợ tạo nhanh mô hình xem trước (Preview) cho Cầu Vượt (Bridge) 
/// và Chướng Ngại Vật (Obstacle) ngay trong Scene View của Unity Editor.
/// </summary>
public class RewiringPreviewGenerator
{
    [MenuItem("Tools/Rewiring/Spawn Bridge & Obstacle Previews in Scene")]
    public static void SpawnPreviewsInScene()
    {
        // 1. Tìm hoặc tạo thư mục chứa Preview trong Hierarchy
        GameObject root = GameObject.Find("--- REWIRING PREVIEWS ---");
        if (root == null)
        {
            root = new GameObject("--- REWIRING PREVIEWS ---");
        }

        // Xóa các preview cũ nếu có để tránh trùng lặp
        while (root.transform.childCount > 0)
        {
            Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
        }

        // Kích thước ô lưới tiêu chuẩn
        float cellW = 1.0f;
        float cellH = 1.0f;

        // 2. Tạo mô hình Cầu Vượt (Wire Bridge Preview)
        GameObject bridgeObj = new GameObject("Preview_Bridge_Cell (Ceramic Insulator)");
        bridgeObj.transform.SetParent(root.transform, false);
        bridgeObj.transform.position = new Vector3(-1.2f, 0, 0); // Đặt bên trái Scene View
        
        RewiringBridge bridgeComp = bridgeObj.AddComponent<RewiringBridge>();
        bridgeComp.Initialize(new Vector2Int(0, 0));
        bridgeComp.CreateVisualModel(cellW, cellH);

        // 3. Tạo mô hình Chướng Ngại Vật - Tụ Điện Cháy (Burnt Capacitor Preview)
        GameObject obsCapObj = new GameObject("Preview_Obstacle_Cell (Burnt Capacitor)");
        obsCapObj.transform.SetParent(root.transform, false);
        obsCapObj.transform.position = new Vector3(0.3f, 0, 0); // Đặt giữa Scene View
        
        RewiringObstacle obsCapComp = obsCapObj.AddComponent<RewiringObstacle>();
        obsCapComp.Initialize(new Vector2Int(1, 0), RewiringObstacle.ObstacleType.BurntCapacitor);
        obsCapComp.CreateVisualModel(cellW, cellH);

        // Chọn vào Root object để người dùng thấy ngay trong Hierarchy và Scene View
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
            SceneView.lastActiveSceneView.Repaint();
        }

        Debug.Log("[RewiringPreviewGenerator] Đã tạo thành công mô hình xem trước (Cầu vượt & Chướng ngại vật) trong Scene View!");
    }
}
#endif
