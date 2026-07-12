using UnityEngine;

public class PlayerController : MonoBehaviour
{


    [Header("Cấu hình di chuyển")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float jumpHeight = 5f;

    [Header("Input Lock")]
    [SerializeField] private MinigameManager minigameManager;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Audio")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip footstepClip;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        CacheMinigameManager();
    }

    private void Start()
    {
        // Load saved position if continuing game
        if (PlayerPrefs.GetInt("IsNewGame", 1) == 0)
        {
            if (SaveSystem.TryLoadPlayerPosition(out Vector3 savedPos))
            {
                // Must disable CharacterController temporarily to change transform.position directly
                _controller.enabled = false;
                transform.position = savedPos;
                _controller.enabled = true;
            }
        }
    }

    private void Update()
    {
        // Kiểm tra xem nhân vật có đang đứng trên mặt đất hay không
        // Tự động dùng isGrounded của CharacterController thay vì CheckSphere vì GroundMask bị set sai
        _isGrounded = _controller.isGrounded;

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

        // Kiểm tra Shift để chạy
        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        // Tạo vector di chuyển dựa trên đầu vào
        Vector3 move = transform.right * x + transform.forward * z;

        // Di chuyển nhân vật
        _controller.Move(move * currentSpeed * Time.deltaTime);

        // Xử lý âm thanh bước chân
        if (footstepAudioSource != null && footstepClip != null)
        {
            bool isMoving = _isGrounded && move.sqrMagnitude > 0.01f;
            if (isMoving)
            {
                if (!footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.clip = footstepClip;
                    footstepAudioSource.loop = true;
                    footstepAudioSource.Play();
                }
                // Tăng tốc độ phát âm thanh nếu đang chạy
                footstepAudioSource.pitch = isRunning ? 1.3f : 1.0f;
            }
            else
            {
                if (footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.Pause();
                }
            }
        }

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

    private bool _wasLocked;

    private bool IsGameplayInputLocked()
    {
        CacheMinigameManager();
        bool isLocked = (minigameManager != null && minigameManager.IsMinigameActive) || 
                        (PhoneManager.Instance != null && PhoneManager.Instance.IsPhoneOpen);

        if (isLocked)
        {
            _wasLocked = true;
            return true;
        }
        else if (_wasLocked)
        {
            // Tránh tình trạng nhả minigame ra mà input Space/Chuột vẫn còn dính ở frame đó
            // Phải đợi người chơi nhả hết phím nhảy/chuột ra thì mới cho phép di chuyển tiếp
            if (Input.GetButton("Jump") || Input.GetMouseButton(0))
            {
                return true;
            }
            
            // Xoá cờ nếu các phím đã được nhả
            _wasLocked = false;
        }

        return false;
    }

    private void CacheMinigameManager()
    {
        if (minigameManager == null)
        {
            minigameManager = FindAnyObjectByType<MinigameManager>();
        }
    }
}

