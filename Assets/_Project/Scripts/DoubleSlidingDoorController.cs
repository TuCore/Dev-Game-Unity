using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class DoubleSlidingDoorController : MonoBehaviour, IInteractable
{
    [Header("Door Objects")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Settings")]
    public float openSpeed = 2f;
    public KeyCode interactKey = KeyCode.E;
    public float interactionDistance = 15f;

    [Tooltip("Trục mà cánh cửa sẽ trượt. Mặc định là trục X (1, 0, 0).")]
    public Vector3 slideDirection = new Vector3(1, 0, 0);

    [Header("Folding Settings")]
    public bool isFoldingDoor = true; // Bật chế độ Cửa Xếp
    [Range(0.01f, 1f)]
    public float foldAmount = 0.1f; // Xếp lại còn 10% chiều rộng ban đầu
    public float customSlideDistance = 3f; // Dùng khi isFoldingDoor = false

    private bool isDoorOpen = false;
    private bool isPlayerNearby = false;
    private Transform player;
    private Coroutine animationCoroutine;

    private Vector3 leftDoorClosedPos;
    private Vector3 rightDoorClosedPos;
    private Vector3 leftDoorClosedScale;
    private Vector3 rightDoorClosedScale;
    
    // Lưu lại chiều rộng ban đầu của cửa
    private float leftDoorWidth;
    private float rightDoorWidth;

    void Start()
    {
        // Tắt BoxCollider bao trùm cả 2 cánh cửa để người chơi có thể đi qua cửa
        Collider parentCollider = GetComponent<Collider>();
        if (parentCollider != null) parentCollider.enabled = false;

        if (leftDoor != null)
        {
            leftDoorClosedPos = leftDoor.localPosition;
            leftDoorClosedScale = leftDoor.localScale;
            Renderer r = leftDoor.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                leftDoorWidth = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
                // Tự động thêm BoxCollider và Proxy
                SetupDoorColliderAndProxy(r.gameObject);
            }
        }
        if (rightDoor != null)
        {
            rightDoorClosedPos = rightDoor.localPosition;
            rightDoorClosedScale = rightDoor.localScale;
            Renderer r = rightDoor.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                rightDoorWidth = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
                SetupDoorColliderAndProxy(r.gameObject);
            }
        }
    }

    void SetupDoorColliderAndProxy(GameObject meshObj)
    {
        if (meshObj.GetComponent<Collider>() == null)
        {
            meshObj.AddComponent<BoxCollider>();
        }
        // Thêm script proxy để nối từ tia nhìn Raycast về lại controller này
        DoorInteractProxy proxy = meshObj.GetComponent<DoorInteractProxy>();
        if (proxy == null) proxy = meshObj.AddComponent<DoorInteractProxy>();
        proxy.controller = this;
    }

    public string GetInteractionPrompt()
    {
        return isDoorOpen ? "Nhấn [E] để ĐÓNG CỬA" : "Nhấn [E] để MỞ CỬA";
    }

    public void Interact()
    {
        ToggleDoor();
    }

    void ToggleDoor()
    {
        if (leftDoor == null || rightDoor == null) return;
        isDoorOpen = !isDoorOpen;

        // Play the custom iron door sounds
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(isDoorOpen ? "open iron door" : "closing iron door");
        }
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateDoors(isDoorOpen));
    }

    IEnumerator AnimateDoors(bool open)
    {
        Vector3 leftScaleTarget = leftDoorClosedScale;
        Vector3 rightScaleTarget = rightDoorClosedScale;

        if (isFoldingDoor && open)
        {
            leftScaleTarget.x = leftDoorClosedScale.x * foldAmount;
            rightScaleTarget.x = rightDoorClosedScale.x * foldAmount;
        }

        Vector3 leftTarget = leftDoorClosedPos;
        Vector3 rightTarget = rightDoorClosedPos;

        if (open)
        {
            if (isFoldingDoor)
            {
                float slideDistLeft = leftDoorWidth * (1f - foldAmount) / 2f;
                float slideDistRight = rightDoorWidth * (1f - foldAmount) / 2f;
                
                leftTarget = leftDoorClosedPos - slideDirection.normalized * slideDistLeft;
                rightTarget = rightDoorClosedPos + slideDirection.normalized * slideDistRight;
            }
            else
            {
                leftTarget = leftDoorClosedPos - slideDirection.normalized * customSlideDistance;
                rightTarget = rightDoorClosedPos + slideDirection.normalized * customSlideDistance;
            }
        }
        
        float t = 0;
        Vector3 startLeftPos = leftDoor.localPosition;
        Vector3 startRightPos = rightDoor.localPosition;
        Vector3 startLeftScale = leftDoor.localScale;
        Vector3 startRightScale = rightDoor.localScale;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            leftDoor.localPosition = Vector3.Lerp(startLeftPos, leftTarget, smoothT);
            rightDoor.localPosition = Vector3.Lerp(startRightPos, rightTarget, smoothT);

            leftDoor.localScale = Vector3.Lerp(startLeftScale, leftScaleTarget, smoothT);
            rightDoor.localScale = Vector3.Lerp(startRightScale, rightScaleTarget, smoothT);
            
            yield return null;
        }
    }
}

public class DoorInteractProxy : MonoBehaviour, IInteractable
{
    public DoubleSlidingDoorController controller;

    public string GetInteractionPrompt()
    {
        if (controller != null) return controller.GetInteractionPrompt();
        return "";
    }

    public void Interact()
    {
        if (controller != null) controller.Interact();
    }
}
