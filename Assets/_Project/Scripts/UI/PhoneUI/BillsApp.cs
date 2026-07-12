using UnityEngine;
using TMPro;

public class BillsApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI electricityText;
    [SerializeField] private TextMeshProUGUI waterText;
    [SerializeField] private TextMeshProUGUI rentText;
    [SerializeField] private GameObject loanItemObj;
    [SerializeField] private TextMeshProUGUI loanText;
    [SerializeField] private TextMeshProUGUI totalText;

    protected override void OnAppOpened()
    {
        EconomyManager economy = EconomyManager.Instance;
        if (economy != null)
        {
            bool isDay1 = false;
            if (DayClock.Instance != null)
            {
                isDay1 = DayClock.Instance.CurrentDay == 1;
            }

            if (electricityText != null) electricityText.text = isDay1 ? "0đ" : $"{economy.DailyElectricity:N0}đ";
            if (waterText != null) waterText.text = isDay1 ? "0đ" : $"{economy.DailyWater:N0}đ";
            if (rentText != null) rentText.text = isDay1 ? "0đ" : $"{economy.DailyRent:N0}đ";
            
            if (economy.HasActiveLoan)
            {
                if (loanItemObj != null) loanItemObj.SetActive(true);
                if (loanText != null) loanText.text = isDay1 ? "0đ" : $"{economy.DailyInstallment:N0}đ";
            }
            else
            {
                if (loanItemObj != null) loanItemObj.SetActive(false);
            }

            if (totalText != null)
            {
                totalText.gameObject.SetActive(true);
                totalText.text = isDay1 ? "Tổng hóa đơn dự kiến: 0đ" : $"Tổng hóa đơn dự kiến: {economy.TotalDailyDeduction:N0}đ";
            }
        }
    }
}
