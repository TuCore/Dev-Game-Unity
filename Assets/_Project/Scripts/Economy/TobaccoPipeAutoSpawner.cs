using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TobaccoPipeAutoSpawner : MonoBehaviour
{
    private static TobaccoPipeAutoSpawner instance;

    [Header("Tự tạo điểm thuốc lào nếu scene chưa có")]
    [SerializeField] private bool spawnIfMissing = false;
    [SerializeField] private float distanceInFrontOfPlayer = 4.4f;
    [SerializeField] private float sideOffset = 0f;
    [SerializeField] private Vector3 defaultInteractionAreaSize = new Vector3(3.4f, 1.7f, 2.8f);
    [SerializeField] private Vector3 defaultInteractionAreaCenter = new Vector3(0f, 0.92f, 0f);
    [SerializeField] [Range(0f, 0.35f)] private float defaultInteractionAreaAlpha = 0.1f;

    [Header("Cấu hình mặc định")]
    [SerializeField] private float defaultPrice = 5000f;
    [SerializeField] private float defaultCooldownSeconds = 180f;

    public static TobaccoPipeAutoSpawner EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<TobaccoPipeAutoSpawner>();
        if (instance != null)
        {
            return instance;
        }

        GameObject spawnerObject = new GameObject("TobaccoPipeAutoSpawner");
        instance = spawnerObject.AddComponent<TobaccoPipeAutoSpawner>();
        DontDestroyOnLoad(spawnerObject);
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
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (IsValidScene(SceneManager.GetActiveScene().name))
        {
            StartCoroutine(SpawnAfterSceneSettles());
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsValidScene(scene.name))
        {
            StartCoroutine(SpawnAfterSceneSettles());
        }
    }

    private IEnumerator SpawnAfterSceneSettles()
    {
        yield return null;
        yield return new WaitForSeconds(0.18f);

        if (!spawnIfMissing || HasSceneStation())
        {
            yield break;
        }

        Transform anchor = FindPlayerAnchor();
        Vector3 forward = anchor != null ? ProjectOnGround(anchor.forward) : Vector3.forward;
        Vector3 right = anchor != null ? ProjectOnGround(anchor.right) : Vector3.right;
        Vector3 basePosition = anchor != null
            ? anchor.position + (forward * distanceInFrontOfPlayer) + (right * sideOffset)
            : GetFallbackPosition(SceneManager.GetActiveScene().name);

        CreateStation(SnapToGround(basePosition));
    }

    private bool HasSceneStation()
    {
        TobaccoPipeStation[] stations = FindObjectsByType<TobaccoPipeStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Scene activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] != null && stations[i].gameObject.scene == activeScene)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateStation(Vector3 position)
    {
        GameObject stationObject = new GameObject("TobaccoPipeStation_Default");
        stationObject.transform.position = position;

        TobaccoPipeStation station = stationObject.AddComponent<TobaccoPipeStation>();
        station.ConfigureStation("Điếu cày", defaultPrice, defaultCooldownSeconds);
        station.ConfigureInteractionArea(defaultInteractionAreaSize, defaultInteractionAreaCenter, new Color(0.86f, 0.95f, 0.5f, 1f), defaultInteractionAreaAlpha);
    }

    private Transform FindPlayerAnchor()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    private Vector3 ProjectOnGround(Vector3 value)
    {
        value.y = 0f;
        return value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.forward;
    }

    private Vector3 GetFallbackPosition(string sceneName)
    {
        if (sceneName == "Shop_Main")
        {
            return new Vector3(0f, 0.05f, 4.1f);
        }

        return new Vector3(119.4f, 0.05f, 14.8f);
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 14f, ~0, QueryTriggerInteraction.Ignore))
        {
            position.y = hit.point.y + 0.05f;
        }

        return position;
    }

    private bool IsValidScene(string sceneName)
    {
        return sceneName == "Shop_Main" || sceneName == "VietnamStreet";
    }
}
