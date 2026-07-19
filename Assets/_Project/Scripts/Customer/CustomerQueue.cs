using UnityEngine;
using System.Collections.Generic;

public class CustomerQueue : MonoBehaviour
{
    private static CustomerQueue _instance;
    public static CustomerQueue Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CustomerQueue>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CustomerQueue_Singleton");
                    _instance = go.AddComponent<CustomerQueue>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        var init = Instance;
    }

    [Header("Cấu hình hàng đợi")]
    [Tooltip("Số đơn/món đồ sửa chữa có thể tồn tại cùng lúc. Giới hạn NPC ngoài scene nằm ở CustomerSpawner.")]
    [SerializeField] private int maxSimultaneousCustomers = 8;
    private const int MinimumRepairOrderCapacity = 8;

    [Header("Reputation System")]
    public int currentReputation = 50;

    private List<CustomerOrder> _activeOrders = new List<CustomerOrder>();

    public int ActiveOrderCount => _activeOrders.Count;
    public int MaxCustomers => maxSimultaneousCustomers;
    public bool CanAcceptMoreOrders => _activeOrders.Count < maxSimultaneousCustomers;
    public List<CustomerOrder> ActiveOrders => new List<CustomerOrder>(_activeOrders);

    // Events
    public System.Action<CustomerOrder> OnCustomerArrived;
    public System.Action<CustomerOrder> OnCustomerLeft;
    public System.Action<CustomerOrder> OnOrderCompleted;

    private void Awake()
    {
        if (_instance != null && _instance != this) 
        {
            Destroy(this); // Chỉ huỷ component này, không huỷ gameObject (CustomerManager)
        }
        else 
        {
            _instance = this;
            maxSimultaneousCustomers = Mathf.Max(MinimumRepairOrderCapacity, maxSimultaneousCustomers);
            // Di chuyển component này ra khỏi CustomerManager nếu nó đang nằm trên đó
            if (gameObject.name != "CustomerQueue_Singleton" && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    private void OnValidate()
    {
        maxSimultaneousCustomers = Mathf.Max(MinimumRepairOrderCapacity, maxSimultaneousCustomers);
    }

    public void SetMaxCustomers(int max)
    {
        maxSimultaneousCustomers = Mathf.Max(MinimumRepairOrderCapacity, max);
    }

    public bool AddCustomer(CustomerOrder order)
    {
        if (_activeOrders.Count >= maxSimultaneousCustomers)
        {
            return false;
        }

        _activeOrders.Add(order);
        CustomerMessageLog.AddOrderAccepted(order);
        OnCustomerArrived?.Invoke(order);
        TaskManager.EnsureInstance().NotifyOrderAccepted(order);
        
        if (ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast("[+] Có khách mới đem đồ tới sửa kìa!", 3f);
        }
        // Chi hien toast lon. Phat am thanh truc tiep de khong can tao them
        // SubtitlePanel nho o goc trai.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Tiếng mở cửa");
        }
        
        return true;
    }

    public void CompleteOrder(CustomerOrder order)
    {
        if (_activeOrders.Remove(order))
        {
            CustomerMessageLog.AddOrderCompleted(order);
            OnOrderCompleted?.Invoke(order);
            TaskManager.EnsureInstance().NotifyOrderReturned(order);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Tiếng thanh toán");
            }
        }
    }

    public void RemoveFailedOrder(CustomerOrder order)
    {
        if (_activeOrders.Remove(order))
        {
            CustomerMessageLog.AddOrderFailed(order);
            OnCustomerLeft?.Invoke(order);
            
            if (ToastNotificationManager.Instance != null)
            {
                ToastNotificationManager.Instance.ShowToast("[!] Khách đợi lâu quá nên bỏ về rồi!", 4f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Tiếng đóng cửa");
            }
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
