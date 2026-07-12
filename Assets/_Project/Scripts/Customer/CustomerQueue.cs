using UnityEngine;
using System.Collections.Generic;

public class CustomerQueue : MonoBehaviour
{
    public static CustomerQueue Instance { get; private set; }

    [Header("Reputation System")]
    public int currentReputation = 50; // Max 100, min 0
    
    [Header("Config")]
    [SerializeField] private int maxSimultaneousCustomers = 3;

    private List<CustomerOrder> _activeOrders = new List<CustomerOrder>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public int ActiveOrderCount => _activeOrders.Count;
    public int MaxCustomers => maxSimultaneousCustomers;
    public List<CustomerOrder> ActiveOrders => new List<CustomerOrder>(_activeOrders);

    public void AddCustomer(CustomerOrder order)
    {
        _activeOrders.Add(order);
    }

    public void CompleteOrder(CustomerOrder order)
    {
        // Don't remove it from the list here, because the customer still needs to come pick it up!
        // We just mark it as completed so when they come back, they pay.
    }

    public void RemoveFailedOrder(CustomerOrder order)
    {
        _activeOrders.Remove(order);
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
