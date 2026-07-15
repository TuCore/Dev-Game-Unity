using UnityEngine;

/// <summary>
/// Tổng hợp quản lý Cash, chi phí sinh hoạt, giá nâng cấp.
/// Tập trung mọi thay đổi Cash qua đây để dễ balance & log (GDD mục 13).
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }


    [Header("Cấu hình Kinh tế")]
    [SerializeField] private float startingCash = 50000f;   // VNĐ khởi đầu

    [Header("Chi phí sinh hoạt hàng ngày")]
    [SerializeField] private float dailyRent = 15000f;       // Tiền trọ
    [SerializeField] private float dailyElectricity = 5000f;  // Tiền điện
    [SerializeField] private float dailyWater = 3000f;        // Tiền nước

    [Header("Cấu hình Tín dụng (Vay trả góp)")]
    [SerializeField] private float loanAmount = 100000f;     // Gói vay mặc định
    [SerializeField] private int loanTermDays = 5;           // Kỳ hạn trả
    [SerializeField] private float loanInterestRate = 0.20f; // Lãi suất tổng cộng (20%)
    [SerializeField] private float penaltyFee = 50000f;      // Phạt mỗi ngày trễ hạn

    public float DailyRent => dailyRent;
    public float DailyElectricity => dailyElectricity;
    public float DailyWater => dailyWater;
    public float LoanAmount => loanAmount;
    public int LoanTermDays => loanTermDays;
    public float LoanInterestRate => loanInterestRate;

    private float _currentCash;
    private float _dailyIncome;
    private float _currentDebt;
    private int _remainingTermDays;
    private int _penaltyDays;

    public float CurrentCash => _currentCash;
    public float DailyIncome => _dailyIncome;
    public float CurrentDebt => _currentDebt;
    public int RemainingTermDays => _remainingTermDays;
    public int PenaltyDays => _penaltyDays;

    public float DailyInstallment => _currentDebt > 0 && _remainingTermDays > 0 ? _currentDebt / _remainingTermDays : 0f;
    public float DailyExpenses => dailyRent + dailyElectricity + dailyWater;
    public float TotalDailyDeduction => DailyExpenses + DailyInstallment;
    public bool IsBankrupt => _currentCash < 0f || _penaltyDays >= 3;
    public bool HasActiveLoan => _currentDebt > 0;

    // Events
    public System.Action<float> OnCashChanged;        
    public System.Action<float> OnCashEarned;         
    public System.Action<float> OnCashSpent;          
    public System.Action<float> OnDebtChanged;        
    public System.Action OnBankrupt;                   

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        _currentCash = startingCash;
        _dailyIncome = 0f;
        _currentDebt = 0f;
        _remainingTermDays = 0;
        _penaltyDays = 0;
    }

    public void AddCash(float amount)
    {
        if (amount <= 0) return;
        _currentCash += amount;
        _dailyIncome += amount;
        OnCashEarned?.Invoke(amount);
        OnCashChanged?.Invoke(_currentCash);
    }
    
    public void ResetDailyIncome()
    {
        _dailyIncome = 0f;
    }

    public bool SpendCash(float amount)
    {
        if (amount <= 0) return false;
        if (_currentCash < amount) return false; 

        _currentCash -= amount;
        OnCashSpent?.Invoke(amount);
        OnCashChanged?.Invoke(_currentCash);

        if (IsBankrupt) OnBankrupt?.Invoke();

        return true;
    }

    public void TakeLoan()
    {
        if (HasActiveLoan) return; // Chỉ được 1 khoản vay
        
        _currentCash += loanAmount;
        _currentDebt = loanAmount + (loanAmount * loanInterestRate);
        _remainingTermDays = loanTermDays;
        _penaltyDays = 0;

        OnCashChanged?.Invoke(_currentCash);
        OnDebtChanged?.Invoke(_currentDebt);
        Debug.Log($"[EconomyManager] Đã vay {loanAmount:N0}đ. Tổng nợ: {_currentDebt:N0}đ");
    }

    public bool RepayFullLoan()
    {
        if (_currentDebt <= 0) return false;
        if (_currentCash < _currentDebt) return false;
        
        _currentCash -= _currentDebt;
        _currentDebt = 0;
        _remainingTermDays = 0;
        _penaltyDays = 0;

        OnCashChanged?.Invoke(_currentCash);
        OnDebtChanged?.Invoke(_currentDebt);
        Debug.Log($"[EconomyManager] Đã TẤT TOÁN nợ.");
        return true;
    }

    /// <summary>
    /// Trừ chi phí sinh hoạt cuối ngày (tiền trọ + điện + nước) và tiền góp.
    /// Trả về false nếu game over.
    /// </summary>
    public bool DeductDailyExpenses()
    {
        // 1. Trừ chi phí sinh hoạt
        if (!SpendCash(DailyExpenses))
        {
            // Trượt đóng tiền trọ -> Bị đuổi -> Game Over
            OnBankrupt?.Invoke();
            return false; 
        }
        
        // 2. Trừ tiền góp (nếu có nợ)
        if (HasActiveLoan)
        {
            float installment = DailyInstallment;
            
            if (_currentCash >= installment)
            {
                // Trả thành công
                _currentCash -= installment;
                _currentDebt -= installment;
                _remainingTermDays--;
                _penaltyDays = 0; // Trả được nợ thì xoá số ngày phạt trước đó
                
                OnCashChanged?.Invoke(_currentCash);
                OnDebtChanged?.Invoke(_currentDebt);
            }
            else
            {
                // Trả THẤT BẠI -> Phạt
                _penaltyDays++;
                _currentDebt += penaltyFee;
                OnDebtChanged?.Invoke(_currentDebt);
                Debug.Log($"[EconomyManager] Không đủ tiền góp! Bị phạt {penaltyFee:N0}đ. Tổng nợ mới: {_currentDebt:N0}đ");
                
                if (IsBankrupt)
                {
                    OnBankrupt?.Invoke();
                    return false;
                }
            }

            // Xử lý sạch nợ khi trả xong
            if (_currentDebt <= 0 || _remainingTermDays <= 0)
            {
                _currentDebt = 0;
                _remainingTermDays = 0;
                _penaltyDays = 0;
                OnDebtChanged?.Invoke(_currentDebt);
            }
        }
        
        Debug.Log($"[EconomyManager] Chi phí ngày đã trừ. Còn lại: {_currentCash:N0}đ");
        return true;
    }

    public bool CanAfford(float price) => _currentCash >= price;
}
