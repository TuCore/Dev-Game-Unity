using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InventoryApp : BaseApp
{
    public TextMeshProUGUI inventoryText;

    private void OnEnable()
    {
        UpdateUI();
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateUI;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        if (inventoryText == null) return;

        Dictionary<string, int> items = InventoryManager.Instance.GetItems();
        if (items == null || items.Count == 0)
        {
            inventoryText.text = "<color=#AAAAAA><i>Chưa có linh kiện nào trong túi.</i></color>";
            return;
        }

        string txt = "";
        foreach (var kvp in items)
        {
            if (kvp.Value > 0)
            {
                txt += $"<size=22>📦 <b>{kvp.Key}</b></size>\n<color=#888888>Số lượng:</color> <color=#00FF00><b>{kvp.Value}</b></color>\n\n";
            }
        }
        
        if (string.IsNullOrEmpty(txt))
        {
            inventoryText.text = "<color=#AAAAAA><i>Chưa có linh kiện nào trong túi.</i></color>";
        }
        else
        {
            inventoryText.text = txt;
        }
    }
}
