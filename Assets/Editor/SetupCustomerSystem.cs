using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tool tự động sửa toàn bộ hệ thống Customer trên Scene.
/// Chạy từ menu: Tools > Setup Customer System
/// </summary>
public class SetupCustomerSystem : Editor
{
    [MenuItem("Tools/Setup Customer System")]
    public static void Setup()
    {
        Debug.Log("=== BẮT ĐẦU SETUP CUSTOMER SYSTEM ===");

        // ==============================
        // BƯỚC 1: Xử lý CustomerManager
        // ==============================
        GameObject customerManager = GameObject.Find("CustomerManager");
        if (customerManager == null)
        {
            customerManager = new GameObject("CustomerManager");
            Debug.Log("[Setup] Tạo mới GameObject CustomerManager");
        }
        else
        {
            Debug.Log("[Setup] Tìm thấy CustomerManager, xóa sạch component cũ...");
            // Xóa TẤT CẢ MonoBehaviour cũ (kể cả missing script)
            var allComponents = customerManager.GetComponents<Component>();
            foreach (var comp in allComponents)
            {
                if (comp == null || (comp is MonoBehaviour && !(comp is Transform)))
                {
                    if (comp == null)
                    {
                        Debug.Log("[Setup] Xóa missing script component");
                    }
                    else
                    {
                        Debug.Log($"[Setup] Xóa component: {comp.GetType().Name}");
                        DestroyImmediate(comp);
                    }
                }
            }
            // Xóa missing scripts (component == null)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(customerManager);
        }

        // ==============================
        // BƯỚC 2: Gắn các script mới
        // ==============================

        // 2a. CustomerQueue
        var queue = customerManager.GetComponent<CustomerQueue>();
        if (queue == null)
        {
            queue = customerManager.AddComponent<CustomerQueue>();
            Debug.Log("[Setup] ✅ Đã gắn CustomerQueue");
        }

        // 2b. CustomerSpawner
        var spawner = customerManager.GetComponent<CustomerSpawner>();
        if (spawner == null)
        {
            spawner = customerManager.AddComponent<CustomerSpawner>();
            Debug.Log("[Setup] ✅ Đã gắn CustomerSpawner");
        }

        // 2c. DayClock
        var clock = customerManager.GetComponent<DayClock>();
        if (clock == null)
        {
            clock = customerManager.AddComponent<DayClock>();
            Debug.Log("[Setup] ✅ Đã gắn DayClock");
        }

        // 2d. EconomyManager
        var economy = customerManager.GetComponent<EconomyManager>();
        if (economy == null)
        {
            economy = customerManager.AddComponent<EconomyManager>();
            Debug.Log("[Setup] ✅ Đã gắn EconomyManager");
        }

        // 2e. ToastNotificationManager
        var toast = customerManager.GetComponent<ToastNotificationManager>();
        if (toast == null)
        {
            toast = customerManager.AddComponent<ToastNotificationManager>();
            Debug.Log("[Setup] ✅ Đã gắn ToastNotificationManager");
        }

        // ==============================
        // BƯỚC 3: Tìm và gắn reference cho Spawner
        // ==============================
        // SpawnPoint: Vị trí khách xuất hiện (xa bên trái đường, ngoài tầm nhìn camera)
        Transform spawnPoint = FindOrCreate("SpawnPoint", new Vector3(116.384f, 0.077f, 12.74f));
        
        // CounterPoint: Vị trí khách đứng trước cửa tiệm để nói chuyện
        // Shop_Main ở x=136, cửa tiệm offset -4.4 → cửa ở khoảng x=131.6
        // NPC đứng trước cửa (lùi ra ngoài 1m theo trục Z)
        Transform counterPoint = FindOrCreate("CounterPoint", new Vector3(131.6f, 0.077f, 16.5f));
        
        // ExitPoint: Vị trí khách đi ra (xa bên phải đường)
        Transform exitPoint = FindOrCreate("ExitPoint", new Vector3(159.3f, 0.077f, 12.74f));
        
        // ItemDropPoint: Vị trí đặt đồ sửa, ngay trước cửa tiệm (cạnh CounterPoint)
        Transform itemDropPoint = FindOrCreate("ItemDropPoint", new Vector3(132.5f, 0.077f, 16.5f));

        spawner.spawnPoint = spawnPoint;
        spawner.counterTarget = counterPoint;
        spawner.exitTarget = exitPoint;
        spawner.itemDropPoint = itemDropPoint;
        spawner.minSpawnDelay = 10f;
        spawner.maxSpawnDelay = 30f;
        spawner.maxSimultaneousNPCs = 3;
        Debug.Log("[Setup] ✅ Đã gắn SpawnPoint, CounterPoint, ExitPoint, ItemDropPoint");

        // 3a. Tìm Customer Prefabs
        if (spawner.customerPrefabs == null || spawner.customerPrefabs.Count == 0)
        {
            spawner.customerPrefabs = new List<GameObject>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/NPC" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<CustomerController>() != null)
                {
                    spawner.customerPrefabs.Add(prefab);
                    Debug.Log($"[Setup] ✅ Tìm thấy Prefab khách hàng: {prefab.name}");
                }
            }
            if (spawner.customerPrefabs.Count == 0)
            {
                Debug.LogWarning("[Setup] ⚠️ Không tìm thấy Prefab NPC nào có CustomerController!");
            }
        }

