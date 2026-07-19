using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupItem : MonoBehaviour, IInteractable
{
    private Rigidbody _rb;
    private Collider _col;
    private Transform _originalParent;

    private Vector3 _originalScale;
    private Quaternion _originalRotation;

    public Quaternion OriginalRotation => _originalRotation;
    public Vector3 OriginalScale => _originalScale;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        _originalParent = transform.parent;
        
        _originalScale = transform.localScale;
    }

    public string GetInteractionPrompt()
    {
        // Viết ngắn gọn và dễ hiểu trên UI
        return $"Nhấn [E] để Nhặt {gameObject.name.Replace("Interactable_", "")}";
    }

    public void Interact()
    {
        // Interface IInteractable bắt buộc phải có
    }

    public void Pickup(Transform holdPosition, float scaleMultiplier = 0.25f)
    {
        // Lưu lại góc xoay hiện tại (để lúc đặt xuống nó vẫn đứng thẳng như cũ)
        _originalRotation = transform.rotation;

        // Tắt vật lý để vật thể không bị rơi
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        
        // Tắt collider để tránh va chạm cản đường người chơi khi đang cầm
        if (_col != null)
        {
            _col.enabled = false;
        }

        // Gắn vật thể vào tay người chơi
        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        // Thu nhỏ lại để không chiếm hết màn hình
        transform.localScale = _originalScale * scaleMultiplier;
    }

    public float GetHeightOffsetForRotation(Quaternion rotation)
    {
        if (_col == null) return 0f;
        
        // Lưu trữ tạm thời trạng thái hiện tại
        Vector3 tempPos = transform.position;
        Quaternion tempRot = transform.rotation;
        Vector3 tempScale = transform.localScale;
        Transform tempParent = transform.parent;
        bool tempColEnabled = _col.enabled;

        // Áp dụng trạng thái thả tạm thời để tính toán bounding box
        transform.SetParent(_originalParent);
        transform.localScale = _originalScale;
        transform.rotation = rotation;
        transform.position = Vector3.zero;
        _col.enabled = true;

        // Tính toán offset từ tâm (pivot) đến điểm thấp nhất của collider
        float bottomY = _col.bounds.min.y;
        float offsetY = -bottomY; // Vì vị trí tạm thời có Y = 0, nên offset = -bottomY

        // Phục hồi lại trạng thái cũ
        transform.SetParent(tempParent);
        transform.position = tempPos;
        transform.rotation = tempRot;
        transform.localScale = tempScale;
        _col.enabled = tempColEnabled;

        return offsetY;
    }

    public void Drop(Vector3 placePosition, Quaternion placeRotation)
    {
        // Thả vật thể về Parent cũ
        transform.SetParent(_originalParent);
        
        // Trả lại kích thước thật
        transform.localScale = _originalScale;
        
        // Sử dụng góc xoay mới (đã được xoay bằng chuột)
        transform.rotation = placeRotation;

        // Bật lại collider để hệ thống tính toán chính xác bounds
        if (_col != null)
        {
            _col.enabled = true;
        }

        // Tính toán offset nâng vật thể lên mặt bàn/đất
        float offsetY = 0f;
        if (_col != null)
        {
            transform.position = placePosition; // Đặt tạm thời để tính bounds
            float bottomY = _col.bounds.min.y;
            float pivotY = transform.position.y;
            offsetY = pivotY - bottomY;
        }

        // Đưa vật thể đến đúng vị trí mặt bàn/đất đã nâng offset
        transform.position = placePosition + new Vector3(0f, offsetY, 0f);

        // Bật lại vật lý nhưng giữ ở trạng thái Kinematic để nó dính chặt vào mặt bàn/đất (không bị rơi)
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
    }
}
