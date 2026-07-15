using UnityEngine;

namespace DevGameUnity.NPC
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class StreetNpcWalker : MonoBehaviour
    {
        public Animator animator;
        public string walkState = "Walk";
        public Vector3 patrolPointA;
        public Vector3 patrolPointB;
        public float movementSpeed = 1.65f;
        public float turnSpeed = 5f;
        public float arrivalDistance = 0.65f;
        public float gravity = -24f;
        public float walkDistance = 20f; // Quãng đường đi thẳng
        public bool loopPatrol = true; // Đi qua đi lại liên tục

        [Header("Delivery")]
        public GameObject deliveryPrefab;
        public float dropForwardOffset = 0.8f;

        private CharacterController controller;
        private float verticalVelocity;
        private int walkStateHash;
        private bool delivered;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            walkStateHash = Animator.StringToHash(walkState);

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            //if ((patrolPointB - patrolPointA).sqrMagnitude < 4f)
            //{
            //    patrolPointA = transform.position;
            //    patrolPointB = transform.position + transform.forward * 20f;
            //}

            patrolPointA = transform.position;
            patrolPointB = transform.position + transform.forward * walkDistance;
        }

        private void Start()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.applyRootMotion = false;
                animator.CrossFadeInFixedTime(walkStateHash, 0.1f);
                animator.speed = Mathf.Clamp(movementSpeed / 1.65f, 0.75f, 1.35f);
            }
        }

        private bool isTurning; // Đang ở trạng thái xoay tại chỗ — không xét arrival

        private void Update()
        {
            if (delivered && !loopPatrol)
            {
                ApplyGravity();
                return;
            }

            var toTarget = patrolPointB - transform.position;
            toTarget.y = 0f;

            // Chỉ xét đến đích khi không đang xoay (tránh ping-pong swap gây đi vòng cung)
            if (!isTurning && toTarget.magnitude <= arrivalDistance)
            {
                if (!delivered && deliveryPrefab != null)
                {
                    DeliverCarriedObject();
                }

                if (loopPatrol)
                {
                    var temp = patrolPointA;
                    patrolPointA = patrolPointB;
                    patrolPointB = temp;

                    toTarget = patrolPointB - transform.position;
                    toTarget.y = 0f;

                    // Bắt đầu pha xoay tại chỗ — ngăn di chuyển và ngăn swap lại
                    isTurning = true;
                }
                else
                {
                    if (animator != null) animator.speed = 0f;
                    delivered = true;
                    ApplyGravity();
                    return;
                }
            }

            var direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
            float angleToTarget = Vector3.Angle(transform.forward, direction);

            // Xoay người nhanh và dứt khoát về hướng di chuyển (RotateTowards thay vì Slerp tiệm cận)
            float maxDegreesPerSecond = angleToTarget > 15f ? turnSpeed * 80f : turnSpeed * 40f;
            var desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, maxDegreesPerSecond * Time.deltaTime);

            // Khi đang xoay hoặc góc còn lớn (> 5 độ): dừng di chuyển thẳng hoàn toàn
            float moveMultiplier;
            if (isTurning)
            {
                // Kết thúc pha xoay khi đã căn chỉnh xong (sai số ≤ 2°)
                if (angleToTarget <= 2f)
                {
                    isTurning = false;
                }
                moveMultiplier = 0f;
            }
            else
            {
                moveMultiplier = angleToTarget > 5f ? 0f : Mathf.Clamp01((5f - angleToTarget) / 5f);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            var motion = direction * (movementSpeed * moveMultiplier);
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private void DeliverCarriedObject()
        {
            delivered = true;

            if (deliveryPrefab == null)
            {
                return;
            }

            var droppedObject = Instantiate(deliveryPrefab).transform;
            droppedObject.name = deliveryPrefab.name;

            var colliders = droppedObject.GetComponentsInChildren<Collider>(true);
            foreach (var itemCollider in colliders)
            {
                itemCollider.enabled = false;
            }

            var dropPosition = transform.position + transform.forward * dropForwardOffset;
            var dropRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            droppedObject.SetPositionAndRotation(dropPosition, dropRotation);

            if (Physics.Raycast(dropPosition + Vector3.up * 3f, Vector3.down, out var hit, 20f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var renderers = droppedObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    for (var index = 1; index < renderers.Length; index++)
                    {
                        bounds.Encapsulate(renderers[index].bounds);
                    }

                    dropPosition.y = hit.point.y + (droppedObject.position.y - bounds.min.y);
                }
                else
                {
                    dropPosition.y = hit.point.y;
                }

                droppedObject.position = dropPosition;
            }

            foreach (var itemCollider in colliders)
            {
                itemCollider.enabled = true;
            }
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        // Vẽ hỗ trợ trực quan trong Scene để bạn dễ dàng kéo thanh trượt
        private void OnDrawGizmosSelected()
        {
            // 1. Điểm NPC sẽ đứng lại
            Vector3 standPoint = transform.position + transform.forward * walkDistance;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(standPoint, 0.5f);
            
            // Vẽ đường nối từ NPC đến chỗ đứng
            Gizmos.DrawLine(transform.position, standPoint);

            // 2. Điểm thả đồ
            Vector3 dropPoint = standPoint + transform.forward * dropForwardOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(dropPoint, new Vector3(0.5f, 0.5f, 0.5f));
            
            // Vẽ đường nối từ chỗ đứng tới chỗ thả đồ
            Gizmos.DrawLine(standPoint, dropPoint);
        }
    }
}
