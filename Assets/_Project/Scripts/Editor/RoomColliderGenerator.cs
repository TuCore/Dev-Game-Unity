using UnityEngine;
using UnityEditor;

public class RoomColliderGenerator : Editor
{
    [MenuItem("GameObject/Tạo Tường Tàng Hình Tự Động", false, 10)]
    public static void GenerateColliders()
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            Debug.LogError("Vui lòng chọn Bedroom_Shell trước khi chạy lệnh!");
            return;
        }

        Renderer rend = selectedObj.GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("GameObject này không có MeshRenderer. Không thể tính toán kích thước tự động!");
            return;
        }

        Bounds bounds = rend.bounds;

        Transform group = selectedObj.transform.Find("Colliders_Group");
        if (group != null)
        {
            DestroyImmediate(group.gameObject);
        }

        GameObject groupObj = new GameObject("Colliders_Group");
        groupObj.transform.SetParent(selectedObj.transform);
        groupObj.transform.position = Vector3.zero;

        float thickness = 0.5f;

        CreateBoxCollider(groupObj.transform, "Floor_Collider", 
            new Vector3(bounds.center.x, bounds.min.y - thickness/2f, bounds.center.z),
            new Vector3(bounds.size.x, thickness, bounds.size.z));

        CreateBoxCollider(groupObj.transform, "Wall_Left", 
            new Vector3(bounds.min.x - thickness/2f, bounds.center.y, bounds.center.z),
            new Vector3(thickness, bounds.size.y, bounds.size.z));

        CreateBoxCollider(groupObj.transform, "Wall_Right", 
            new Vector3(bounds.max.x + thickness/2f, bounds.center.y, bounds.center.z),
            new Vector3(thickness, bounds.size.y, bounds.size.z));

        CreateBoxCollider(groupObj.transform, "Wall_Front", 
            new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + thickness/2f),
            new Vector3(bounds.size.x, bounds.size.y, thickness));

        CreateBoxCollider(groupObj.transform, "Wall_Back", 
            new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - thickness/2f),
            new Vector3(bounds.size.x, bounds.size.y, thickness));

        Debug.Log("Đã tạo Tường tàng hình thành công cho " + selectedObj.name);
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
