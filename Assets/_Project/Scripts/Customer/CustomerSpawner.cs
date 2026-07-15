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

    private void Start()
    {
        // Khởi tạo khách đầu tiên xuất hiện NGAY LẬP TỨC khi load Scene
        SpawnWanderingCustomer();
        ScheduleNextSpawn();
    }

    private void Update()
    {
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
                if (order.IsAppointmentDue(day, hour) && !order.hasSpawnedReturning)
                {
                    // Check limit before spawning returning customers
                    if (FindObjectsOfType<CustomerController>().Length < maxSimultaneousNPCs)
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
            SpawnWanderingCustomer();
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
        if (customerPrefabs.Count == 0 || spawnPoint == null || counterTarget == null || exitTarget == null) return;

        CustomerController[] currentControllers = FindObjectsOfType<CustomerController>();

        // Kiểm tra giới hạn số lượng NPC vật lý trên Scene
        if (currentControllers.Length >= maxSimultaneousNPCs)
        {
            return;
        }

        if (CustomerQueue.Instance != null && CustomerQueue.Instance.ActiveOrderCount >= CustomerQueue.Instance.MaxCustomers)
        {
            return; // Đã kín chỗ
        }

        // Thu thập danh sách các loại khách (đang mua đồ/trong tiệm)
        HashSet<string> activeArchetypes = new HashSet<string>();
        foreach (var ctrl in currentControllers)
        {
            if (ctrl != null && ctrl.currentState != CustomerState.AmbientWalking)
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
        else if (spawnPoint == null)
        {
            return;
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

        GameObject spawned = Instantiate(selectedPrefab, finalSpawnPos, spawnPoint.rotation);

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
        if (customerPrefabs.Count == 0 || spawnPoint == null || counterTarget == null || exitTarget == null) return;

        // Find matching prefab (simplified: just random for now if archetype name matching is too complex)
        GameObject prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
        foreach (var p in customerPrefabs)
        {
            if (p.GetComponent<CustomerController>()?.archetype.archetypeName == order.customerName)
            {
                prefab = p;
                break;
            }
        }

        Transform chosenSpawn = spawnPoint;
        if (leftSpawnPoint != null && rightSpawnPoint != null)
        {
            chosenSpawn = Random.value > 0.5f ? leftSpawnPoint : rightSpawnPoint;
        }
        else if (spawnPoint == null) return;

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
}

