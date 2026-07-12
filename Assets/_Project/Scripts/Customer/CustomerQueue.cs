using UnityEngine;
using System.Collections.Generic;

public class CustomerQueue : MonoBehaviour
{
    public static CustomerQueue Instance { get; private set; }

    [Header("Cấu hình hàng đợi")]
    [SerializeField] private int maxSimultaneousCustomers = 3;

    [Header("Reputation System")]
    public int currentReputation = 50;

    private List<CustomerOrder> _activeOrders = new List<CustomerOrder>();

    public int ActiveOrderCount => _activeOrders.Count;
    public int MaxCustomers => maxSimultaneousCustomers;
    public List<CustomerOrder> ActiveOrders => new List<CustomerOrder>(_activeOrders);

    // Events
    public System.Action<CustomerOrder> OnCustomerArrived;
    public System.Action<CustomerOrder> OnCustomerLeft;
    public System.Action<CustomerOrder> OnOrderCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetMaxCustomers(int max)
    {
        maxSimultaneousCustomers = Mathf.Max(1, max);
    }

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

    public void CompleteOrder(CustomerOrder order)
    {
        order.MarkCompleted();
        OnOrderCompleted?.Invoke(order);
    }

    public void RemoveFailedOrder(CustomerOrder order)
    {
        if (_activeOrders.Remove(order))
        {
            OnCustomerLeft?.Invoke(order);
        }
    }

    public void RemoveOrderWhenPickedUp(CustomerOrder order)
    {
        _activeOrders.Remove(order);
    }

    public void ReduceReputation(int amount)
    {
        currentReputation -= amount;
        if (currentReputation < 0) currentReputation = 0;
        
        if (ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast($"Danh tiếng giảm {amount}! Hiện tại: {currentReputation}", 3f);
        }
    }

    public void IncreaseReputation(int amount)
    {
        currentReputation += amount;
        if (currentReputation > 100) currentReputation = 100;
    }
}
