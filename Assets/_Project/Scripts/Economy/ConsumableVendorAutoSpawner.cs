using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConsumableVendorAutoSpawner : MonoBehaviour
{
    private static ConsumableVendorAutoSpawner instance;

    [Header("Tự tạo điểm mua nếu scene chưa có")]
    [SerializeField] private bool spawnDefaultVendorsIfMissing = true;
    [SerializeField] private float distanceInFrontOfPlayer = 3.8f;
    [SerializeField] private float sideOffset = 2.2f;
    [SerializeField] private Vector3 defaultInteractionAreaSize = new Vector3(3.8f, 1.8f, 2.8f);
    [SerializeField] private Vector3 defaultInteractionAreaCenter = new Vector3(0f, 0.9f, 0f);
    [SerializeField] [Range(0f, 0.35f)] private float defaultInteractionAreaAlpha = 0.1f;

    [Header("Bánh mì mặc định")]
    [SerializeField] private float banhMiPrice = 12000f;
    [SerializeField] private float banhMiFatigueRecovery = 8f;
    [SerializeField] private float banhMiHungerRecovery = 35f;
    [SerializeField] private float banhMiThirstRecovery = 0f;

    [Header("Trà đá mặc định")]
    [SerializeField] private float traDaPrice = 8000f;
    [SerializeField] private float traDaFatigueRecovery = 12f;
    [SerializeField] private float traDaHungerRecovery = 0f;
    [SerializeField] private float traDaThirstRecovery = 38f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        EnsureInstance();
    }

    public static ConsumableVendorAutoSpawner EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<ConsumableVendorAutoSpawner>();
        if (instance != null)
        {
            return instance;
        }

        GameObject spawnerObject = new GameObject("ConsumableVendorAutoSpawner");
        instance = spawnerObject.AddComponent<ConsumableVendorAutoSpawner>();
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
            StartCoroutine(SpawnDefaultsAfterSceneSettles());
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
            StartCoroutine(SpawnDefaultsAfterSceneSettles());
        }
    }

    private IEnumerator SpawnDefaultsAfterSceneSettles()
    {
        yield return null;
        yield return new WaitForSeconds(0.15f);

        if (!spawnDefaultVendorsIfMissing || HasSceneVendor())
        {
            yield break;
        }

        Transform anchor = FindPlayerAnchor();
        Vector3 forward = anchor != null ? ProjectOnGround(anchor.forward) : Vector3.forward;
        Vector3 right = anchor != null ? ProjectOnGround(anchor.right) : Vector3.right;
        Vector3 basePosition = anchor != null
            ? anchor.position + (forward * distanceInFrontOfPlayer)
            : GetFallbackPosition(SceneManager.GetActiveScene().name);

        CreateVendor(
            "Vendor_BanhMi_Default",
            "BÁNH MÌ",
            ConsumableVendor.ConsumablePreset.BanhMi,
            "Bánh mì",
            banhMiPrice,
            banhMiFatigueRecovery,
            banhMiHungerRecovery,
            banhMiThirstRecovery,
            SnapToGround(basePosition - (right * sideOffset)),
            new Color(1f, 0.62f, 0.22f, 1f));

        CreateVendor(
            "Vendor_TraDa_Default",
            "TRÀ ĐÁ",
            ConsumableVendor.ConsumablePreset.TraDa,
            "Trà đá",
            traDaPrice,
            traDaFatigueRecovery,
            traDaHungerRecovery,
            traDaThirstRecovery,
            SnapToGround(basePosition + (right * sideOffset)),
            new Color(0.18f, 0.78f, 1f, 1f));
    }

    private bool HasSceneVendor()
    {
        ConsumableVendor[] vendors = FindObjectsByType<ConsumableVendor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Scene activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < vendors.Length; i++)
        {
            if (vendors[i] != null && vendors[i].gameObject.scene == activeScene)
            {
                return true;
            }
        }

        return false;
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
            return new Vector3(0f, 1f, 3.5f);
        }

        return new Vector3(119.4f, 1f, 14.8f);
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 4f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
        {
            position.y = hit.point.y + 0.05f;
        }

        return position;
    }

    private void CreateVendor(
        string objectName,
        string label,
        ConsumableVendor.ConsumablePreset preset,
        string itemName,
        float price,
        float fatigueRecovery,
        float hungerRecovery,
        float thirstRecovery,
        Vector3 position,
        Color color)
    {
        GameObject vendorObject = new GameObject(objectName);
        vendorObject.name = objectName;
        vendorObject.transform.position = position;

        ConsumableVendor vendor = vendorObject.AddComponent<ConsumableVendor>();
        vendor.Configure(preset, itemName, price, fatigueRecovery, hungerRecovery, thirstRecovery);
        vendor.ConfigureInteractionArea(defaultInteractionAreaSize, defaultInteractionAreaCenter, color, defaultInteractionAreaAlpha);

        CreateWorldLabel(vendorObject.transform, label, color);
    }

    private void CreateWorldLabel(Transform parent, string text, Color color)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        labelObject.transform.localScale = Vector3.one * 0.07f;

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 34;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.Lerp(color, Color.white, 0.25f);
    }

    private bool IsValidScene(string sceneName)
    {
        return sceneName == "Shop_Main" || sceneName == "VietnamStreet";
    }
}
