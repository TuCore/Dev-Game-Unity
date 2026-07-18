using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConsumableVendorAutoSpawner : MonoBehaviour
{
    private static ConsumableVendorAutoSpawner instance;

    public static ConsumableVendorAutoSpawner EnsureInstance()
    {
        instance = FindFirstObjectByType<ConsumableVendorAutoSpawner>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }
}
