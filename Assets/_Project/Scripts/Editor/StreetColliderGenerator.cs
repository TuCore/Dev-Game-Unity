using UnityEngine;
using UnityEditor;

public class StreetColliderGenerator : Editor
{
    [MenuItem("GameObject/Tạo Tường Tàng Hình Cho Phố (Street)", false, 11)]
    public static void GenerateColliders()
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            Debug.LogError("Vui lòng chọn GameObject (ví dụ VietnamStreetV2) trước khi chạy lệnh!");
            return;
        }

        // Lấy tất cả MeshRenderer của object cha và các object con
        Renderer[] renderers = selectedObj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("GameObject này không có MeshRenderer nào. Không thể tính toán kích thước tự động!");
            return;
        }

        // Tính toán hộp bao quanh (Bounds) tổng cộng của toàn bộ khu phố
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Transform group = selectedObj.transform.Find("Colliders_Group");
        if (group != null)
        {
            DestroyImmediate(group.gameObject);
        }

        GameObject groupObj = new GameObject("Colliders_Group");
        groupObj.transform.SetParent(selectedObj.transform);
        groupObj.transform.position = Vector3.zero;

        float thickness = 1f;
        float heightOffset = 10f; // Cho tường cao thêm 10 đơn vị để tránh player nhảy hoặc văng ra ngoài

        // Sàn (Floor)
        CreateBoxCollider(groupObj.transform, "Floor_Collider", 
            new Vector3(bounds.center.x, bounds.min.y - thickness/2f, bounds.center.z),
            new Vector3(bounds.size.x, thickness, bounds.size.z));

        // Tường trái (Left)
        CreateBoxCollider(groupObj.transform, "Wall_Left", 
            new Vector3(bounds.min.x - thickness/2f, bounds.center.y, bounds.center.z),
            new Vector3(thickness, bounds.size.y + heightOffset, bounds.size.z));

        // Tường phải (Right)
        CreateBoxCollider(groupObj.transform, "Wall_Right", 
            new Vector3(bounds.max.x + thickness/2f, bounds.center.y, bounds.center.z),
            new Vector3(thickness, bounds.size.y + heightOffset, bounds.size.z));

        // Tường trước (Front)
        CreateBoxCollider(groupObj.transform, "Wall_Front", 
            new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + thickness/2f),
            new Vector3(bounds.size.x, bounds.size.y + heightOffset, thickness));

        // Tường sau (Back)
        CreateBoxCollider(groupObj.transform, "Wall_Back", 
            new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - thickness/2f),
            new Vector3(bounds.size.x, bounds.size.y + heightOffset, thickness));

        Debug.Log("Đã tạo Tường tàng hình thành công bao quanh " + selectedObj.name);
    }

    private static void CreateBoxCollider(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = position;

        BoxCollider col = obj.AddComponent<BoxCollider>();
        col.size = size;
    }
}
