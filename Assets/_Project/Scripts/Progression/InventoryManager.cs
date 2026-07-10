using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InventoryManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("InventoryManager");
                    _instance = go.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }

    private Dictionary<string, int> items = new Dictionary<string, int>();

    public Action<string, int> OnItemAdded;
    public Action OnInventoryChanged;

    public Dictionary<string, int> GetItems()
    {
        return items;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public void AddItem(string itemName, int amount = 1)
    {
        if (items.ContainsKey(itemName))
            items[itemName] += amount;
        else
            items.Add(itemName, amount);
            
        Debug.Log($"[Inventory] Đã thêm {amount} {itemName}. Tổng cộng: {items[itemName]}");
        OnItemAdded?.Invoke(itemName, items[itemName]);
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(string itemName, int amount = 1)
    {
        return items.ContainsKey(itemName) && items[itemName] >= amount;
    }

    public bool ConsumeItem(string itemName, int amount = 1)
    {
        if (HasItem(itemName, amount))
        {
            items[itemName] -= amount;
            Debug.Log($"[Inventory] Đã dùng {amount} {itemName}. Còn lại: {items[itemName]}");
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }
}
