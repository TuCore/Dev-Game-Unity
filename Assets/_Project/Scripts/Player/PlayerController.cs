using UnityEngine;

public class PlayerController : MonoBehaviour
{


    [Header("Cấu hình di chuyển")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float jumpHeight = 5f;

    [Header("Âm thanh di chuyển")]
    [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.85f;
    [SerializeField] private float footstepInterval = 0.42f;

    [Header("Input Lock")]
    [SerializeField] private MinigameManager minigameManager;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;
    private float _stepTimer = 0f;
    private static bool _hasPlayedIntroSession = false;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        CacheMinigameManager();
        if (footstepVolume <= 0.05f)
        {
            footstepVolume = 1.0f; // Bảo vệ khi deserialize từ scene cũ chưa lưu giá trị
        }
        if (footstepInterval <= 0.05f)
        {
            footstepInterval = 0.42f;
        }
    }

    private void Start()
    {
        if (!_hasPlayedIntroSession)
        {
            _hasPlayedIntroSession = true;
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.PlayIntroSequence(() =>
                {
                    if (ToastNotificationManager.Instance != null)
                    {
                        ToastNotificationManager.Instance.ShowToast("[+] Thức dậy rồi, hãy khám phá tiệm hoặc đón khách sửa chữa ngay thôi!", 4f);
                    }
                });
            }
        }

        // Tự động spawn Pause Menu (Settings) nếu chưa có trong scene
        if (FindAnyObjectByType<AnhThoDien.UI.Menu.PauseMenuUI>() == null)
        {
            GameObject pausePrefab = Resources.Load<GameObject>("PauseMenu_Canvas");
            if (pausePrefab != null) Instantiate(pausePrefab);
        }

        // Khôi phục vị trí người chơi nếu chọn "Chơi tiếp"
        if (PlayerPrefs.GetInt("IsNewGame", 1) == 0)
        {
            if (SaveSystem.TryLoadPlayerPosition(out Vector3 savedPos))
            {
                _controller.enabled = false; // Tắt CharacterController để set position
                transform.position = savedPos;
                _controller.enabled = true;
            }
        }
        else
        {
            // Tự động spawn bảng hướng dẫn nếu chưa xem
            if (PlayerPrefs.GetInt("TutorialShown", 0) == 0)
            {
                GameObject tutPrefab = Resources.Load<GameObject>("Tutorial_Canvas");
                if (tutPrefab != null && FindAnyObjectByType<AnhThoDien.UI.TutorialUI>() == null)
                {
                    Instantiate(tutPrefab);
                }
            }

            // Bắt đầu game mới -> Dịch chuyển người chơi tới "shop main" nếu có trong scene
            GameObject shopMain = GameObject.Find("shop main");
            if (shopMain == null) shopMain = GameObject.Find("Shop Main");
            if (shopMain == null) shopMain = GameObject.Find("ShopMain");
            
            if (shopMain != null)
            {
                _controller.enabled = false;
                transform.position = shopMain.transform.position;
                transform.rotation = shopMain.transform.rotation;
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

        float x = CustomInputManager.GetAxisHorizontal();
        float z = CustomInputManager.GetAxisVertical();

        // Kiểm tra Shift để chạy (chạy nhanh hơn một chút, giảm từ x2 xuống x1.5)
        bool isRunning = CustomInputManager.GetKey("Run");
        float currentSpeed = isRunning ? runSpeed * 1.5f : moveSpeed;

        // Tạo vector di chuyển dựa trên đầu vào
        Vector3 move = transform.right * x + transform.forward * z;

        // Di chuyển nhân vật
        _controller.Move(move * currentSpeed * Time.deltaTime);

        // Phát tiếng bước chân vừa đủ nghe, rõ ràng, không vang vọng và chỉ ngắt khi dừng phím di chuyển
        bool isMoving = move.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                // Khi chạy (giữ Shift), tiếng bước chân dồn dập hơn (thời gian giữa 2 bước ngắn lại)
                _stepTimer = isRunning ? footstepInterval * 0.65f : footstepInterval;
                float currentVolume = (footstepVolume <= 0.05f) ? 1.0f : footstepVolume;
                
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
                if (sceneName.Contains("vietnamstreet") || sceneName.Contains("shop"))
                {
                    currentVolume = Mathf.Max(currentVolume, 1.0f);
                }
                
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayFootstep("521590__fission9__hiking-boot-footsteps-on-stone", currentVolume, Random.Range(0.98f, 1.02f));
                }
            }
        }
        else
        {
            if (_stepTimer > 0f)
            {
                _stepTimer = 0f;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopFootstep();
                }
            }
        }

        // Nhảy khi nhấn phím Space và nhân vật đang đứng trên mặt đất
        if (CustomInputManager.GetKeyDown("Jump") && _isGrounded)
        {
            // Nhảy cao hơn một chút (x1.3), và cao hơn nữa nếu đang chạy
            float baseJump = jumpHeight * 1.3f;
            float currentJumpHeight = isRunning ? baseJump * 1.3f : baseJump;
            // Áp dụng chung hệ số trọng lực (x4) vào công thức tính lực nhảy ban đầu
            _velocity.y = Mathf.Sqrt(currentJumpHeight * -2f * (gravity * 4.0f));
        }

        // Áp dụng trọng lực (tốc độ nhảy lên và rơi xuống đều nhanh gấp 4 lần)
        float gravityMultiplier = 4.0f;
        _velocity.y += gravity * gravityMultiplier * Time.deltaTime;

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
            if (CustomInputManager.GetKey("Jump") || Input.GetMouseButton(0))
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






