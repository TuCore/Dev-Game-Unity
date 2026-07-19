using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class ConsumableVendor : MonoBehaviour, IInteractable
{
    private const string InteractionAreaObjectName = "ConsumableVendor_InteractionArea";

    public enum ConsumablePreset
    {
        BanhMi,
        TraDa,
        Custom
    }

    [Header("Món bán")]
    [SerializeField] private ConsumablePreset preset = ConsumablePreset.BanhMi;
    [SerializeField] private string itemName = "Bánh mì";
    [SerializeField] private float price = 12000f;

    [Header("Lượng hồi phục")]
    [SerializeField] private float fatigueRecovery = 8f;
    [SerializeField] private float hungerRecovery = 35f;
    [SerializeField] private float thirstRecovery = 0f;

    [Header("Tương tác")]
    [SerializeField] private bool requireNeedMissing = true;
    [SerializeField] private float purchaseCooldown = 0.35f;

    [Header("Vùng mua hàng")]
    [SerializeField] private Vector3 interactionAreaSize = new Vector3(3.6f, 1.8f, 2.4f);
    [SerializeField] private Vector3 interactionAreaCenter = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private bool showTransparentInteractionArea = true;
    [SerializeField] private bool showInteractionAreaWhilePlaying = false;
    [SerializeField] [Range(0f, 0.35f)] private float interactionAreaAlpha = 0.1f;
    [SerializeField] private Color interactionAreaColor = new Color(0.35f, 0.95f, 0.85f, 1f);

    private float _nextPurchaseTime;

    public string ItemName => itemName;
    public float Price => price;
    public float FatigueRecovery => fatigueRecovery;
    public float HungerRecovery => hungerRecovery;
    public float ThirstRecovery => thirstRecovery;

    public void Configure(ConsumablePreset newPreset, string newItemName, float newPrice, float newFatigueRecovery, float newHungerRecovery, float newThirstRecovery)
    {
        preset = newPreset;
        itemName = newItemName;
        price = Mathf.Max(0f, newPrice);
        fatigueRecovery = Mathf.Max(0f, newFatigueRecovery);
        hungerRecovery = Mathf.Max(0f, newHungerRecovery);
        thirstRecovery = Mathf.Max(0f, newThirstRecovery);
        EnsureInteractionArea();
    }

    public void ConfigureInteractionArea(Vector3 areaSize, Vector3 areaCenter, Color areaColor, float alpha)
    {
        interactionAreaSize = new Vector3(Mathf.Max(0.2f, areaSize.x), Mathf.Max(0.2f, areaSize.y), Mathf.Max(0.2f, areaSize.z));
        interactionAreaCenter = areaCenter;
        interactionAreaColor = areaColor;
        interactionAreaAlpha = Mathf.Clamp(alpha, 0f, 0.35f);
        showTransparentInteractionArea = true;
        EnsureInteractionArea();
    }

    private void Reset()
    {
        ApplyPresetDefaults();
        EnsureInteractionArea();
    }

    private void Awake()
    {
        string normalizedName = string.IsNullOrEmpty(itemName) ? "" : itemName.ToLowerInvariant();
        if (preset == ConsumablePreset.TraDa || normalizedName.Contains("trà") || normalizedName.Contains("tra"))
        {
            // Tự động thu nhỏ vùng chọn của ấm trà vì mô hình ấm trà rất nhỏ
            interactionAreaSize = new Vector3(0.5f, 0.5f, 0.5f);
            interactionAreaCenter = new Vector3(0f, 0.15f, 0f);
        }
        EnsureInteractionArea();
    }

    public void ApplyPresetDefaults()
    {
        switch (preset)
        {
            case ConsumablePreset.BanhMi:
                itemName = "Bánh mì";
                price = 12000f;
                fatigueRecovery = 8f;
                hungerRecovery = 35f;
                thirstRecovery = 0f;
                break;
            case ConsumablePreset.TraDa:
                itemName = "Trà đá";
                price = 8000f;
                fatigueRecovery = 12f;
                hungerRecovery = 0f;
                thirstRecovery = 38f;
                break;
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Nhấn [E] mua {itemName} ({price:N0} VNĐ)\n<color=#D7F8FF>{BuildRecoveryText()}</color>";
    }

    public void Interact()
    {
        if (Time.time < _nextPurchaseTime)
        {
            return;
        }

        _nextPurchaseTime = Time.time + purchaseCooldown;

        PlayerNeeds needs = PlayerNeeds.EnsureInstance();
        if (requireNeedMissing && !needs.WouldRecoverAny(fatigueRecovery, hungerRecovery, thirstRecovery))
        {
            ShowToast("Bạn đang ổn rồi, chưa cần mua thêm.");
            return;
        }

        EconomyManager economy = EconomyManager.Instance != null ? EconomyManager.Instance : FindFirstObjectByType<EconomyManager>();
        if (economy == null)
        {
            ShowToast("Không tìm thấy hệ thống tiền.");
            return;
        }

        if (!economy.SpendCash(price))
        {
            ShowToast($"Không đủ tiền mua {itemName}. Cần {price:N0} VNĐ.");
            return;
        }

        needs.RecoverNeeds(fatigueRecovery, hungerRecovery, thirstRecovery);
        ShowToast($"Đã mua {itemName}. {BuildRecoveryText()}");

        PlayPurchaseSound();
    }

    private void PlayPurchaseSound()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        string normalizedName = string.IsNullOrEmpty(itemName) ? "" : itemName.ToLowerInvariant();
        if (preset == ConsumablePreset.TraDa || normalizedName.Contains("trà") || normalizedName.Contains("tra"))
        {
            AudioClip teaClip = Resources.Load<AudioClip>("Audio/SFX/tieng_uong_tra");
            if (teaClip != null)
            {
                AudioManager.Instance.PlaySFX(teaClip, 0.9f, 1f);
            }
            else
            {
                AudioManager.Instance.PlaySFX("tieng_uong_tra", 0.9f, 1f);
            }
            return;
        }

        AudioManager.Instance.PlaySFX("Tiếng đặt đồ");
    }

    private string BuildRecoveryText()
    {
        StringBuilder builder = new StringBuilder("Hồi ");
        bool hasAny = false;
        AppendRecovery(builder, "năng lượng", fatigueRecovery, ref hasAny);
        AppendRecovery(builder, "no bụng", hungerRecovery, ref hasAny);
        AppendRecovery(builder, "nước", thirstRecovery, ref hasAny);
        return hasAny ? builder.ToString() : "Không hồi chỉ số";
    }

    private void AppendRecovery(StringBuilder builder, string label, float amount, ref bool hasAny)
    {
        if (amount <= 0f)
        {
            return;
        }

        if (hasAny)
        {
            builder.Append(", ");
        }

        builder.Append(label).Append(" +").Append(Mathf.RoundToInt(amount));
        hasAny = true;
    }

    private void ShowToast(string message)
    {
        if (ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast(message, 2.6f);
        }
        else
        {
            Debug.Log("[ConsumableVendor] " + message);
        }
    }

    private void EnsureInteractionArea()
    {
        GameObject areaObject = GetOrCreateInteractionAreaObject();
        areaObject.layer = gameObject.layer;

        // Tính toán localScale và localPosition dựa trên lossyScale của cha để giữ kích thước thực tế trong thế giới luôn chuẩn (ví dụ: 0.5m)
        Vector3 lossy = transform.lossyScale;
        float lx = Mathf.Max(0.001f, Mathf.Abs(lossy.x));
        float ly = Mathf.Max(0.001f, Mathf.Abs(lossy.y));
        float lz = Mathf.Max(0.001f, Mathf.Abs(lossy.z));

        areaObject.transform.localPosition = new Vector3(
            interactionAreaCenter.x / lx,
            interactionAreaCenter.y / ly,
            interactionAreaCenter.z / lz);

        areaObject.transform.localRotation = Quaternion.identity;

        areaObject.transform.localScale = new Vector3(
            Mathf.Max(0.2f, interactionAreaSize.x) / lx,
            Mathf.Max(0.2f, interactionAreaSize.y) / ly,
            Mathf.Max(0.2f, interactionAreaSize.z) / lz);

        BoxCollider box = areaObject.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = areaObject.AddComponent<BoxCollider>();
        }

        box.isTrigger = true;
        box.size = Vector3.one;
        box.center = Vector3.zero;

        Renderer renderer = areaObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = showTransparentInteractionArea && (!Application.isPlaying || showInteractionAreaWhilePlaying);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = CreateAreaMaterial();
        }
    }

    private GameObject GetOrCreateInteractionAreaObject()
    {
        Transform existing = transform.Find(InteractionAreaObjectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject areaObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        areaObject.name = InteractionAreaObjectName;
        areaObject.transform.SetParent(transform, false);
        return areaObject;
    }

    private Material CreateAreaMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        Color color = interactionAreaColor;
        color.a = interactionAreaAlpha;
        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        return material;
    }
}
