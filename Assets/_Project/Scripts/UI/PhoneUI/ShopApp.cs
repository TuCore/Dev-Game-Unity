using UnityEngine;

public class ShopApp : BaseApp
{
    private EconomyManager _economy;

    private void Start()
    {
        _economy = FindFirstObjectByType<EconomyManager>();
    }

    public void BuyItem(string itemName, float price)
    {
        if (_economy == null) _economy = FindFirstObjectByType<EconomyManager>();

        if (_economy != null && _economy.CanAfford(price))
        {
            _economy.SpendCash(price);
            SpawnDeliveryBox(itemName);
            Debug.Log($"[S-Market] Đã thanh toán {price:N0}đ. Hàng ({itemName}) đang được giao tới!");
            ToastNotificationManager.Instance.ShowToast($"[S-Market] Kiện hàng chứa {itemName} đang được giao tới cửa!", 4f);
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.ShowSubtitle("S-Market (Thanh toán)", $"Đã trừ {price:N0}đ từ tài khoản. Đặt mua [{itemName}] thành công, kiện hàng đang được giao tới trước cửa!", 4f);
            }
        }
        else
        {
            Debug.Log($"[S-Market] Bạn không đủ {price:N0}đ để mua {itemName}!");
            ToastNotificationManager.Instance.ShowToast($"Thẻ của bạn không đủ tiền để mua {itemName}!", 3f);
        }
    }

    private void SpawnDeliveryBox(string itemName)
    {
        // Tạo Cube ảo làm kiện hàng rớt xuống
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "DeliveryBox_" + itemName;
        
        // Vị trí rớt: Trước mặt camera 1.5m, cao 2m
        Camera mainCam = Camera.main;
        Vector3 spawnPos = new Vector3(0, 5, 2); 
        if (mainCam != null)
        {
            spawnPos = mainCam.transform.position + mainCam.transform.forward * 1.5f + Vector3.up * 2f;
        }
        
        box.transform.position = spawnPos;
        box.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // Hộp cỡ 40cm

        // Đổi màu thành hộp carton nâu
        Renderer r = box.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = new Color(0.82f, 0.61f, 0.41f);
        }

        // Cho nó rơi vật lý
        Rigidbody rb = box.AddComponent<Rigidbody>();
        rb.mass = 3f;

        // Thêm DeliveryBox script để tương tác
        DeliveryBox db = box.AddComponent<DeliveryBox>();
        db.itemName = itemName;
    }
}
