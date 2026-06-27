using UnityEngine;

namespace DevGameUnity.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimpleFirstPersonController : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;

        [Header("Movement")]
        public float walkSpeed = 5.5f;
        public float sprintSpeed = 8.5f;
        public float jumpHeight = 1.2f;
        public float gravity = -24f;

        [Header("Look")]
        public float mouseSensitivity = 2.0f;
        public float minPitch = -80f;
        public float maxPitch = 82f;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || playerCamera == null)
            {
                return;
            }

            var mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            var mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);

            var speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            var move = (transform.right * input.x + transform.forward * input.z) * speed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (controller.isGrounded && Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }
    }
}
