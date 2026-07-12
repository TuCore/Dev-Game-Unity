using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class GenerateMapBoundaries
{
    [MenuItem("Tools/Generate Map Boundaries (Invisible Walls)")]
    public static void Generate()
    {
        // Xóa viền cũ nếu có
        var oldBoundaries = GameObject.Find("MapBoundaries");
        if (oldBoundaries != null)
        {
            GameObject.DestroyImmediate(oldBoundaries);
        }

        // Tạo parent
        GameObject boundariesParent = new GameObject("MapBoundaries");

        // Hàm tạo tường tàng hình
        void CreateWall(string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(boundariesParent.transform);
            wall.transform.position = position;
            
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;

            // Đặt tag hoặc layer nếu cần (Mặc định Default là đủ chặn player)
            wall.layer = 0; 
        }

        // Tạo 4 bức tường xung quanh (Kích thước khổng lồ để chắn ngang đường)
        // Lưu ý: Tọa độ này là tương đối, bạn có thể dùng công cụ Move (W) trong Unity để kéo các bức tường này cho khớp với map thực tế!
        
        float mapSize = 100f; // Chiều dài tường
        float wallHeight = 50f; // Chiều cao tường để không nhảy qua được
        float wallThickness = 2f; // Độ dày tường

        // Tường phía Bắc (Z+)
        CreateWall("Wall_North", new Vector3(0, wallHeight / 2, 50f), new Vector3(mapSize, wallHeight, wallThickness));
        
        // Tường phía Nam (Z-)
        CreateWall("Wall_South", new Vector3(0, wallHeight / 2, -50f), new Vector3(mapSize, wallHeight, wallThickness));
        
        // Tường phía Đông (X+)
        CreateWall("Wall_East", new Vector3(50f, wallHeight / 2, 0), new Vector3(wallThickness, wallHeight, mapSize));
        
        // Tường phía Tây (X-)
        CreateWall("Wall_West", new Vector3(-50f, wallHeight / 2, 0), new Vector3(wallThickness, wallHeight, mapSize));
        
        // Tường lót sàn đáy vực (Phòng hờ rớt xuống thì đứng lại, hoặc gắn trigger báo game over)
        CreateWall("Wall_Bottom_KillPlane", new Vector3(0, -10f, 0), new Vector3(mapSize, 1f, mapSize));

        // Tự động focus vào MapBoundaries trong Hierarchy để dễ tìm
        Selection.activeGameObject = boundariesParent;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Đã tạo Tường Tàng Hình (Invisible Walls)! Hãy chọn các Wall_... trong Hierarchy và kéo chúng chặn đúng mép đường nhé.");
    }
}
