using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class RollingDoorController : MonoBehaviour, IInteractable
{
    private bool isDoorOpen = false;
    private Collider doorCollider;
    private Animator animator;

    [Header("Cấu hình Animation")]
    [Tooltip("Thời điểm cửa mở hoàn toàn (0.5 = một nửa clip)")]
    [SerializeField] private float openNormalizedTime = 0.5f; 
    [Tooltip("Tốc độ cuộn cửa (clip gốc 24s nên để 2-3 cho nhanh)")]
    [SerializeField] private float animationSpeed = 2.5f;

    void Start()
    {
        doorCollider = GetComponent<Collider>();
        animator = GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            animator.Play("Rolling", 0, 0f);
            animator.SetFloat("Speed", 0f);
        }
    }

    void Update()
    {
        if (animator == null) return;
        
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Rolling"))
        {
            if (isDoorOpen)
            {
                // Nếu đang mở và chạm đến điểm dừng ở giữa clip
                if (state.normalizedTime >= openNormalizedTime && animator.GetFloat("Speed") > 0)
                {
                    animator.SetFloat("Speed", 0f);
                    animator.Play("Rolling", 0, openNormalizedTime);
                }
            }
            else
            {
                // Nếu đang đóng và đã lùi về 0
                if (state.normalizedTime <= 0f && animator.GetFloat("Speed") < 0)
                {
                    animator.SetFloat("Speed", 0f);
                    animator.Play("Rolling", 0, 0f);
                }
            }
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

        if (doorCollider != null) doorCollider.isTrigger = isDoorOpen;

        if (animator != null)
        {
            // Mở: chạy xuôi, Đóng: chạy ngược
            animator.SetFloat("Speed", isDoorOpen ? animationSpeed : -animationSpeed);
        }
    }
}
