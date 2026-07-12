using UnityEngine;
using TMPro;

public class RaycastInteract : MonoBehaviour
{
    [Header("Cấu hình tương tác")]
    [SerializeField] private float interactRange = 5f; // Tăng khoảng cách tương tác lên 5 mét
    [SerializeField] private LayerMask interactableMask = ~0; // Mặc định là Everything để tránh bị lỗi Nothing

    [Header("Cầm nắm đồ vật")]
    [SerializeField] private Transform holdPosition; // Điểm cầm đồ vật (thường là con của Camera)
    [SerializeField] private float heldItemScale = 0.25f; // Tỉ lệ thu nhỏ khi cầm (vd: 0.25 = nhỏ bằng 1/4)
    [SerializeField] private float rotationSpeed = 300f; // Tốc độ xoay đồ vật bằng con lăn chuột

    private Camera _cam;
    private PickupItem _currentlyHeldItem;
    private TextMeshProUGUI _promptText;
    private GameObject _placementGhost;
    private float _heldItemRotationOffset = 0f; // Lưu góc xoay tuỳ chỉnh
    private MinigameManager _minigameManager;

    private void Start()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
        {
            _cam = Camera.main;
        }
        
        _minigameManager = FindObjectOfType<MinigameManager>();

        // Tự tạo một HoldPosition nếu chưa gán trong Editor
        if (holdPosition == null && _cam != null)
        {
            GameObject hpObj = new GameObject("HoldPosition");
            hpObj.transform.SetParent(_cam.transform);
            // Đặt vị trí trước mặt camera khoảng 1.5m, hơi xích xuống một chút
            hpObj.transform.localPosition = new Vector3(0, -0.3f, 1.2f);
            holdPosition = hpObj.transform;
        }