        // ==============================
        // BƯỚC 4: Kiểm tra NavMesh
        // ==============================
        GameObject navObj = GameObject.Find("NavMesh");
        if (navObj != null)
        {
            Debug.Log("[Setup] ✅ NavMesh GameObject đã có trên Scene");
        }
        else
        {
            Debug.LogWarning("[Setup] ⚠️ Chưa có NavMesh! Khách sẽ không đi được. Hãy tạo NavMesh và Bake.");
        }

        // ==============================
        // BƯỚC 5: Kiểm tra DialogueUI
        // ==============================
        var dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogueUI == null)
        {
            Debug.LogWarning("[Setup] ⚠️ Chưa có DialogueUI trên Scene! Khách sẽ không nói chuyện được.");
        }
        else
        {
            Debug.Log("[Setup] ✅ DialogueUI đã có");
        }

        // ==============================
        // BƯỚC 6: Đánh dấu dirty để lưu
        // ==============================
        EditorUtility.SetDirty(customerManager);
        EditorUtility.SetDirty(spawner);
        EditorUtility.SetDirty(queue);
        EditorUtility.SetDirty(clock);

        Debug.Log("=== SETUP CUSTOMER SYSTEM HOÀN TẤT ===");
        
        string report = "Setup Customer System hoàn tất!\n\n";
        report += $"• CustomerQueue: ✅\n";
        report += $"• CustomerSpawner: ✅ ({spawner.customerPrefabs.Count} prefabs)\n";
        report += $"• DayClock: ✅\n";
        report += $"• EconomyManager: ✅\n";
        report += $"• ToastNotificationManager: ✅\n";
        report += $"• SpawnPoint: {spawnPoint.position}\n";
        report += $"• CounterPoint: {counterPoint.position}\n";
        report += $"• ExitPoint: {exitPoint.position}\n";
        report += $"• ItemDropPoint: {itemDropPoint.position}\n";
        report += $"\nMin Spawn Delay: {spawner.minSpawnDelay}s\n";
        report += $"Max Spawn Delay: {spawner.maxSpawnDelay}s\n";
        report += $"Max NPCs: {spawner.maxSimultaneousNPCs}\n";
        report += $"\n⚠️ Nhớ bấm Ctrl+S để lưu Scene!";
        report += $"\n⚠️ Nhớ Bake NavMesh nếu khách không đi được!";

        EditorUtility.DisplayDialog("Setup Customer System", report, "OK");
    }

    private static Transform FindOrCreate(string name, Vector3 defaultPosition)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = new GameObject(name);
            Debug.Log($"[Setup] Tạo mới {name} tại {defaultPosition}");
        }
        else
        {
            Debug.Log($"[Setup] Cập nhật vị trí {name}: {obj.transform.position} → {defaultPosition}");
        }
        obj.transform.position = defaultPosition;
        EditorUtility.SetDirty(obj);
        return obj.transform;
    }
}
