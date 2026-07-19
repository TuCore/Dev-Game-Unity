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

    private float GetLocalBottomY()
    {
        if (_col == null) return 0f;

        if (_col is BoxCollider box)
        {
            return box.center.y - box.size.y / 2f;
        }
        else if (_col is SphereCollider sphere)
        {
            return sphere.center.y - sphere.radius;
        }
        else if (_col is CapsuleCollider capsule)
        {
            // Capsule direction: 0 = X, 1 = Y, 2 = Z
            int direction = capsule.direction;
            float height = capsule.height;
            float radius = capsule.radius;
            float offset = (direction == 1) ? height / 2f : radius;
            return capsule.center.y - offset;
        }
        else if (_col is MeshCollider meshCol && meshCol.sharedMesh != null)
        {
            return meshCol.sharedMesh.bounds.min.y;
        }

        return 0f;
    }

    public float GetHeightOffsetForRotation(Quaternion rotation)
    {
        // Vì vật thể luôn đứng thẳng (chỉ xoay quanh trục Y), 
        // khoảng cách từ tâm đến đáy theo phương thẳng đứng (trục Y) là không đổi và bằng:
        return -GetLocalBottomY() * _originalScale.y;
    }

    public void Drop(Vector3 placePosition, Quaternion placeRotation)
    {
        // Thả vật thể về Parent cũ
        transform.SetParent(_originalParent);
        
        // Trả lại kích thước thật
        transform.localScale = _originalScale;
        
        // Sử dụng góc xoay mới (đã được xoay bằng chuột)
        transform.rotation = placeRotation;

        // Bật lại collider
        if (_col != null)
        {
            _col.enabled = true;
        }

        // Tính toán offset nâng vật thể lên mặt bàn/đất dựa trên local bottom và original scale
        float offsetY = GetHeightOffsetForRotation(placeRotation);

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
