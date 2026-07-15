using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ứng dụng "Thầu Thợ" trên Điện thoại để người chơi mua nâng cấp (Không gian + Đồ nghề).
/// </summary>
public class UpgradesApp : MonoBehaviour
{
    [Header("UI Elements - Buttons")]
    [SerializeField] private Button buyAirConButton;
    [SerializeField] private Button buySpeakerButton;
    [SerializeField] private Button upgradeShopButton;
    [SerializeField] private Button closeButton;

    [Header("UI Elements - Text")]
    [SerializeField] private TextMeshProUGUI airConPriceText;
    [SerializeField] private TextMeshProUGUI speakerPriceText;
    [SerializeField] private TextMeshProUGUI shopUpgradePriceText;
    [SerializeField] private TextMeshProUGUI currentComfortText;
    
    [Header("Prices")]
    [SerializeField] private float airConPrice = 2000f;
    [SerializeField] private float speakerPrice = 1500f;
    [SerializeField] private float shopUpgradePrice = 5000f;

    private EconomyManager _economy;
    private SpaceUpgradeSystem _space;

    private void Start()
    {
        _economy = FindFirstObjectByType<EconomyManager>();
        _space = SpaceUpgradeSystem.Instance;

        // Cập nhật text giá tiền ban đầu
        if (airConPriceText != null) airConPriceText.text = $"Mua Máy Lạnh\n{airConPrice}$";
        if (speakerPriceText != null) speakerPriceText.text = $"Mua Loa Xịn\n{speakerPrice}$";
        if (shopUpgradePriceText != null) shopUpgradePriceText.text = $"Nâng Cấp Tiệm\n{shopUpgradePrice}$";

        UpdateUI();

        // Gán sự kiện
        if (buyAirConButton != null) buyAirConButton.onClick.AddListener(OnBuyAirConditioner);
        if (buySpeakerButton != null) buySpeakerButton.onClick.AddListener(OnBuySpeaker);
        if (upgradeShopButton != null) upgradeShopButton.onClick.AddListener(OnUpgradeShop);
        if (closeButton != null) closeButton.onClick.AddListener(CloseApp);
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_space == null) return;

        // Cập nhật text tiện nghi
        if (currentComfortText != null)
        {
            currentComfortText.text = $"Độ tiện nghi: {_space.ComfortLevel} (Buff Hồi thể lực: +{_space.ComfortLevel * 10}%)";
        }

        // Ẩn/hiện nút mua máy lạnh
        if (buyAirConButton != null)
        {
            buyAirConButton.interactable = !_space.hasAirConditioner;
            if (_space.hasAirConditioner && airConPriceText != null)
            {
                airConPriceText.text = "Đã sở hữu Máy Lạnh";
            }
        }

        // Ẩn/hiện nút mua loa
        if (buySpeakerButton != null)
        {
            buySpeakerButton.interactable = !_space.hasSpeaker;
            if (_space.hasSpeaker && speakerPriceText != null)
            {
                speakerPriceText.text = "Đã sở hữu Loa";
            }
        }
    }

    private void OnBuyAirConditioner()
    {
        if (_space != null && _economy != null)
        {
            if (_space.BuyAirConditioner(_economy, airConPrice))
            {
                Debug.Log("[UpgradesApp] Đã mua Máy Lạnh thành công!");
                UpdateUI();
            }
            else
            {
                Debug.LogWarning("[UpgradesApp] Không đủ tiền mua Máy Lạnh!");
            }
        }
    }

    private void OnBuySpeaker()
    {
        if (_space != null && _economy != null)
        {
            if (_space.BuySpeaker(_economy, speakerPrice))
            {
                Debug.Log("[UpgradesApp] Đã mua Loa xịn thành công!");
                UpdateUI();
            }
            else
            {
                Debug.LogWarning("[UpgradesApp] Không đủ tiền mua Loa xịn!");
            }
        }
    }

    private void OnUpgradeShop()
    {
        if (_space != null && _economy != null)
        {
            if (_space.UpgradeShop(_economy, shopUpgradePrice))
            {
                Debug.Log($"[UpgradesApp] Đã nâng cấp tiệm lên Cấp {_space.ShopLevel}!");
                UpdateUI();
            }
            else
            {
                Debug.LogWarning("[UpgradesApp] Không đủ tiền nâng cấp tiệm!");
            }
        }
    }

    private void CloseApp()
    {
        gameObject.SetActive(false);
    }
}
