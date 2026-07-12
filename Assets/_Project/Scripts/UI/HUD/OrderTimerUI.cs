using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class OrderTimerUI : MonoBehaviour
{
    public static OrderTimerUI Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject timerPanel; // Panel chứa danh sách đơn hàng
    public GameObject orderEntryPrefab; // Prefab cho 1 đơn hàng (có Text hiển thị tên đồ & thời gian)
    public Transform orderListContainer;

    private Dictionary<CustomerOrder, GameObject> orderEntries = new Dictionary<CustomerOrder, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (timerPanel != null) timerPanel.SetActive(false);
    }

    private void Update()
    {
        if (CustomerQueue.Instance == null) return;

        var activeOrders = CustomerQueue.Instance.ActiveOrders;
        if (activeOrders.Count > 0)
        {
            if (timerPanel != null && !timerPanel.activeSelf) timerPanel.SetActive(true);

            // Xóa UI cũ nếu đơn hàng đã hoàn thành/hết hạn
            List<CustomerOrder> toRemove = new List<CustomerOrder>();
            foreach (var kvp in orderEntries)
            {
                if (!activeOrders.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove) orderEntries.Remove(key);

            // Cập nhật/Tạo UI mới
            foreach (var order in activeOrders)
            {
                if (!orderEntries.ContainsKey(order))
                {
                    GameObject entry = Instantiate(orderEntryPrefab, orderListContainer);
                    orderEntries[order] = entry;
                }

                GameObject uiObj = orderEntries[order];
                TextMeshProUGUI textComp = uiObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                {
                    string status = order.isCompleted ? "<color=#44ff44>Đã sửa</color>" : "<color=#ff4444>Chưa sửa</color>";
                    textComp.text = $"{order.itemName}\n{status} - Lấy: {order.appointmentHour:00}:00 Ng{order.appointmentDay}";
                }
            }
        }
        else
        {
            if (timerPanel != null && timerPanel.activeSelf) timerPanel.SetActive(false);
        }
    }
}
