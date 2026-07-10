using UnityEngine;
using TMPro;

public class BankApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI balanceText;

    protected override void OnAppOpened()
    {
        EconomyManager economy = FindFirstObjectByType<EconomyManager>();
        if (economy != null && balanceText != null)
        {
            balanceText.text = $"{economy.CurrentCash:N0} VNĐ";
        }
    }
}
