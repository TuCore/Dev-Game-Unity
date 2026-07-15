using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum CustomerState
{
    Wandering,
    Visiting,
    WaitingToNegotiate,
    Leaving,
    ReturningForPickup,
    WaitingForPickup,
    AmbientWalking
}

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour, IInteractable
{
    public NPCArchetype archetype;
    public Transform counterTarget;
    public Transform exitTarget;
    public GameObject itemPrefabToDrop;
    [Tooltip("Nếu danh sách này có đồ, NPC sẽ bốc random từ đây. Nếu để trống, sẽ xài Item Prefab To Drop bên trên.")]
    public List<GameObject> possibleItemsToDrop;
    public Transform itemDropPoint;
    
    private GameObject _selectedItemPrefab;
    private MinigameType _selectedMinigame;
    private int _selectedDifficulty;
    private float _selectedBasePay;
    
    private NavMeshAgent agent;
    private Animator animator;
    private CustomerOrder currentOrder;
    public CustomerState currentState = CustomerState.Wandering;
    private bool hasInteracted = false;
    
    private Transform ambientPointA;
    private Transform ambientPointB;
    
    // Queue static list để xếp hàng
    public static List<CustomerController> storeLine = new List<CustomerController>();

    private Vector3 originalBodyScale;
    private int wanderCount = 0; // Số điểm muốn đi dạo trước khi vào tiệm

    private Vector3 approachOffset;
    private bool hasConvergedToQueue = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            originalBodyScale = animator.transform.localScale;
            animator.applyRootMotion = false; // TẮT CHẶN: Tránh Animator dành quyền di chuyển gây teleport
        }
        
        if (agent != null)
        {
            agent.updateRotation = false; // TẮT CHẶN: Để script tự xoay mặt (chống moonwalk do NavMeshAgent)
        }

        // TẮT CHẶN: Đảm bảo Rigidbody không đánh lộn với NavMeshAgent gây văng tung tóe (teleport)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void Start()
    {
        agent.stoppingDistance = 0.5f;
        agent.avoidancePriority = Random.Range(30, 70); 
        
        // Tạo một góc tiếp cận ngẫu nhiên (rộng 8m) để khách đi từ xa không bị trùng đường
        Vector2 randCircle = Random.insideUnitCircle * 8f;
        approachOffset = new Vector3(randCircle.x, 0, randCircle.y);

        if (currentState != CustomerState.ReturningForPickup && currentState != CustomerState.AmbientWalking)
        {
            currentState = CustomerState.Wandering;
            wanderCount = Random.Range(2, 6); // Đi dạo từ 2 đến 5 điểm trước khi vào tiệm
            PickRandomWanderPoint();
        }
    }

    private void PickRandomWanderPoint()
    {
        // Đi dạo ngẫu nhiên trong bán kính 10m xung quanh vị trí hiện tại
        Vector2 randomCircle = Random.insideUnitCircle * 10f;
        Vector3 randomWander = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(randomWander, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (agent != null) agent.SetDestination(hit.position);
        }
    }

    public void SetReturningOrder(CustomerOrder order)
    {
        this.currentOrder = order;
        this.currentState = CustomerState.ReturningForPickup;
        if (!storeLine.Contains(this)) storeLine.Add(this);
    }

    public void SetAmbientWalker(Transform pointA, Transform pointB, bool startAtA)
    {
        this.currentState = CustomerState.AmbientWalking;
        this.ambientPointA = pointA;
        this.ambientPointB = pointB;
        this.exitTarget = startAtA ? pointB : pointA;
        
        if (agent != null) 
        {
            // Tạo đích đến ngẫu nhiên trong bán kính 8 mét quanh điểm chốt để đi thành nhiều làn khác nhau
            Vector2 randomCircle = Random.insideUnitCircle * 8f;
            Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            
            Vector3 finalTargetPos = this.exitTarget.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(this.exitTarget.position + randomOffset, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                finalTargetPos = hit.position;
            }
            agent.SetDestination(finalTargetPos);
        }
    }

    private void Update()
    {
        if (animator != null)
        {
            if (agent.velocity.magnitude > 0.1f) 
            {
                animator.speed = 1f;
                animator.transform.localScale = originalBodyScale; // Reset khi đi
                
                // Ép NPC luôn quay mặt về hướng nó đang di chuyển (chống lỗi đi lùi / moonwalk)
                if (agent.velocity.sqrMagnitude > 0.01f)
                {
                    // Dùng steeringTarget (điểm đến tiếp theo) thay vì velocity để chống bị rung giật (jitter)
                    Vector3 moveDirection = (agent.steeringTarget - transform.position).normalized;
                    moveDirection.y = 0; // Giữ cho không bị ngửa mặt lên trời
                    if (moveDirection != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                    }
                }
            }
            else 
            {
                animator.speed = 0f;
                // Hiệu ứng "thở" (co giãn nhẹ) khi đứng yên, chỉ scale phần thân (animator) để không bị lỗi xuyên tường
                float breathe = Mathf.Sin(Time.time * 3f) * 0.015f;
                animator.transform.localScale = originalBodyScale + new Vector3(breathe, breathe, breathe);
            }
        }

        if (agent.pathPending) return;

        if (currentState == CustomerState.Wandering)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                wanderCount--;
                if (wanderCount <= 0)
                {
                    // Đi dạo chán rồi, rẽ vào tiệm thôi!
                    currentState = CustomerState.Visiting;
                    if (!storeLine.Contains(this)) storeLine.Add(this);
                }
                else
                {
                    // Đi dạo tiếp điểm khác
                    PickRandomWanderPoint();
                }
            }
        }
        else if (currentState == CustomerState.Visiting || currentState == CustomerState.ReturningForPickup)
        {
            UpdateQueuePosition();
            
            // Chỉ tương tác nếu đứng đầu hàng
            if (storeLine.IndexOf(this) == 0 && agent.remainingDistance <= 1.5f)
            {
                currentState = (currentState == CustomerState.Visiting) ? CustomerState.WaitingToNegotiate : CustomerState.WaitingForPickup;
            }
        }
        else if (currentState == CustomerState.AmbientWalking)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Quay đầu đi ngược lại!
                if (ambientPointA != null && ambientPointB != null)
                {
                    // Quay đầu đi thẳng về điểm kia
                    this.exitTarget = (this.exitTarget == ambientPointA) ? ambientPointB : ambientPointA;
                    
                    Vector2 randomCircle = Random.insideUnitCircle * 8f;
                    Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                    
                    Vector3 finalTargetPos = this.exitTarget.position;
                    if (UnityEngine.AI.NavMesh.SamplePosition(this.exitTarget.position + randomOffset, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        finalTargetPos = hit.position;
                    }
                    agent.SetDestination(finalTargetPos);
                }
            }
        }
        else if (currentState == CustomerState.Leaving && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateQueuePosition()
    {
        if (counterTarget == null) return;

        int myIndex = storeLine.IndexOf(this);
        if (myIndex == -1) 
        {
            storeLine.Add(this);
            myIndex = storeLine.Count - 1;
        }

        // Xếp hàng lùi về phía sau (ngược hướng nhìn của counter) cách nhau 1.5m
        Vector3 queueOffset = -counterTarget.forward * (myIndex * 1.5f);
        
        // Tránh bị dính vào tường (nếu quầy gần tường)
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.Raycast(counterTarget.position, counterTarget.position + queueOffset, out hit, UnityEngine.AI.NavMesh.AllAreas))
        {
            // Nếu bị vướng tường, xếp dạt sang ngang (right)
            queueOffset = counterTarget.right * (myIndex * 1.5f);
        }

        Vector3 exactQueuePos = counterTarget.position + queueOffset;

        // Nếu khách còn đang ở xa (hơn 10m) thì đi vào điểm lệch (approachOffset) để tản ra nhiều đường
        if (!hasConvergedToQueue && Vector3.Distance(transform.position, exactQueuePos) > 10f)
        {
            Vector3 approachPos = exactQueuePos + approachOffset;
            if (UnityEngine.AI.NavMesh.SamplePosition(approachPos, out UnityEngine.AI.NavMeshHit hitSample, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.SetDestination(hitSample.position);
            }
        }
        else
        {
            // Đã tới gần tiệm, bắt đầu đi thẳng vào hàng ngay ngắn
            hasConvergedToQueue = true;
            agent.SetDestination(exactQueuePos);
        }
    }

    private void OnDestroy()
    {
        if (storeLine.Contains(this)) storeLine.Remove(this);
    }

    public string GetInteractionPrompt()
    {
        if (currentState == CustomerState.WaitingToNegotiate && !hasInteracted) 
        {
            string npcName = archetype != null ? archetype.archetypeName : "Khách hàng";
            return $"Nói chuyện với {npcName}";
        }
        
        if (currentState == CustomerState.WaitingForPickup && !hasInteracted)
            return $"Trả đồ cho {(archetype != null ? archetype.archetypeName : "Khách hàng")}";
            
        return "";
    }

    public void Interact()
    {
        if (hasInteracted) return;

        if (currentState == CustomerState.WaitingToNegotiate)
        {
            hasInteracted = true;
            StartNegotiation();
        }
        else if (currentState == CustomerState.WaitingForPickup)
        {
            hasInteracted = true;
            ProcessPickup();
        }
    }

    private void StartNegotiation()
    {
        string greeting = (archetype != null && archetype.greetingDialogues != null && archetype.greetingDialogues.Count > 0)
            ? archetype.greetingDialogues[Random.Range(0, archetype.greetingDialogues.Count)] 
            : "Tôi có món đồ bị hỏng, sửa giúp tôi nhé!";

        if (DialogueUI.Instance != null)
        {
            string npcName = archetype != null ? archetype.archetypeName : "Khách hàng";
            DialogueUI.Instance.ShowDialogue(npcName, greeting, ShowNegotiationOptions);
        }
    }

    private void ShowNegotiationOptions()
    {
        // 1. Pick random item prefab
        if (possibleItemsToDrop != null && possibleItemsToDrop.Count > 0)
        {
            _selectedItemPrefab = possibleItemsToDrop[Random.Range(0, possibleItemsToDrop.Count)];
        }
        else
        {
            _selectedItemPrefab = itemPrefabToDrop;
        }

        // 2. Randomize properties
        _selectedMinigame = (Random.value > 0.5f) ? MinigameType.Soldering : MinigameType.Diagnosis;
        _selectedDifficulty = Random.Range(1, 4); // Độ khó 1, 2, 3
        _selectedBasePay = Random.Range(20, 101) * 1000f; // Giá 20k đến 100k

        string itemName = (archetype != null && archetype.preferredItems != null && archetype.preferredItems.Count > 0)
            ? archetype.preferredItems[Random.Range(0, archetype.preferredItems.Count)] 
            : "Đồ gia dụng";
        
        int currentDay = DayClock.Instance != null ? DayClock.Instance.CurrentDay : 1;
        float currentHour = DayClock.Instance != null ? DayClock.Instance.CurrentHour : 8f;
        int apptDay = currentHour > 15f ? currentDay + 1 : currentDay;
        float apptHour = currentHour > 15f ? 10f : currentHour + 4f;

        string offer = $"Bác thợ xem giúp em con {itemName} này. Công cán gửi bác {_selectedBasePay:N0} đ. Cứ thong thả làm, đến {apptHour:00}:00 ngày {apptDay} em qua lấy hàng.";
        
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowDialogue(
                archetype.archetypeName, 
                offer, 
                () => AcceptOrder(itemName, apptDay, apptHour), 
                () => RefuseOrder(), 
                "Nhận sửa", 
                "Từ chối"
            );
        }
    }

    private void RefuseOrder()
    {
        string angry = "Thế thì thôi vậy, tôi mang ra tiệm khác!";
        DialogueUI.Instance.ShowDialogue(archetype.archetypeName, angry, LeaveStore, null, "Đóng");
    }

    private void AcceptOrder(string itemName, int apptDay, float apptHour)
    {
        currentOrder = new CustomerOrder(archetype.archetypeName, itemName, archetype.personality, 1, _selectedBasePay, apptDay, apptHour);
        
        if (_selectedItemPrefab != null && itemDropPoint != null)
        {
            GameObject droppedItem = Instantiate(_selectedItemPrefab, itemDropPoint.position, itemDropPoint.rotation);
            RepairableItem repairable = droppedItem.GetComponentInChildren<RepairableItem>();
            if (repairable != null)
            {
                repairable.linkedOrder = currentOrder;
                repairable.SetRandomizedProperties(_selectedMinigame, _selectedDifficulty, _selectedBasePay);
                Debug.Log($"[CustomerController] Đã tạo món đồ. Minigame: {_selectedMinigame}, Độ khó: {_selectedDifficulty}, Giá: {_selectedBasePay}");
            }
        }

        if (CustomerQueue.Instance != null)
        {
            CustomerQueue.Instance.AddCustomer(currentOrder);
        }

        string agreement = "Cảm ơn, nhớ đúng hẹn nhé!";
        DialogueUI.Instance.ShowDialogue(archetype.archetypeName, agreement, LeaveStore, null, "Đóng");
    }

    private void ProcessPickup()
    {
        if (currentOrder == null)
        {
            LeaveStore();
            return;
        }

        if (currentOrder.isCompleted)
        {
            string satisfied = (archetype != null && archetype.satisfiedDialogues != null && archetype.satisfiedDialogues.Count > 0)
                ? archetype.satisfiedDialogues[Random.Range(0, archetype.satisfiedDialogues.Count)] 
                : "Cảm ơn cậu nhé, đồ sửa tốt lắm!";
            
            // Khách trả tiền
            EconomyManager eco = FindObjectOfType<EconomyManager>();
            if (eco != null) eco.AddCash(currentOrder.negotiatedPrice);
            
            if (ToastNotificationManager.Instance != null)
            {
                ToastNotificationManager.Instance.ShowToast($"Đã nhận {currentOrder.negotiatedPrice:N0} đ", 3f);
            }

            currentOrder.isPickedUp = true;
            CustomerQueue.Instance.CompleteOrder(currentOrder);
            
            // Xoá đồ vật trên bàn (cần tìm theo linkedOrder)
            RepairableItem[] allItems = FindObjectsOfType<RepairableItem>();
            foreach (var item in allItems)
            {
                if (item.linkedOrder == currentOrder)
                {
                    Destroy(item.gameObject);
                    break;
                }
            }

            DialogueUI.Instance.ShowDialogue(archetype.archetypeName, satisfied, LeaveStore);
        }
        else
        {
            if (currentOrder.personality == CustomerPersonality.Easygoing)
            {
                // Dễ tính: cho thêm thời gian
                currentOrder.appointmentDay += 1;
                DialogueUI.Instance.ShowDialogue(archetype.archetypeName, "Chưa xong à? Thôi cứ làm đi, mai tôi quay lại lấy.", LeaveStore);
                hasInteracted = false; // Reset for next time
            }
            else
            {
                // Khách khó tính: Tức giận, huỷ đơn, trừ danh tiếng
                string angry = (archetype != null && archetype.unsatisfiedDialogues != null && archetype.unsatisfiedDialogues.Count > 0)
                    ? archetype.unsatisfiedDialogues[Random.Range(0, archetype.unsatisfiedDialogues.Count)] 
                    : "Làm ăn chậm chạp quá, tôi lấy lại đồ!";
                
                currentOrder.isFailed = true;
                currentOrder.isPickedUp = true;
                
                if (CustomerQueue.Instance != null)
                {
                    CustomerQueue.Instance.ReduceReputation(5); // Trừ 5 uy tín
                    CustomerQueue.Instance.RemoveFailedOrder(currentOrder);
                }

                // TODO: Xoá đồ vật trên bàn (cần tìm theo linkedOrder)
                RepairableItem[] allItems = FindObjectsOfType<RepairableItem>();
                foreach (var item in allItems)
                {
                    if (item.linkedOrder == currentOrder)
                    {
                        Destroy(item.gameObject);
                        break;
                    }
                }

                DialogueUI.Instance.ShowDialogue(archetype.archetypeName, angry, LeaveStore);
            }
        }
    }

    private void LeaveStore()
    {
        if (storeLine.Contains(this)) storeLine.Remove(this);
        currentState = CustomerState.Leaving;
        hasInteracted = false;
        if (exitTarget != null)
        {
            agent.SetDestination(exitTarget.position);
        }
    }
}
