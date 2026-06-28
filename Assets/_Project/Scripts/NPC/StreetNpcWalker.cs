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

        private void Update()
        {
            if (delivered)
            {
                ApplyGravity();
                return;
            }

            var toTarget = patrolPointB - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= arrivalDistance)
            {
                DeliverCarriedObject();
                ApplyGravity();
                return;
            }

            var direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
            var desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            var motion = direction * movementSpeed;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private void DeliverCarriedObject()
        {
            delivered = true;
            if (animator != null)
            {
                animator.speed = 0f;
            }

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
    }
}
