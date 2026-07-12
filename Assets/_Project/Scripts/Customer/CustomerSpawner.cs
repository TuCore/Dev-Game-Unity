using UnityEngine;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> customerPrefabs;
    public Transform spawnPoint;
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
        ScheduleNextSpawn();
    }

    private void Update()
    {
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
        if (customerPrefabs.Count == 0 || spawnPoint == null || counterTarget == null || exitTarget == null) return;

        // Kiểm tra giới hạn số lượng NPC vật lý trên Scene
        if (FindObjectsOfType<CustomerController>().Length >= maxSimultaneousNPCs)
        {
            return;
        }

        if (CustomerQueue.Instance != null && CustomerQueue.Instance.ActiveOrderCount >= CustomerQueue.Instance.MaxCustomers)
        {
            return; // Đã kín chỗ
        }

        GameObject prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
        GameObject spawned = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        
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

        GameObject spawned = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
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
