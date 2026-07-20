using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Cấu hình nhạy chuột")]
    [SerializeField] private float mouseSensitivity = 100f;

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
    }

    /// <summary>
    /// Đồng bộ góc xoay camera từ bên ngoài (khi khôi phục vị trí/góc nhìn sau minigame)
    /// </summary>
    public void SyncRotation(Quaternion localRotation)
    {
        transform.localRotation = localRotation;
        Vector3 euler = localRotation.eulerAngles;
        xRotation = euler.x > 180f ? euler.x - 360f : euler.x;
    }

    [Header("Cấu hình Camera")]
    [Tooltip("Chiều cao của camera so với mặt đất (thân người)")]
    [SerializeField] private float cameraHeight = 1.85f; // Nâng lên 1.85 thay vì 1.6 như cũ

    [Header("References")]
    [SerializeField] private Transform playerBody; // Kéo GameObject Player (thân mình) vào đây

    [SerializeField] private MinigameManager minigameManager;

    private float xRotation = 0f;

    private void Start()
    {
        // Đọc độ nhạy chuột đã lưu (nếu có)
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 100f);

        // Khóa con trỏ chuột vào giữa màn hình và ẩn nó
        CacheMinigameManager();
        Cursor.lockState = CursorLockMode.Locked;

        // Tự động nâng Camera lên tầm mắt để góc nhìn không bị quá thấp
        transform.localPosition = new Vector3(0, cameraHeight, 0);
    }

    private void LateUpdate()
    {
        // Lấy dữ liệu chuột
        if (IsGameplayInputLocked())
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 0.02f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 0.02f;

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
        if (Time.timeScale == 0f) return true;

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

