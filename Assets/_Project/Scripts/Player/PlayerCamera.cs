using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float mouseSensitivity = 300f;
    
    public Transform playerBody;
    private float _xRotation = 0f;

    [HideInInspector] public bool canLook = true;

    void Start()
    {
        // Khóa con trỏ chuột vào giữa màn hình và làm mờ đi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!canLook) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Quay trục dọc (cúi xuống / ngẩng lên)
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        
        // Quay trục ngang (xoay cả thân người)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
