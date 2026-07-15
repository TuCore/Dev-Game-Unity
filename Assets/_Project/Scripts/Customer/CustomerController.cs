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
    WaitingForPickup
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
    private CustomerState currentState = CustomerState.Wandering;
    private bool hasInteracted = false;
    
    // Configurable wandering points around the street
    private Transform[] wanderPoints;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Tăng stopping distance để NPC không dồn cục
        agent.stoppingDistance = 1.5f;
        agent.avoidancePriority = Random.Range(30, 70); // Mỗi NPC ưu tiên tránh nhau khác nhau
        
        // Default to visiting for now if spawned specifically for store
        // If it's a returning customer, the Spawner will override this state.
        if (currentState != CustomerState.ReturningForPickup)
        {
            // Tạm thời để 100% khách vào tiệm để dễ test
            if (Random.value > -1f) // Bỏ cái 50% đi
            {
                currentState = CustomerState.Visiting;
                if (counterTarget != null)
                {
                    // Thêm offset ngẫu nhiên để NPC không đứng chồng lên nhau
                    Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-1f, 1f));
                    agent.SetDestination(counterTarget.position + offset);
                }
            }
        }
    }

    public void SetReturningOrder(CustomerOrder order)
    {
        this.currentOrder = order;
        this.currentState = CustomerState.ReturningForPickup;
        if (counterTarget != null) agent.SetDestination(counterTarget.position);
    }

    private void Update()
    {
        // Xử lý dừng animation (Moonwalk)
        if (animator != null)
        {
            if (agent.velocity.magnitude > 0.1f) animator.speed = 1f;
            else animator.speed = 0f;
        }

        if (agent.pathPending) return;

        // Tăng khoảng cách dừng để khách không bị kẹt vào bàn
        if (currentState == CustomerState.Visiting && (!agent.pathPending && agent.remainingDistance <= 1.5f))
        {
            currentState = CustomerState.WaitingToNegotiate;
        }
        else if (currentState == CustomerState.ReturningForPickup && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = CustomerState.WaitingForPickup;
        }
        else if (currentState == CustomerState.Leaving && agent.remainingDistance <= agent.stoppingDistance)
        {
            Destroy(gameObject); // Khách rời khỏi màn hình
        }
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

        // 2. Randomize properties - Tỷ lệ: Nối dây 50%, Khám bệnh/Dò mạch 30%, Hàn mạch 20%
        float roll = Random.value;
        if (roll < 0.5f)
        {
            _selectedMinigame = MinigameType.Rewiring; // 50% (0.0 đến < 0.5)
        }
        else if (roll < 0.8f)
        {
            _selectedMinigame = MinigameType.Diagnosis; // 30% (0.5 đến < 0.8)
        }
        else
        {
            _selectedMinigame = MinigameType.Soldering; // 20% (0.8 đến 1.0)
        }
        _selectedDifficulty = Random.Range(1, 4); // Độ khó 1, 2, 3
        _selectedBasePay = Random.Range(20, 101) * 1000f; // Giá 20k đến 100k

        string itemName = (archetype != null && archetype.preferredItems != null && archetype.preferredItems.Count > 0)
            ? archetype.preferredItems[Random.Range(0, archetype.preferredItems.Count)] 
            : "Đồ gia dụng";
        
        int currentDay = DayClock.Instance != null ? DayClock.Instance.CurrentDay : 1;
        float currentHour = DayClock.Instance != null ? DayClock.Instance.CurrentHour : 8f;
        int apptDay = currentHour > 15f ? currentDay + 1 : currentDay;
        float apptHour = currentHour > 15f ? 10f : currentHour + 4f;

        string offer = $"Anh sửa món {itemName} này giúp tôi. Tôi gửi {_selectedBasePay:N0} đ. Lúc {apptHour:00}:00 ngày {apptDay} tôi quay lại lấy nhé.";
        
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
        currentState = CustomerState.Leaving;
        hasInteracted = false;
        if (exitTarget != null)
        {
            agent.SetDestination(exitTarget.position);
        }
    }
}
