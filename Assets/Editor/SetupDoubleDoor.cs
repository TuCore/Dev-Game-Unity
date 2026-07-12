using UnityEngine;
using UnityEditor;

public class SetupDoubleDoor : EditorWindow
{
    [MenuItem("Tools/4. Auto Setup Double Sliding Door")]
    public static void ApplyDoubleDoorEffect()
    {
        // Lấy danh sách các object mà người dùng đang chọn
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length != 2)
        {
            Debug.LogError("Vui lòng giữ phím Ctrl và CLICK CHỌN ĐÚNG 2 CÁNH CỬA (ví dụ Cube.009 và Cube.010) trước khi chạy công cụ này!");
            return;
        }

        GameObject door1 = selectedObjects[0];
        GameObject door2 = selectedObjects[1];

        // Xác định cửa nào bên trái, cửa nào bên phải dựa trên toạ độ X toàn cầu
        GameObject leftDoor = door1.transform.position.x < door2.transform.position.x ? door1 : door2;
        GameObject rightDoor = door1.transform.position.x < door2.transform.position.x ? door2 : door1;

        // Tạo một object cha để chứa script điều khiển và tạo vùng tương tác click chuột
        GameObject controllerObj = new GameObject("Double_Sliding_Door_Controller");
        
        // Đặt object điều khiển ở chính giữa 2 cánh cửa
        Vector3 centerPos = (leftDoor.transform.position + rightDoor.transform.position) / 2f;
        controllerObj.transform.position = centerPos;
        controllerObj.transform.SetParent(leftDoor.transform.parent);

        // Thêm BoxCollider bao trùm 2 cánh cửa để click chuột
        BoxCollider boxCollider = controllerObj.AddComponent<BoxCollider>();
        // Tính toán kích thước BoxCollider dựa trên Renderer của 2 cửa
        Renderer rend1 = leftDoor.GetComponent<Renderer>();
        Renderer rend2 = rightDoor.GetComponent<Renderer>();
        if (rend1 != null && rend2 != null)
        {
            Bounds combinedBounds = rend1.bounds;
            combinedBounds.Encapsulate(rend2.bounds);
            boxCollider.center = controllerObj.transform.InverseTransformPoint(combinedBounds.center);
            boxCollider.size = combinedBounds.size;
            // Làm cho collider dày ra 1 chút về phía trước để dễ click
            Vector3 size = boxCollider.size;
            size.z += 2f;
            boxCollider.size = size;
        }

        // Gắn script điều khiển vào
        DoubleSlidingDoorController doorScript = controllerObj.AddComponent<DoubleSlidingDoorController>();
        doorScript.leftDoor = leftDoor.transform;
        doorScript.rightDoor = rightDoor.transform;

        // Đặt khoảng cách trượt tự động dựa trên độ rộng của 1 cánh cửa
        if (rend1 != null)
        {
            // Trục mà cửa trượt thường là trục rộng nhất
            float slideDist = Mathf.Max(rend1.bounds.size.x, rend1.bounds.size.z);
            doorScript.customSlideDistance = slideDist * 0.9f; // Trượt 90% chiều rộng để đẹp hơn
        }

        Selection.activeGameObject = controllerObj;
        Debug.Log("Đã thiết lập thành công cửa trượt đôi ngang! Bấm Play để test ngay.");
    }
}
