using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpPower = 5f;
    public float gravity = 15f;
    public float lookSpeed = 2f;
    public float lookXLimit = 80f;

    public Camera playerCamera;
    
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private float rotationY = 0;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // Khóa chuột vào giữa màn hình và ẩn chuột đi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Tính toán hướng di chuyển
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical");
        float curSpeedY = (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal");
        float movementDirectionY = moveDirection.y;
        
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Nhảy
        if (Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Trọng lực
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else if (moveDirection.y < 0)
        {
            // Tránh việc tích tụ trọng lượng âm khi đang đứng trên mặt đất
            moveDirection.y = -2f;
        }

        // Di chuyển nhân vật
        characterController.Move(moveDirection * Time.deltaTime);

        // Xoay Camera và Nhân vật bằng chuột
        if (playerCamera != null)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;
            transform.localRotation = Quaternion.Euler(0, rotationY, 0);
        }
        
        // Thoát chuột khi bấm ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
