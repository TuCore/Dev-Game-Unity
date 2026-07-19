using System;
using UnityEngine;
using TMPro;

public class RaycastInteract : MonoBehaviour
{
    [Header("Customer Aim Assist")]
    [SerializeField] private float customerAimAssistRadius = 0.85f;
    [SerializeField] private float customerAimViewportRadius = 0.16f;
    [SerializeField] private float customerAimHeight = 1.35f;
    [SerializeField] private float customerCacheRefreshInterval = 0.2f;

    [Header("Station Aim Assist")]
    [SerializeField] private float stationAimAssistRadius = 0.6f; // Giảm từ 2.8m xuống 0.6m để tránh tranh chấp vùng chọn
    [SerializeField] private float stationAimViewportRadius = 0.15f; // Giảm từ 0.46 xuống 0.15
    [SerializeField] private float stationCacheRefreshInterval = 0.25f;

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
    private AnhThoDien.UI.HUD.CrosshairUI _crosshairUI;
    private CustomerController[] _cachedCustomers;
    private float _nextCustomerCacheRefreshTime;
    private TobaccoPipeStation[] _cachedTobaccoPipeStations;
    private float _nextStationCacheRefreshTime;

    private void Start()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
        {
            _cam = Camera.main;
        }
        
        _minigameManager = FindObjectOfType<MinigameManager>();
        _crosshairUI = FindFirstObjectByType<AnhThoDien.UI.HUD.CrosshairUI>();
        stationAimAssistRadius = Mathf.Max(stationAimAssistRadius, 2.8f);
        stationAimViewportRadius = Mathf.Max(stationAimViewportRadius, 0.46f);

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
            SetCrosshairTargeting(false);
            return;
        }

        // Xoá text ở đầu mỗi frame
        if (_promptText != null) _promptText.text = "";

        // Tạo tia Raycast chung cho cả hai trường hợp
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool isHit = Physics.Raycast(ray, out hit, interactRange, interactableMask);
        RaycastHit interactionHit;
        bool hasInteractionHit = TryFindUsableEmptyHandHit(ray, out interactionHit);

        // 1. Nếu đang cầm một đồ vật, xác định vị trí thả
        if (_currentlyHeldItem != null)
        {
            SetCrosshairTargeting(false);

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
                float offsetY = _currentlyHeldItem.GetHeightOffsetForRotation(currentRotation);
                _placementGhost.transform.position = hit.point + new Vector3(0f, offsetY, 0f);
                _placementGhost.transform.rotation = currentRotation; // Áp dụng góc xoay chuột
                _placementGhost.transform.localScale = _currentlyHeldItem.OriginalScale;
            }
            else
            {
                // Nếu tia nhìn không trúng mặt bàn/đất thì ẩn bóng mờ đi
                _placementGhost.SetActive(false);
            }
            // ================================

            if (WasInteractPressed())
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
        bool isTargeting = false;

        // Ưu tiên 1: Tương tác TRỰC TIẾP khi nhìn thẳng vào vật thể (Direct Raycast Hit)
        if (hasInteractionHit)
        {
            hit = interactionHit;

            // Xử lý đồ vật có thể nhặt/tương tác bằng E
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                promptText += interactable.GetInteractionPrompt();
                isTargeting = true;
                
                // Nhấn phím E để tương tác
                if (WasInteractPressed())
                {
                    PickupItem pickup = hit.collider.GetComponentInParent<PickupItem>();
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
            RepairableItem repairable = hit.collider.GetComponentInParent<RepairableItem>();
            if (repairable != null)
            {
                if (!string.IsNullOrEmpty(promptText)) promptText += "\n";
                
                if (!repairable.CanBeRepaired())
                {
                    promptText += "<color=#AAAAAA>Món đồ này đã sửa xong (Không thể sửa thêm)</color>";
                }
                else
                {
                    isTargeting = true;
                    if (repairable.HasRequiredParts())
                    {
                        promptText += $"<color=#00FF00>Nhấn [F] để Sửa chữa</color>\n<color=#D7F8FF>Cần: {repairable.GetRequiredPartsText()}</color>";
                    }
                    else
                    {
                        promptText += $"<color=#FF0000>{repairable.GetMissingPartsText()}</color>";
                    }
                }
                
                if (CustomInputManager.GetKeyDown("Secondary"))
                {
                    repairable.StartRepair();
                }
            }
        }
        else
        {
            // Ưu tiên 2: Sử dụng Aim Assist làm fallback khi nhìn lệch ra ngoài
            CustomerController customerTarget = FindCustomerAimTarget(ray);
            TobaccoPipeStation tobaccoPipeTarget = customerTarget == null ? FindTobaccoPipeAimTarget(ray) : null;
            
            if (customerTarget != null)
            {
                promptText += customerTarget.GetInteractionPrompt();
                isTargeting = true;

                if (WasInteractPressed())
                {
                    customerTarget.Interact();
                }
            }
            else if (tobaccoPipeTarget != null)
            {
                promptText += tobaccoPipeTarget.GetInteractionPrompt();
                isTargeting = true;

                if (WasInteractPressed())
                {
                    tobaccoPipeTarget.Interact();
                }
            }
        }

        // Luôn cập nhật (hoặc xóa) chữ hiển thị trên màn hình
        if (string.IsNullOrEmpty(promptText) && WasInteractPressed() && TryInteractNearestTobaccoPipe(ray))
        {
            SetCrosshairTargeting(false);
            return;
        }

        SetCrosshairTargeting(isTargeting);

        if (_promptText != null)
        {
            if (_promptText.text != promptText)
            {
                _promptText.text = promptText;
            }
        }
    }

    private bool TryFindUsableEmptyHandHit(Ray ray, out RaycastHit usableHit)
    {
        usableHit = default;

        RaycastHit[] hits = Physics.RaycastAll(ray, interactRange, interactableMask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];
            if (candidate.collider == null)
            {
                continue;
            }

            if (IsUsableEmptyHandHit(candidate))
            {
                usableHit = candidate;
                return true;
            }

            if (ShouldIgnoreEmptyHandBlocker(candidate))
            {
                continue;
            }

            return false;
        }

        return false;
    }

    private bool IsUsableEmptyHandHit(RaycastHit hit)
    {
        if (hit.collider.GetComponentInParent<RepairableItem>() != null)
        {
            return true;
        }

        if (hit.collider.GetComponentInParent<PickupItem>() != null)
        {
            return true;
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        return interactable != null && !string.IsNullOrEmpty(interactable.GetInteractionPrompt());
    }

    private bool ShouldIgnoreEmptyHandBlocker(RaycastHit hit)
    {
        CustomerController customer = hit.collider.GetComponentInParent<CustomerController>();
        return customer != null && string.IsNullOrEmpty(customer.GetInteractionPrompt());
    }

    private CustomerController FindCustomerAimTarget(Ray ray)
    {
        CustomerController bestCustomer = null;
        float bestScore = float.MaxValue;

        RaycastHit[] sphereHits = Physics.SphereCastAll(ray, customerAimAssistRadius, interactRange, interactableMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < sphereHits.Length; i++)
        {
            CustomerController customer = sphereHits[i].collider.GetComponentInParent<CustomerController>();
            if (TryGetCustomerAimScore(customer, ray, out float score) && score < bestScore)
            {
                bestScore = score;
                bestCustomer = customer;
            }
        }

        CustomerController[] customers = GetCachedCustomers();
        for (int i = 0; i < customers.Length; i++)
        {
            CustomerController customer = customers[i];
            if (TryGetCustomerAimScore(customer, ray, out float score) && score < bestScore)
            {
                bestScore = score;
                bestCustomer = customer;
            }
        }

        return bestCustomer;
    }

    private bool TryGetCustomerAimScore(CustomerController customer, Ray ray, out float score)
    {
        score = 0f;

        if (customer == null || !customer.isActiveAndEnabled)
        {
            return false;
        }

        if (string.IsNullOrEmpty(customer.GetInteractionPrompt()))
        {
            return false;
        }

        Vector3 aimPoint = GetCustomerAimPoint(customer);
        Vector3 fromCamera = aimPoint - _cam.transform.position;
        float distance = fromCamera.magnitude;
        if (distance <= 0.01f || distance > interactRange + customerAimAssistRadius)
        {
            return false;
        }

        float forwardDistance = Vector3.Dot(fromCamera, ray.direction);
        if (forwardDistance <= 0f || forwardDistance > interactRange)
        {
            return false;
        }

        float distanceFromAimRay = (fromCamera - ray.direction * forwardDistance).magnitude;
        Vector3 viewportPoint = _cam.WorldToViewportPoint(aimPoint);
        Vector2 viewportOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);

        if (viewportPoint.z <= 0f || (distanceFromAimRay > customerAimAssistRadius && viewportOffset.magnitude > customerAimViewportRadius))
        {
            return false;
        }

        if (!HasClearLineToCustomer(customer, aimPoint, distance))
        {
            return false;
        }

        score = viewportOffset.sqrMagnitude * 100f + distanceFromAimRay + distance * 0.01f;
        return true;
    }

    private Vector3 GetCustomerAimPoint(CustomerController customer)
    {
        Collider[] colliders = customer.GetComponentsInChildren<Collider>();
        bool hasBounds = false;
        Bounds bounds = new Bounds(customer.transform.position, Vector3.zero);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || !colliders[i].enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        Vector3 point = hasBounds ? bounds.center : customer.transform.position;
        point.y = Mathf.Max(point.y, customer.transform.position.y + customerAimHeight);
        return point;
    }

    private bool HasClearLineToCustomer(CustomerController customer, Vector3 aimPoint, float distance)
    {
        Vector3 origin = _cam.transform.position;
        Vector3 direction = (aimPoint - origin).normalized;
        float checkDistance = Mathf.Max(0f, distance - 0.05f);

        if (!Physics.Raycast(origin, direction, out RaycastHit blocker, checkDistance, interactableMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        CustomerController blockerCustomer = blocker.collider.GetComponentInParent<CustomerController>();
        return blockerCustomer == customer;
    }

    private CustomerController[] GetCachedCustomers()
    {
        if (_cachedCustomers == null || Time.time >= _nextCustomerCacheRefreshTime)
        {
            _cachedCustomers = FindObjectsOfType<CustomerController>();
            _nextCustomerCacheRefreshTime = Time.time + customerCacheRefreshInterval;
        }

        return _cachedCustomers;
    }

    private TobaccoPipeStation FindTobaccoPipeAimTarget(Ray ray)
    {
        TobaccoPipeStation bestStation = null;
        float bestScore = float.MaxValue;

        RaycastHit[] sphereHits = Physics.SphereCastAll(ray, stationAimAssistRadius, interactRange, interactableMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < sphereHits.Length; i++)
        {
            if (sphereHits[i].collider == null)
            {
                continue;
            }

            TobaccoPipeStation station = sphereHits[i].collider.GetComponentInParent<TobaccoPipeStation>();
            if (TryGetTobaccoPipeAimScore(station, ray, out float score) && score < bestScore)
            {
                bestScore = score;
                bestStation = station;
            }
        }

        TobaccoPipeStation[] stations = GetCachedTobaccoPipeStations();
        for (int i = 0; i < stations.Length; i++)
        {
            TobaccoPipeStation station = stations[i];
            if (TryGetTobaccoPipeAimScore(station, ray, out float score) && score < bestScore)
            {
                bestScore = score;
                bestStation = station;
            }
        }

        return bestStation;
    }

    private bool TryGetTobaccoPipeAimScore(TobaccoPipeStation station, Ray ray, out float score)
    {
        score = 0f;

        if (station == null || !station.isActiveAndEnabled || string.IsNullOrEmpty(station.GetInteractionPrompt()))
        {
            return false;
        }

        Vector3 aimPoint = station.GetAimAssistPoint();
        Vector3 fromCamera = aimPoint - _cam.transform.position;
        float distance = fromCamera.magnitude;
        float radius = Mathf.Max(stationAimAssistRadius, station.AimAssistRadius);
        if (distance <= 0.01f || distance > interactRange + radius)
        {
            return false;
        }

        float forwardDistance = Vector3.Dot(fromCamera, ray.direction);
        if (forwardDistance <= 0f || forwardDistance > interactRange + radius * 0.25f)
        {
            return false;
        }

        float distanceFromAimRay = (fromCamera - ray.direction * forwardDistance).magnitude;
        Vector3 viewportPoint = _cam.WorldToViewportPoint(aimPoint);
        Vector2 viewportOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);

        if (viewportPoint.z <= 0f || (distanceFromAimRay > radius && viewportOffset.magnitude > stationAimViewportRadius))
        {
            return false;
        }

        score = viewportOffset.sqrMagnitude * 100f + distanceFromAimRay + distance * 0.01f;
        return true;
    }

    private TobaccoPipeStation[] GetCachedTobaccoPipeStations()
    {
        if (_cachedTobaccoPipeStations == null || Time.time >= _nextStationCacheRefreshTime)
        {
            _cachedTobaccoPipeStations = FindObjectsByType<TobaccoPipeStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _nextStationCacheRefreshTime = Time.time + stationCacheRefreshInterval;
        }

        return _cachedTobaccoPipeStations ?? Array.Empty<TobaccoPipeStation>();
    }

    private bool TryInteractNearestTobaccoPipe(Ray ray)
    {
        TobaccoPipeStation[] stations = GetCachedTobaccoPipeStations();
        TobaccoPipeStation bestStation = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < stations.Length; i++)
        {
            TobaccoPipeStation station = stations[i];
            if (TryGetTobaccoPipeFallbackScore(station, ray, out float score) && score < bestScore)
            {
                bestScore = score;
                bestStation = station;
            }
        }

        if (bestStation == null)
        {
            return false;
        }

        bestStation.ForceInteractionReady();
        bestStation.Interact();
        return true;
    }

    private bool TryGetTobaccoPipeFallbackScore(TobaccoPipeStation station, Ray ray, out float score)
    {
        score = 0f;
        if (station == null || !station.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 aimPoint = station.GetAimAssistPoint();
        Vector3 fromCamera = aimPoint - _cam.transform.position;
        float distance = fromCamera.magnitude;
        float radius = Mathf.Max(Mathf.Max(stationAimAssistRadius, station.AimAssistRadius), 2.8f);
        float range = Mathf.Max(interactRange + radius, 7.2f);
        if (distance <= 0.01f || distance > range)
        {
            return false;
        }

        float forwardDistance = Vector3.Dot(fromCamera, ray.direction);
        if (forwardDistance <= 0f)
        {
            return false;
        }

        float distanceFromAimRay = (fromCamera - ray.direction * forwardDistance).magnitude;
        Vector3 viewportPoint = _cam.WorldToViewportPoint(aimPoint);
        if (viewportPoint.z <= 0f)
        {
            return false;
        }

        Vector2 viewportOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
        float viewportRadius = Mathf.Max(stationAimViewportRadius, 0.46f);
        bool closeEnough = distance <= Mathf.Max(2.6f, radius);
        bool aimedEnough = distanceFromAimRay <= radius * 1.25f || viewportOffset.magnitude <= viewportRadius;
        if (!closeEnough && !aimedEnough)
        {
            return false;
        }

        score = viewportOffset.sqrMagnitude * 100f + distanceFromAimRay + distance * 0.01f;
        return true;
    }

    private bool WasInteractPressed()
    {
        return Input.GetKeyDown(KeyCode.E) || CustomInputManager.GetKeyDown("Interact");
    }

    private void SetCrosshairTargeting(bool isTargeting)
    {
        if (_crosshairUI == null)
        {
            _crosshairUI = FindFirstObjectByType<AnhThoDien.UI.HUD.CrosshairUI>();
        }

        if (_crosshairUI != null)
        {
            _crosshairUI.SetTargeting(isTargeting);
        }
    }

    private void OnDisable()
    {
        // Xóa sạch chữ trên màn hình khi chuyển cảnh (script bị hủy/tắt)
        if (_promptText != null)
        {
            _promptText.text = "";
        }

        SetCrosshairTargeting(false);
    }
}



