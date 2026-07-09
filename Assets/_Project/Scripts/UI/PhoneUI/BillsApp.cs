using UnityEngine;
using TMPro;

public class BillsApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI electricityText;
    [SerializeField] private TextMeshProUGUI waterText;
    [SerializeField] private TextMeshProUGUI rentText;
    [SerializeField] private TextMeshProUGUI totalText;

    protected override void OnAppOpened()
    {
        EconomyManager economy = FindFirstObjectByType<EconomyManager>();
        if (economy != null)
        {
            if (electricityText != null) electricityText.text = $"Tiền điện: {economy.DailyElectricity:N0}đ";
            if (waterText != null) waterText.text = $"Tiền nước: {economy.DailyWater:N0}đ";
            if (rentText != null) rentText.text = $"Tiền trọ: {economy.DailyRent:N0}đ";
            if (totalText != null) totalText.text = $"Tổng cộng: {economy.DailyExpenses:N0}đ";
        }
    }
}
