using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class RepairingHouseDoorController : MonoBehaviour
{
    private static readonly int DoorClosedState = Animator.StringToHash("DoorClosed");
    private static readonly int DoorOpenState = Animator.StringToHash("DoorOpen");
    private static readonly int DoorCloseState = Animator.StringToHash("DoorClose");

    [SerializeField] private bool openOnStart = true;

    private Animator animator;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (openOnStart)
        {
            OpenDoor();
        }
        else
        {
            animator.Play(DoorClosedState, 0, 0f);
        }
    }

    [ContextMenu("Open Door")]
    public void OpenDoor()
    {
        animator.Play(DoorOpenState, 0, 0f);
        IsOpen = true;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Tiếng mở cửa");
        }
    }

    [ContextMenu("Close Door")]
    public void CloseDoor()
    {
        animator.Play(DoorCloseState, 0, 0f);
        IsOpen = false;
    }

    [ContextMenu("Toggle Door")]
    public void ToggleDoor()
    {
        if (IsOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }
}
