using UnityEngine;

/// <summary>
/// Quản lý thể lực (Stamina) của người chơi.
/// Thể lực ảnh hưởng độ chính xác trong minigame (mệt → tay run, dễ trượt).
/// Làm việc liên tục giảm Stamina, ăn uống/nghỉ ngơi hồi Stamina.
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [Header("Cấu hình Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float workDrainRate = 5f;     // Stamina mất mỗi lần sửa đồ
    [SerializeField] private float restRecoveryRate = 20f;  // Stamina hồi khi nghỉ ngơi

    private float _currentStamina;

    public float CurrentStamina => _currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => _currentStamina / maxStamina;
    public bool IsExhausted => _currentStamina <= 0f;

    // Events
    public System.Action<float> OnStaminaChanged;  // Truyền % stamina hiện tại
    public System.Action OnExhausted;

    private void Awake()
    {
        _currentStamina = maxStamina;
    }

    /// <summary>
    /// Giảm stamina khi làm việc (sửa đồ, chà rỉ sét, hàn mạch...).
    /// </summary>
    public void DrainStamina(float amount)
    {
        _currentStamina = Mathf.Max(0f, _currentStamina - amount);
        OnStaminaChanged?.Invoke(StaminaPercent);

        if (IsExhausted)
        {
            OnExhausted?.Invoke();
        }
    }

    /// <summary>
    /// Giảm stamina mặc định khi hoàn thành 1 lần sửa chữa.
    /// </summary>
    public void DrainFromWork() => DrainStamina(workDrainRate);

    /// <summary>
    /// Hồi stamina (ăn cơm tấm, uống cà phê, nghỉ ngơi).
    /// </summary>
    public void RecoverStamina(float amount)
    {
        _currentStamina = Mathf.Min(maxStamina, _currentStamina + amount);
        OnStaminaChanged?.Invoke(StaminaPercent);
    }

    /// <summary>
    /// Hồi phục qua đêm (nghỉ ngơi).
    /// </summary>
    public void RestOvernight() => RecoverStamina(restRecoveryRate);

    /// <summary>
    /// Reset stamina về max (dùng khi bắt đầu game mới).
    /// </summary>
    public void ResetStamina()
    {
        _currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(StaminaPercent);
    }
}