        CreatePromptUI();
    }

    private void CreatePromptUI()
    {
        // Tái sử dụng HUD_Canvas nếu đã có
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null)
        {
            canvas = existingCanvas.GetComponent<Canvas>();
        }
        
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject textObj = new GameObject("InteractionPromptText");
        textObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -60f); // Nằm ngay dưới tâm ngắm (crosshair)
        rect.sizeDelta = new Vector2(600, 50);

        _promptText = textObj.AddComponent<TextMeshProUGUI>();
        _promptText.fontSize = 24;
        _promptText.alignment = TextAlignmentOptions.Center;
        _promptText.color = Color.white;
        _promptText.fontStyle = FontStyles.Bold;
        
        // Thêm viền đen để chữ luôn nổi bật trên mọi màu nền
        UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        
        // Đảm bảo có EconomyManager và MoneyUI ngay từ đầu để hiển thị số tiền luôn
        if (FindFirstObjectByType<EconomyManager>() == null)
        {
            canvas.gameObject.AddComponent<EconomyManager>();
        }
        if (FindFirstObjectByType<MoneyUI>() == null)
        {
            canvas.gameObject.AddComponent<MoneyUI>();
        }
        
        // Sinh ra hệ thống Điện thoại (Phone UI)
        if (FindFirstObjectByType<PhoneUIBuilder>() == null)
        {
            canvas.gameObject.AddComponent<PhoneUIBuilder>();
        }

        // Sinh ra hệ thống Kho đồ (Inventory UI)
        if (FindFirstObjectByType<InventoryManager>() == null)
        {
            GameObject invObj = new GameObject("InventoryManager");
            invObj.AddComponent<InventoryManager>();
        }

        _promptText.text = "";
    }

    private Material CreateGhostMaterial()
    {
        // Thử dùng Standard shader cho Unity Built-in
        Shader standard = Shader.Find("Standard");
        if (standard != null)
        {
            Material mat = new Material(standard);
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(0f, 1f, 0f, 0.4f); // Xanh lá cây trong suốt
            return mat;
        }
        // Fallback cực an toàn nếu dùng URP
        Material fallback = new Material(Shader.Find("GUI/Text Shader"));
        fallback.color = new Color(0f, 1f, 0f, 0.4f);
        return fallback;
    }

    private void Update()
    {
        // Nếu minigame đang bật thì tắt chữ và cấm nhặt/thả/sửa
        if (_minigameManager != null && _minigameManager.IsMinigameActive)
        {
            if (_promptText != null) _promptText.text = "";
            return;
        }

        // Xoá text ở đầu mỗi frame
        if (_promptText != null) _promptText.text = "";

        // Tạo tia Raycast chung cho cả hai trường hợp
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool isHit = Physics.Raycast(ray, out hit, interactRange, interactableMask);

        // 1. Nếu đang cầm một đồ vật, xác định vị trí thả
        if (_currentlyHeldItem != null)
        {
            // Nhận input con lăn chuột để xoay
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _heldItemRotationOffset += scroll * rotationSpeed;
            }
            
            // Tính toán góc xoay hiện tại (góc nguyên bản + độ lệch xoay)
            Quaternion currentRotation = Quaternion.Euler(0, _heldItemRotationOffset, 0) * _currentlyHeldItem.OriginalRotation;

            if (_promptText != null)
            {
                string t = isHit ? "Nhấn [E] Đặt | Lăn chuột để Xoay" : "Nhấn [E] Thả | Lăn chuột để Xoay";
                if (_promptText.text != t) _promptText.text = t;
            }

            // === TẠO BÓNG MỜ XANH (GHOST) ===
            if (_placementGhost == null)
            {
                _placementGhost = Instantiate(_currentlyHeldItem.gameObject);
                _placementGhost.name = "PlacementGhost";
                
                // Dọn dẹp sạch sẽ các component thừa trên bóng mờ
                Destroy(_placementGhost.GetComponent<PickupItem>());
                Destroy(_placementGhost.GetComponent<Rigidbody>());
                foreach (var col in _placementGhost.GetComponentsInChildren<Collider>()) Destroy(col);
                
                // Đổi toàn bộ material thành xanh lá trong suốt
                Material ghostMat = CreateGhostMaterial();
                foreach (var r in _placementGhost.GetComponentsInChildren<Renderer>())
                {
                    Material[] mats = new Material[r.materials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                    r.materials = mats;
                }
            }

            // Hiển thị và di chuyển bóng mờ
            if (isHit)
            {
                _placementGhost.SetActive(true);
                _placementGhost.transform.position = hit.point;
                _placementGhost.transform.rotation = currentRotation; // Áp dụng góc xoay chuột
                _placementGhost.transform.localScale = _currentlyHeldItem.OriginalScale;
            }
            else
            {
                // Nếu tia nhìn không trúng mặt bàn/đất thì ẩn bóng mờ đi
                _placementGhost.SetActive(false);
            }
            // ================================

            if (Input.GetKeyDown(KeyCode.E))
            {
                // Nếu tia nhìn trúng mặt bàn/đất thì đặt tại hit.point, nếu không thì đặt lơ lửng ở vị trí tay cầm
                Vector3 placePos = isHit ? hit.point : holdPosition.position;
                _currentlyHeldItem.Drop(placePos, currentRotation); // Truyền góc xoay mới vào
                _currentlyHeldItem = null;
                
                // Xoá bóng mờ sau khi đặt xong
                if (_placementGhost != null) Destroy(_placementGhost);
            }
            return; // Đang cầm đồ thì không tương tác với các đồ khác
        }

        // 2. Nếu tay đang trống, tìm đồ để nhặt/tương tác
        string promptText = "";
        
        if (isHit)
        {
            // Xử lý đồ vật có thể nhặt/tương tác bằng E
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                promptText += interactable.GetInteractionPrompt();
                
                // Nhấn phím E để tương tác
                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickupItem pickup = hit.collider.GetComponent<PickupItem>();
                    if (pickup != null)
                    {
                        pickup.Pickup(holdPosition, heldItemScale);
                        _currentlyHeldItem = pickup;
                        _heldItemRotationOffset = 0f; // Đặt lại góc xoay khi nhặt đồ mới
                        return; // Đã nhặt thì ngưng xử lý bên dưới
                    }
                    else
                    {
                        interactable.Interact();
                    }
                }
            }

            // Xử lý đồ vật có thể sửa chữa bằng F
            RepairableItem repairable = hit.collider.GetComponent<RepairableItem>();
            if (repairable != null)
            {
                if (!string.IsNullOrEmpty(promptText)) promptText += "\n";
                
                if (!repairable.CanBeRepaired())
                {
                    promptText += "<color=#AAAAAA>Món đồ này đã sửa xong (Không thể sửa thêm)</color>";
                }
                else if (repairable.HasRequiredParts())
                {
                    promptText += "<color=#00FF00>Nhấn [F] để Sửa chữa (Đủ đồ)</color>";
                }
                else
                {
                    promptText += $"<color=#FF0000>Thiếu: {repairable.GetMissingPartsText()}</color>";
                }
                
                if (Input.GetKeyDown(KeyCode.F))
                {
                    repairable.StartRepair();
                }
            }
        }

        // Luôn cập nhật (hoặc xóa) chữ hiển thị trên màn hình
        if (_promptText != null)
        {
            if (_promptText.text != promptText)
            {
                _promptText.text = promptText;
            }
        }
    }

    private void OnDisable()
    {
        // Xóa sạch chữ trên màn hình khi chuyển cảnh (script bị hủy/tắt)
        if (_promptText != null)
        {
            _promptText.text = "";
        }
    }
}

