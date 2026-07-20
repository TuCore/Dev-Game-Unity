using UnityEngine;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> customerPrefabs;
    [Tooltip("Dùng Left/Right Spawn Point để spawn ở 2 đầu phố. Nếu để trống sẽ dùng Spawn Point cũ.")]
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public Transform spawnPoint; // Cũ
    public Transform counterTarget;
    public Transform exitTarget;
    public Transform itemDropPoint;

    [Header("Base Timers")]
    public float minSpawnDelay = 30f;
    public float maxSpawnDelay = 90f;
    public int maxSimultaneousNPCs = 3;

    private float nextSpawnTime;

    private bool HasAnySpawnPoint => spawnPoint != null || (leftSpawnPoint != null && rightSpawnPoint != null);

    private void Start()
    {
        // Khởi tạo khách đầu tiên xuất hiện NGAY LẬP TỨC khi load Scene
        SpawnWanderingCustomer();
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (DayClock.Instance != null && !DayClock.Instance.IsRunning) return;

        // Debug override
        if (CustomInputManager.GetKeyDown("Secondary"))
        {
            Debug.Log("[DEBUG] Force spawning customer via F key!");
            SpawnWanderingCustomer();
        }

        // 1. Check for returning customers
        if (DayClock.Instance != null && CustomerQueue.Instance != null)
        {
            int day = DayClock.Instance.CurrentDay;
            float hour = DayClock.Instance.CurrentHour;

            foreach (var order in CustomerQueue.Instance.ActiveOrders)
            {
                if (order == null || !order.IsAppointmentDue(day, hour))
                {
                    continue;
                }

                if (order.hasSpawnedReturning && !IsReturningCustomerAlive(order))
                {
                    order.hasSpawnedReturning = false;
                }

                if (!order.hasSpawnedReturning)
                {
                    // Check limit before spawning returning customers.
                    // Ambient street walkers also use CustomerController, but should not block real customers.
                    int activeCustomerLimit = order.isCompleted ? maxSimultaneousNPCs + 2 : maxSimultaneousNPCs;
                    if (CountActiveShopCustomers() < activeCustomerLimit)
                    {
                        SpawnReturningCustomer(order);
                    }
                }
            }
        }

        // 2. Check for new wandering customers
        if (Time.time >= nextSpawnTime)
        {
            Debug.Log($"[DEBUG] Timer reached! Attempting to spawn at {Time.time}");
            if (!HasDueReturningOrder())
            {
                SpawnWanderingCustomer();
            }
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        float repFactor = 1f;
        if (CustomerQueue.Instance != null)
        {
            // Lower reputation = longer delay
            float rep = CustomerQueue.Instance.currentReputation;
            if (rep < 50) repFactor = 1f + ((50f - rep) / 50f); // Max 2x delay at 0 rep
        }

        float delay = Random.Range(minSpawnDelay, maxSpawnDelay) * repFactor;
        nextSpawnTime = Time.time + delay;
    }

    private void SpawnWanderingCustomer()
    {
        Debug.Log("[DEBUG] SpawnWanderingCustomer called!");
        if (customerPrefabs.Count == 0 || !HasAnySpawnPoint || counterTarget == null || exitTarget == null) return;

        CustomerController[] currentControllers = FindCurrentCustomerControllers();

        // Kiểm tra giới hạn khách thật. Không tính NPC nền đi ngoài phố.
        if (CountActiveShopCustomers(currentControllers) >= maxSimultaneousNPCs)
        {
            return;
        }

        // Disabled: pending repair orders should not stop new visitors from entering the street.
        if (ShouldBlockNewVisitorsForPendingOrders())
        {
            return; // Đã kín chỗ
        }

        // Thu thập danh sách các loại khách (đang mua đồ/trong tiệm)
        HashSet<string> activeArchetypes = new HashSet<string>();
        foreach (var ctrl in currentControllers)
        {
            if (ctrl != null && IsSpawnBlockingCustomer(ctrl))
            {
                string id = (ctrl.archetype != null && !string.IsNullOrEmpty(ctrl.archetype.archetypeName)) 
                    ? ctrl.archetype.archetypeName 
                    : ctrl.gameObject.name.Replace("(Clone)", "").Trim();
                activeArchetypes.Add(id);
            }
        }

        // Lọc ra các prefab hợp lệ (chưa xuất hiện trên scene)
        List<GameObject> availablePrefabs = new List<GameObject>();
        foreach (var prefab in customerPrefabs)
        {
            if (prefab != null)
            {
                CustomerController prefController = prefab.GetComponent<CustomerController>();
                if (prefController != null)
                {
                    string id = (prefController.archetype != null && !string.IsNullOrEmpty(prefController.archetype.archetypeName)) 
                        ? prefController.archetype.archetypeName 
                        : prefab.name.Replace("(Clone)", "").Trim();

                    // Nếu nhân vật này CHƯA có mặt trên phố, thì mới cho vào danh sách bốc thăm
                    if (!activeArchetypes.Contains(id))
                    {
                        availablePrefabs.Add(prefab);
                    }
                }
            }
        }

        if (availablePrefabs.Count == 0)
        {
            return;
        }

        Debug.Log("[DEBUG] Conditions met! Instantiating prefab...");
        GameObject selectedPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];

        Transform chosenSpawn = spawnPoint;
        if (leftSpawnPoint != null && rightSpawnPoint != null)
        {
            chosenSpawn = Random.value > 0.5f ? leftSpawnPoint : rightSpawnPoint;
        }

        Vector3 finalSpawnPos = chosenSpawn.position;
        UnityEngine.AI.NavMeshHit hit;
        // Lệch rộng ra 8 mét để lấp đầy lòng đường
        Vector2 randomCircle = Random.insideUnitCircle * 8f;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        if (UnityEngine.AI.NavMesh.SamplePosition(chosenSpawn.position + randomOffset, out hit, 15f, UnityEngine.AI.NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;
        }

        GameObject spawned = Instantiate(selectedPrefab, finalSpawnPos, chosenSpawn.rotation);

        CustomerController controller = spawned.GetComponent<CustomerController>();
        if (controller != null)
        {
            controller.counterTarget = counterTarget;
            controller.exitTarget = exitTarget;
            controller.itemDropPoint = itemDropPoint;
            // They start as wandering by default
        }
    }

    private void SpawnReturningCustomer(CustomerOrder order)
    {
        if (customerPrefabs == null || customerPrefabs.Count == 0 || !HasAnySpawnPoint || counterTarget == null || exitTarget == null) return;

        GameObject prefab = FindReturningCustomerPrefab(order);
        if (prefab == null)
        {
            return;
        }

        Transform chosenSpawn = spawnPoint;
        if (leftSpawnPoint != null && rightSpawnPoint != null)
        {
            chosenSpawn = Random.value > 0.5f ? leftSpawnPoint : rightSpawnPoint;
        }

        Vector3 finalSpawnPos = chosenSpawn.position;
        UnityEngine.AI.NavMeshHit hit;
        Vector3 randomOffset = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        if (UnityEngine.AI.NavMesh.SamplePosition(chosenSpawn.position + randomOffset, out hit, 15f, UnityEngine.AI.NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;
        }

        GameObject spawned = Instantiate(prefab, finalSpawnPos, chosenSpawn.rotation);
        CustomerController controller = spawned.GetComponent<CustomerController>();
        if (controller != null)
        {
            controller.counterTarget = counterTarget;
            controller.exitTarget = exitTarget;
            controller.itemDropPoint = itemDropPoint;
            controller.SetReturningOrder(order);
            order.hasSpawnedReturning = true; // Mark as spawned so it doesn't spawn again next frame!
        }
    }

    private GameObject FindReturningCustomerPrefab(CustomerOrder order)
    {
        List<GameObject> validPrefabs = new List<GameObject>();
        for (int i = 0; i < customerPrefabs.Count; i++)
        {
            GameObject prefab = customerPrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            CustomerController controller = prefab.GetComponent<CustomerController>();
            if (controller == null)
            {
                continue;
            }

            validPrefabs.Add(prefab);
            string archetypeName = controller.archetype != null ? controller.archetype.archetypeName : "";
            if (!string.IsNullOrWhiteSpace(archetypeName) && order != null && archetypeName == order.customerName)
            {
                return prefab;
            }
        }

        return validPrefabs.Count > 0 ? validPrefabs[Random.Range(0, validPrefabs.Count)] : null;
    }

    private bool HasDueReturningOrder()
    {
        if (DayClock.Instance == null || CustomerQueue.Instance == null)
        {
            return false;
        }

        int day = DayClock.Instance.CurrentDay;
        float hour = DayClock.Instance.CurrentHour;
        List<CustomerOrder> orders = CustomerQueue.Instance.ActiveOrders;
        for (int i = 0; i < orders.Count; i++)
        {
            CustomerOrder order = orders[i];
            if (order != null && order.IsAppointmentDue(day, hour) && !order.isPickedUp && !order.isFailed)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsReturningCustomerAlive(CustomerOrder order)
    {
        if (order == null)
        {
            return false;
        }

        CustomerController[] controllers = FindCurrentCustomerControllers();
        for (int i = 0; i < controllers.Length; i++)
        {
            CustomerController controller = controllers[i];
            if (controller != null && controller.IsHandlingReturningOrder(order))
            {
                return true;
            }
        }

        return false;
    }

    private int CountActiveShopCustomers(CustomerController[] controllers = null)
    {
        if (controllers == null)
        {
            controllers = FindCurrentCustomerControllers();
        }

        int count = 0;
        foreach (var controller in controllers)
        {
            if (controller == null || !IsSpawnBlockingCustomer(controller))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private bool IsSpawnBlockingCustomer(CustomerController controller)
    {
        return controller.currentState != CustomerState.AmbientWalking
            && controller.currentState != CustomerState.Leaving;
    }

    private bool ShouldBlockNewVisitorsForPendingOrders()
    {
        return false;
    }

    private CustomerController[] FindCurrentCustomerControllers()
    {
        return FindObjectsByType<CustomerController>(FindObjectsSortMode.None);
    }
}

