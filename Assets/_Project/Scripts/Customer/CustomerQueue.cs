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
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. Kiểm tra deadline từng đơn
        for (int i = _activeOrders.Count - 1; i >= 0; i--)
        {
            if (_activeOrders[i] != null)
            {
                _activeOrders[i].UpdateTimer(dt);
                if (_activeOrders[i].IsExpired)
                {
                    RemoveExpiredCustomer(_activeOrders[i]);
                }
            }
        }

        // 2. Logic tự động sinh khách mới
        if (_activeOrders.Count < maxSimultaneousCustomers)
        {
            _nextCustomerTimer -= dt;
            if (_nextCustomerTimer <= 0f)
            {
                SpawnRandomCustomer();
                // Reset timer ngẫu nhiên cho lần tiếp theo
                _nextCustomerTimer = Random.Range(minTimeBetweenCustomers, maxTimeBetweenCustomers);
            }
        }
    }

    private void SpawnRandomCustomer()
    {
        // Lấy danh sách đồ đã mở khóa từ SkillTree
        if (SkillTree.Instance == null) return;
        var unlockedItems = SkillTree.Instance.GetUnlockedItemTypes();
        if (unlockedItems.Count == 0) return;

        // Bốc ngẫu nhiên 1 món
        string randomItem = unlockedItems[Random.Range(0, unlockedItems.Count)];
        
        // Random độ khó và tiền công
        int difficulty = Random.Range(1, 4);
        float pay = Random.Range(50f, 150f) * difficulty;
        float deadline = Random.Range(45f, 120f); // 45s đến 120s để sửa
        
        CustomerOrder newOrder = new CustomerOrder("Khách ngẫu nhiên", randomItem, difficulty, pay, deadline);
        AddCustomer(newOrder);
    }
}
