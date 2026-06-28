using UnityEngine;

namespace DevGameUnity.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class FreeFlyCameraController : MonoBehaviour
    {
        public float moveSpeed = 8f;
        public float fastMoveMultiplier = 3f;
        public float lookSensitivity = 2f;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        private float yaw;
        private float pitch;

        private void Awake()
        {
            var euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetMouseButtonUp(1))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (!Input.GetMouseButton(1))
            {
                return;
            }

            yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * lookSensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void HandleMovement()
        {
            var vertical = 0f;
            if (Input.GetKey(KeyCode.E))
            {
                vertical += 1f;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                vertical -= 1f;
            }

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), vertical, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);

            var speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= fastMoveMultiplier;
            }

            var movement = transform.right * input.x + Vector3.up * input.y + transform.forward * input.z;
            transform.position += movement * (speed * Time.unscaledDeltaTime);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
