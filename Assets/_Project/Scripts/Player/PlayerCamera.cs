using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Cấu hình nhạy chuột")]
    [SerializeField] private float mouseSensitivity = 100f;

    [Header("References")]
    [SerializeField] private Transform playerBody; // Kéo GameObject Player (thân mình) vào đây

    [SerializeField] private MinigameManager minigameManager;

    private float xRotation = 0f;

    private void Start()
    {
        // Khóa con trỏ chuột vào giữa màn hình và ẩn nó
        CacheMinigameManager();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // Lấy dữ liệu chuột
        if (IsGameplayInputLocked())
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Tính toán góc xoay dọc (pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Giới hạn góc nhìn lên/xuống

        // Áp dụng xoay dọc cho camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Xoay thân người theo hướng ngang (yaw)
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private bool IsGameplayInputLocked()
    {
        CacheMinigameManager();
        return (minigameManager != null && minigameManager.IsMinigameActive) || 
               (PhoneManager.Instance != null && PhoneManager.Instance.IsPhoneOpen);
    }

    private void CacheMinigameManager()
    {
        if (minigameManager == null)
        {
            minigameManager = FindAnyObjectByType<MinigameManager>();
        }
    }


}

