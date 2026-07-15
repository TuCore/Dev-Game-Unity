using UnityEngine;
using System.Collections;

public class RollingDoor : MonoBehaviour, IInteractable
{
    [Header("Cấu hình Cửa Cuốn")]
    [Tooltip("Độ cao khi cửa cuộn lên hết cỡ")]
    [SerializeField] private float rollHeight = 2.8f; 
    
    [Tooltip("Tốc độ cuộn")]
    [SerializeField] private float rollSpeed = 2f;

    [Tooltip("Tên âm thanh khi cuộn cửa (nếu có)")]
    [SerializeField] private string soundName = "Tiếng mở cửa";

    [Tooltip("Danh sách các thanh cửa cần cuộn")]
    public Transform[] doorParts;

    private bool _isOpen = false;
    private bool _isMoving = false;

    public string GetInteractionPrompt()
    {
        if (_isMoving) return "Đang chạy...";
        return _isOpen ? "Đóng cửa cuốn" : "Cuộn cửa lên";
    }

    public void Interact()
    {
        if (_isMoving) return;
        
        _isOpen = !_isOpen;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }

        StopAllCoroutines();
        StartCoroutine(RollRoutine(_isOpen ? rollHeight : -rollHeight));
    }

    private IEnumerator RollRoutine(float heightOffset)
    {
        _isMoving = true;
        Vector3 direction = Vector3.up * heightOffset;

        // Lưu vị trí đích của từng thanh cửa
        Vector3[] targetPositions = new Vector3[doorParts.Length];
        for (int i = 0; i < doorParts.Length; i++)
        {
            if (doorParts[i] != null)
                targetPositions[i] = doorParts[i].position + direction;
        }

        float distanceMoved = 0f;
        float totalDistance = Mathf.Abs(heightOffset);

        while (distanceMoved < totalDistance)
        {
            float step = rollSpeed * Time.deltaTime;
            if (distanceMoved + step > totalDistance) step = totalDistance - distanceMoved;

            Vector3 moveVector = Vector3.up * (Mathf.Sign(heightOffset) * step);
            
            // Di chuyển chính collider
            transform.position += moveVector;

            // Di chuyển các thanh cửa
            for (int i = 0; i < doorParts.Length; i++)
            {
                if (doorParts[i] != null)
                {
                    doorParts[i].position += moveVector;
                }
            }

            distanceMoved += step;
            yield return null;
        }

        _isMoving = false;
    }
}
