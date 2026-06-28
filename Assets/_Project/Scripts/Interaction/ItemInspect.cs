using UnityEngine;

/// <summary>
/// Cho phép người chơi cầm, xoay, soi đồ vật trong chế độ FPP.
/// Sử dụng để "khám bệnh" sơ bộ trước khi chuyển sang minigame sửa chữa.
/// </summary>
public class ItemInspect : MonoBehaviour
{
    [Header("Cấu hình xoay/soi")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2f;
    [SerializeField] private Transform inspectPoint; // Vị trí đặt đồ vật khi soi

    private GameObject _currentItem;
    private bool _isInspecting;
    private float _currentZoom = 1f;

    public bool IsInspecting => _isInspecting;
    public GameObject CurrentItem => _currentItem;

    // Events
    public System.Action<GameObject> OnInspectStarted;
    public System.Action OnInspectEnded;

    /// <summary>
    /// Bắt đầu chế độ soi đồ vật — cầm đồ lên và đặt trước mặt.
    /// </summary>
    public void StartInspect(GameObject item)
    {
        if (_isInspecting) return;

        _currentItem = item;
        _isInspecting = true;
        _currentZoom = 1f;

        // Đặt đồ vật vào vị trí soi
        if (inspectPoint != null)
        {
            item.transform.SetParent(inspectPoint);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }

        OnInspectStarted?.Invoke(item);
    }

    /// <summary>
    /// Kết thúc chế độ soi — trả đồ vật về vị trí cũ.
    /// </summary>
    public void EndInspect()
    {
        if (!_isInspecting) return;

        _isInspecting = false;

        if (_currentItem != null)
        {
            _currentItem.transform.SetParent(null);
        }

        _currentItem = null;
        OnInspectEnded?.Invoke();
    }

    private void Update()
    {
        if (!_isInspecting || _currentItem == null) return;

        HandleRotation();
        HandleZoom();
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(0)) // Kéo chuột trái để xoay
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;
            _currentItem.transform.Rotate(Vector3.up, -rotX, Space.World);
            _currentItem.transform.Rotate(Vector3.right, rotY, Space.World);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _currentZoom = Mathf.Clamp(_currentZoom + scroll * zoomSpeed, minZoom, maxZoom);
            _currentItem.transform.localScale = Vector3.one * _currentZoom;
        }
    }
}
