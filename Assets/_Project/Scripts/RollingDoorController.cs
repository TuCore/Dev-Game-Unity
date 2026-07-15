using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class RollingDoorController : MonoBehaviour, IInteractable
{
    private bool isDoorOpen = false;
    private Collider doorCollider;
    private Animator animator;

    void Start()
    {
        doorCollider = GetComponent<Collider>();
        animator = GetComponentInChildren<Animator>();
        
        // Ensure animation starts stopped at 0
        if (animator != null)
        {
            animator.Play("Rolling", 0, 0f);
            animator.SetFloat("Speed", 0f);
        }
    }

    public string GetInteractionPrompt()
    {
        return isDoorOpen ? "Nhấn [E] để ĐÓNG CỬA CUỐN" : "Nhấn [E] để MỞ CỬA CUỐN";
    }

    public void Interact()
    {
        isDoorOpen = !isDoorOpen;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(isDoorOpen ? "open iron door" : "closing iron door");
        }

        // Tắt va chạm của khung cửa để người chơi đi qua khi cửa mở
        if (doorCollider != null) doorCollider.enabled = !isDoorOpen;

        // Bật animation: speed = 1 để cuộn lên (mở), speed = -1 để cuộn xuống (đóng)
        if (animator != null)
        {
            animator.SetFloat("Speed", isDoorOpen ? 1f : -1f);
        }
    }
}
