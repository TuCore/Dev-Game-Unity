using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class AmbientSpawner : MonoBehaviour
{
    [Header("NPC Settings")]
    public List<GameObject> npcPrefabs;
    public int maxSimultaneousAmbientNPCs = 6;
    
    [Header("Spawn Points")]
    [Tooltip("Điểm xuất phát bên trái phố")]
    public Transform leftSpawnPoint;
    [Tooltip("Điểm xuất phát bên phải phố")]
    public Transform rightSpawnPoint;
    
    private void Start()
    {
        if (npcPrefabs == null || npcPrefabs.Count == 0)
        {
            Debug.LogWarning("[AmbientSpawner] Chưa cài đặt NPC Prefabs!");
            return;
        }
        if (leftSpawnPoint == null || rightSpawnPoint == null)
        {
            Debug.LogWarning("[AmbientSpawner] Chưa kéo Left/Right Spawn Point!");
            return;
        }

        // Đẻ lần lượt toàn bộ những người trong mảng npcPrefabs ra phố
        StartCoroutine(SpawnAllAmbientNPCs());
    }

    private System.Collections.IEnumerator SpawnAllAmbientNPCs()
    {
        foreach (var prefab in npcPrefabs)
        {
            if (prefab != null)
            {
                SpawnSpecificAmbientNPC(prefab);
            }
            // Chờ 2 - 5 giây rồi mới đẻ người tiếp theo để khỏi bị đè lên nhau
            yield return new WaitForSeconds(Random.Range(2f, 5f));
        }
    }

    private void SpawnSpecificAmbientNPC(GameObject prefab)
    {
        bool leftToRight = Random.value > 0.5f;
        Transform spawnPt = leftToRight ? leftSpawnPoint : rightSpawnPoint;
        Transform targetPt = leftToRight ? rightSpawnPoint : leftSpawnPoint;
        
        // Tạo độ tản mát rộng lên 8 mét để lấp đầy lòng đường rộng
        Vector2 randomCircle = Random.insideUnitCircle * 8f;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        
        Vector3 finalSpawnPos = spawnPt.position;
        if (NavMesh.SamplePosition(spawnPt.position + randomOffset, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;
        }
        
        GameObject spawned = Instantiate(prefab, finalSpawnPos, spawnPt.rotation);
        spawned.name = "[AMBIENT] " + prefab.name; // Đổi tên để phân biệt với khách hàng
        
        CustomerController controller = spawned.GetComponent<CustomerController>();
        if (controller != null)
        {
            controller.SetAmbientWalker(leftSpawnPoint, rightSpawnPoint, leftToRight);
        }
    }
}
