using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BankApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI debtText;
    [SerializeField] private TextMeshProUGUI loanInfoText;
    [SerializeField] private Button borrowButton;
    [SerializeField] private Button repayButton;

    private void Start()
    {
        if (borrowButton != null)
        {
            borrowButton.onClick.AddListener(OnBorrowClicked);
        }
        if (repayButton != null)
        {
            repayButton.onClick.AddListener(OnRepayClicked);
        }
        
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnCashChanged += UpdateUI;
            EconomyManager.Instance.OnDebtChanged += UpdateUI;
        }
    }

    protected override void OnAppOpened()
    {
        UpdateUI(0);
    }

    private void UpdateUI(float dummy)
    {
        if (EconomyManager.Instance != null)
        {
            if (balanceText != null) balanceText.text = $"{EconomyManager.Instance.CurrentCash:N0} VNĐ";
            if (debtText != null) debtText.text = $"{EconomyManager.Instance.CurrentDebt:N0} VNĐ";
            
            bool hasActiveLoan = EconomyManager.Instance.HasActiveLoan;

            if (loanInfoText != null)
            {
                if (!hasActiveLoan)
                {
                    float amt = EconomyManager.Instance.LoanAmount;
                    float rate = EconomyManager.Instance.LoanInterestRate;
                    int term = EconomyManager.Instance.LoanTermDays;
                    loanInfoText.text = $"Gói Vay: <color=#00FF00>{amt:N0}đ</color>\nLãi suất: {rate*100}%\nKỳ hạn: {term} ngày\n<size=12><i>(Trả góp hằng ngày)</i></size>";
                }
                else
                {
                    int remDays = EconomyManager.Instance.RemainingTermDays;
                    int term = EconomyManager.Instance.LoanTermDays;
                    float daily = EconomyManager.Instance.DailyInstallment;
                    int pen = EconomyManager.Instance.PenaltyDays;
                    string penStr = pen > 0 ? $"\n<color=#FF0000>Phạt trễ hạn: {pen} ngày!</color>" : "";
                    loanInfoText.text = $"Đã trả: {term - remDays}/{term} ngày\nGóp mỗi ngày: <color=#FF4444>{daily:N0}đ</color>{penStr}\nTất toán: <color=#FFAA00>{EconomyManager.Instance.CurrentDebt:N0}đ</color>";
                }
            }
            
            if (borrowButton != null)
            {
                borrowButton.gameObject.SetActive(!hasActiveLoan);
            }
            if (repayButton != null)
            {
                repayButton.gameObject.SetActive(hasActiveLoan);
                repayButton.interactable = EconomyManager.Instance.CurrentCash >= EconomyManager.Instance.CurrentDebt;
            }
        }
    }

    private void OnBorrowClicked()
    {
        if (EconomyManager.Instance != null && !EconomyManager.Instance.HasActiveLoan)
        {
            EconomyManager.Instance.TakeLoan();
            if (ToastNotificationManager.Instance != null)
                ToastNotificationManager.Instance.ShowToast($"Đã ký Hợp đồng vay {EconomyManager.Instance.LoanAmount:N0}đ", 3f);
            UpdateUI(0);
        }
    }

    private void OnRepayClicked()
    {
        if (EconomyManager.Instance != null && EconomyManager.Instance.HasActiveLoan)
        {
            if (EconomyManager.Instance.RepayFullLoan())
            {
                if (ToastNotificationManager.Instance != null)
                    ToastNotificationManager.Instance.ShowToast($"Đã TẤT TOÁN hợp đồng!", 3f);
                UpdateUI(0);
            }
        }
    }
    
    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnCashChanged -= UpdateUI;
            EconomyManager.Instance.OnDebtChanged -= UpdateUI;
        }
    }
}
