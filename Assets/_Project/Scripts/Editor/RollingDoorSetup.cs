using UnityEngine;
using UnityEditor;

public class RollingDoorSetup : Editor
{
    [MenuItem("GameObject/Cài Đặt Cửa Cuốn Tự Động (Rolling Door)", false, 12)]
    public static void SetupRollingDoor()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogError("Vui lòng chọn tất cả các thanh cửa (Object_...) cần gộp thành cửa cuốn trước khi chạy lệnh!");
            return;
        }

        // Tạo object cha
        GameObject parentObj = new GameObject("RollingDoor_Interactable");
        
        // Đặt object cha ở vị trí của object đầu tiên được chọn
        parentObj.transform.position = selectedObjects[0].transform.position;
        parentObj.transform.rotation = selectedObjects[0].transform.rotation;

        if (selectedObjects[0].transform.parent != null)
        {
            parentObj.transform.SetParent(selectedObjects[0].transform.parent);
        }

        // Không gom con nữa để tránh lỗi Prefab (Không được dùng Undo.SetTransformParent).
        // Thay vào đó ta sẽ truyền mảng Transform cho RollingDoor điều khiển!
        Bounds bounds = new Bounds(selectedObjects[0].transform.position, Vector3.zero);
        bool hasBounds = false;
        
        Transform[] parts = new Transform[selectedObjects.Length];

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            GameObject obj = selectedObjects[i];
            parts[i] = obj.transform;

            // Tắt Static cho object này và tất cả con của nó
            foreach(Transform t in obj.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.isStatic = false;
            }

            // Tính Bounds dựa trên tất cả các Renderer con
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                if (!hasBounds)
                {
                    bounds = rend.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }
        }

        // Gắn script RollingDoor
        RollingDoor rollingDoor = parentObj.AddComponent<RollingDoor>();
        rollingDoor.doorParts = parts;

        // Thiết lập BoxCollider bao quanh tất cả các thanh cửa
        if (hasBounds)
        {
            BoxCollider col = parentObj.AddComponent<BoxCollider>();
            
            // Chuyển Bounds từ world space sang local space của parentObj
            col.center = parentObj.transform.InverseTransformPoint(bounds.center);
            
            // Chuyển kích thước world space sang local space (xử lý cả scale và rotation)
            Vector3 localSize = parentObj.transform.InverseTransformVector(bounds.size);
            col.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }
        else
        {
            parentObj.AddComponent<BoxCollider>();
        }

        // Thay đổi layer thành layer tương tác (nếu bạn có Layer "Interactable")
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer != -1)
        {
            parentObj.layer = interactableLayer;
        }

        Selection.activeGameObject = parentObj;
        Debug.Log("Đã thiết lập Cửa cuốn thành công! Một GameObject 'RollingDoor_Interactable' đã được tạo.");
    }
}
