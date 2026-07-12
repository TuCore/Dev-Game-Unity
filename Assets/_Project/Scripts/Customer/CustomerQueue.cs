using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý hàng đợi khách hàng.
/// Giai đoạn 1: 1 khách/lượt. Giai đoạn 2-3: nhiều khách cùng lúc với deadline riêng.
/// Không hardcode số lượng khách tối đa — đọc từ config theo giai đoạn.
/// </summary>
public class CustomerQueue : MonoBehaviour
{
    [Header("Cấu hình hàng đợi")]
    [SerializeField] private int maxSimultaneousCustomers = 1; // Tăng dần theo giai đoạn
    [SerializeField] private float minTimeBetweenCustomers = 30f;
    [SerializeField] private float maxTimeBetweenCustomers = 90f;

    private List<CustomerOrder> _activeOrders = new List<CustomerOrder>();
    private float _nextCustomerTimer;

    public int ActiveOrderCount => _activeOrders.Count;
    public int MaxCustomers => maxSimultaneousCustomers;
    public List<CustomerOrder> ActiveOrders => new List<CustomerOrder>(_activeOrders);

    // Events
    public System.Action<CustomerOrder> OnCustomerArrived;
    public System.Action<CustomerOrder> OnCustomerLeft;       // Khách bỏ đi (hết deadline)
    public System.Action<CustomerOrder> OnOrderCompleted;

    /// <summary>
    /// Cập nhật cấu hình max khách theo giai đoạn game (gọi khi lên level/danh tiếng).
    /// </summary>
    public void SetMaxCustomers(int max)
    {
        maxSimultaneousCustomers = Mathf.Max(1, max);
    }

    /// <summary>
    /// Thêm khách mới vào hàng đợi.
    /// </summary>
    public bool AddCustomer(CustomerOrder order)
    {
        if (_activeOrders.Count >= maxSimultaneousCustomers)
        {
            return false;
        }

        _activeOrders.Add(order);
        OnCustomerArrived?.Invoke(order);
        
        if (ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast("[+] Có khách mới đem đồ tới sửa kìa!", 3f);
        }
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowSubtitle("Khách Hàng (Ngoài cửa)", "Anh thợ ơi có nhà không? Sửa gấp giúp tôi món đồ này với!", 4f, "Tiếng mở cửa");
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Tiếng mở cửa");
        }
        
        return true;
    }

    /// <summary>
    /// Hoàn thành đơn hàng — khách nhận đồ và rời đi.
    /// </summary>
    public void CompleteOrder(CustomerOrder order)
    {
        if (_activeOrders.Remove(order))
        {
            OnOrderCompleted?.Invoke(order);
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.ShowSubtitle("Khách Hàng", "Sửa kỹ ghê, máy chạy êm ru! Gửi anh thêm chút tiền tip nha.", 4f, "Tiếng thanh toán");
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Tiếng thanh toán");
            }
        }
    }

    /// <summary>
    /// Xóa khách khi hết deadline (bỏ đi, mất danh tiếng).
    /// </summary>
    public void RemoveExpiredCustomer(CustomerOrder order)
    {
        if (_activeOrders.Remove(order))
        {
            OnCustomerLeft?.Invoke(order);
            
            if (ToastNotificationManager.Instance != null)
            {
                ToastNotificationManager.Instance.ShowToast("[!] Khách đợi lâu quá nên bỏ về rồi!", 4f);
            }
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.ShowSubtitle("Khách Hàng (Bực bội)", "Anh thợ làm ăn lâu lắc quá, tôi mang đồ qua tiệm khác sửa đây!", 4f, "Tiếng đóng cửa");
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Tiếng đóng cửa");
            }
        }
    }

    private void Update()
    {
        // Kiểm tra deadline từng đơn
        for (int i = _activeOrders.Count - 1; i >= 0; i--)
        {
            if (_activeOrders[i] != null && _activeOrders[i].IsExpired)
            {
                RemoveExpiredCustomer(_activeOrders[i]);
            }
        }
    }
}
