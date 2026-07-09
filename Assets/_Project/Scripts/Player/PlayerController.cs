using UnityEngine;

public class PlayerController : MonoBehaviour
{


    [Header("Cấu hình di chuyển")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Input Lock")]
    [SerializeField] private MinigameManager minigameManager;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        CacheMinigameManager();
    }

    private void Update()
    {
        // Kiểm tra xem nhân vật có đang đứng trên mặt đất hay không
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Nếu nhân vật đang đứng trên mặt đất và có vận tốc theo trục y âm, đặt vận tốc theo trục y về 0
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Đặt một giá trị nhỏ để giữ nhân vật trên mặt đất
        }

        // Nhận đầu vào từ người chơi
        if (IsGameplayInputLocked())
        {
            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
            return;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Tạo vector di chuyển dựa trên đầu vào
        Vector3 move = transform.right * x + transform.forward * z;

        // Di chuyển nhân vật
        _controller.Move(move * moveSpeed * Time.deltaTime);

        // Nhảy khi nhấn phím Space và nhân vật đang đứng trên mặt đất
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Áp dụng trọng lực
        _velocity.y += gravity * Time.deltaTime;

        // Di chuyển nhân vật theo vận tốc
        _controller.Move(_velocity * Time.deltaTime);
    }

    private bool IsGameplayInputLocked()
    {
        CacheMinigameManager();
        return minigameManager != null && minigameManager.IsMinigameActive;
    }

    private void CacheMinigameManager()
    {
        if (minigameManager == null)
        {
            minigameManager = FindAnyObjectByType<MinigameManager>();
        }
    }

}

