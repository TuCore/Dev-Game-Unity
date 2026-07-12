using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneUIBuilder : MonoBehaviour
{
    private void Start()
    {
        // Tránh tạo đúp
        if (FindFirstObjectByType<PhoneManager>() != null) return;

        // Đảm bảo có EventSystem để nhận Click chuột
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        BuildPhoneUI();
    }

    private void BuildPhoneUI()
    {
        // 1. Phone Canvas
        GameObject canvasObj = new GameObject("Phone_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Đặt độ phân giải chuẩn

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Phone Container (Vỏ điện thoại - Bezel)
        GameObject phoneContainer = CreateUIObject("PhoneContainer", canvasObj.transform);
        RectTransform phoneRect = phoneContainer.GetComponent<RectTransform>();
        phoneRect.anchorMin = new Vector2(0.5f, 0.5f); // Chính giữa màn hình
        phoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        phoneRect.pivot = new Vector2(0.5f, 0.5f);
        phoneRect.anchoredPosition = new Vector2(0, 0); // Vị trí giữa
        phoneRect.sizeDelta = new Vector2(416, 816); // Kích thước bezel lớn hơn màn hình xíu

        Image phoneBg = phoneContainer.AddComponent<Image>();
        phoneBg.sprite = RoundedSpriteGenerator.GenerateRoundedRect(100, 100, 20, Color.white);
        phoneBg.type = Image.Type.Sliced;
        phoneBg.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Màu viền xám đen ánh kim
        
        // Màn hình điện thoại (Screen Mask) bo góc
        GameObject screenObj = CreateUIObject("ScreenMask", phoneContainer.transform);
        RectTransform screenRect = screenObj.GetComponent<RectTransform>();
        screenRect.anchorMin = new Vector2(0.5f, 0.5f); screenRect.anchorMax = new Vector2(0.5f, 0.5f);
        screenRect.pivot = new Vector2(0.5f, 0.5f);
        screenRect.anchoredPosition = Vector2.zero;
        screenRect.sizeDelta = new Vector2(400, 800);
        
        Image screenImage = screenObj.AddComponent<Image>();
        screenImage.sprite = RoundedSpriteGenerator.GenerateRoundedRect(100, 100, 18, Color.white);
        screenImage.type = Image.Type.Sliced;
        
        Mask screenMask = screenObj.AddComponent<Mask>();
        screenMask.showMaskGraphic = true; // Hiện nền trắng (sau đó bị che bởi wallpaper)

        // Khởi tạo PhoneManager trên Canvas (để script luôn Active và bắt được phím Tab)
        PhoneManager phoneManager = canvasObj.AddComponent<PhoneManager>();
        phoneManager.phoneContainer = phoneContainer;

        // 3. Home Screen (Nằm trong ScreenMask)
        GameObject homeScreen = CreateUIObject("HomeScreen", screenObj.transform);
        StretchRect(homeScreen.GetComponent<RectTransform>());
        Image homeBg = homeScreen.AddComponent<Image>();
        Sprite wallpaper = LoadSprite("wallpaper.png");
        if (wallpaper != null) homeBg.sprite = wallpaper;
        else homeBg.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        phoneManager.homeScreen = homeScreen;

        // --- Hardware UI ---
        // Dynamic Island
        GameObject island = CreateUIObject("DynamicIsland", screenObj.transform); // Bỏ vào screen để không lồi ra ngoài viền
        RectTransform islandRect = island.GetComponent<RectTransform>();
        islandRect.anchorMin = new Vector2(0.5f, 1); islandRect.anchorMax = new Vector2(0.5f, 1);
        islandRect.pivot = new Vector2(0.5f, 1);
        islandRect.sizeDelta = new Vector2(100, 26);
        islandRect.anchoredPosition = new Vector2(0, -5);
        Image islandBg = island.AddComponent<Image>();
        islandBg.sprite = RoundedSpriteGenerator.GenerateRoundedRect(100, 100, 50, Color.white);
        islandBg.type = Image.Type.Sliced;
        islandBg.color = new Color(0.02f, 0.02f, 0.02f, 1f);

        // Status Bar
        GameObject statusTime = CreateTextObj("Time", screenObj.transform, "14:30", 16, TextAlignmentOptions.Left);
        RectTransform timeRect = statusTime.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0, 1); timeRect.anchorMax = new Vector2(0, 1);
        timeRect.pivot = new Vector2(0, 1); timeRect.sizeDelta = new Vector2(100, 30); timeRect.anchoredPosition = new Vector2(25, -15);
        
        GameObject statusIcons = CreateTextObj("Icons", screenObj.transform, "5G [|||]", 16, TextAlignmentOptions.Right);
        RectTransform iconRect = statusIcons.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1, 1); iconRect.anchorMax = new Vector2(1, 1);
        iconRect.pivot = new Vector2(1, 1); iconRect.sizeDelta = new Vector2(100, 30); iconRect.anchoredPosition = new Vector2(-25, -15);

        // Home Indicator
        GameObject homeInd = CreateUIObject("HomeIndicator", screenObj.transform);
        RectTransform indRect = homeInd.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.5f, 0); indRect.anchorMax = new Vector2(0.5f, 0);
        indRect.pivot = new Vector2(0.5f, 0);
        indRect.sizeDelta = new Vector2(140, 5); indRect.anchoredPosition = new Vector2(0, 10);
        Image indBg = homeInd.AddComponent<Image>();
        indBg.sprite = RoundedSpriteGenerator.GenerateRoundedRect(100, 20, 10, Color.white);
        indBg.type = Image.Type.Sliced;
        indBg.color = new Color(1, 1, 1, 0.5f);

        // App Grid Container
        GameObject appsGrid = CreateUIObject("AppsGrid", screenObj.transform);
        RectTransform gridRect = appsGrid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0, 0);
        gridRect.anchorMax = new Vector2(1, 1);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.offsetMin = new Vector2(25, 200); // Lề trái
        gridRect.offsetMax = new Vector2(-25, -150); // Lề phải

        GridLayoutGroup gridLayout = appsGrid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(65, 65); // Kích thước icon tiêu chuẩn iOS
        gridLayout.spacing = new Vector2(30, 40); // 4 icon vừa khít chiều ngang 400px
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        // 4. Tạo các App Screens (Nằm trong ScreenMask)
        phoneManager.appsContainer = screenObj.transform;

        // Bank App
        BankApp bankApp = CreateAppScreen<BankApp>("BankAppScreen", screenObj.transform, "VPBank", new Color(0.1f, 0.1f, 0.12f));
        GameObject bankContent = CreateUIObject("Content", bankApp.transform);
        RectTransform bcRect = bankContent.GetComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(0, 0); bcRect.anchorMax = new Vector2(1, 1);
        bcRect.offsetMin = new Vector2(20, 20); bcRect.offsetMax = new Vector2(-20, -100);
        VerticalLayoutGroup bankLayout = bankContent.AddComponent<VerticalLayoutGroup>();
        bankLayout.spacing = 15; bankLayout.childControlHeight = false; bankLayout.childForceExpandHeight = false;
        bankLayout.childControlWidth = false; bankLayout.childForceExpandWidth = false;

        GameObject atmCard = CreateCard(bankContent.transform, new Vector2(360, 220), Color.white, 25);
        UIGradient atmGrad = atmCard.AddComponent<UIGradient>();
        atmGrad.color1 = new Color(0f, 0.7f, 0.5f); // Lục bảo (Emerald)
        atmGrad.color2 = new Color(0.05f, 0.15f, 0.35f); // Xanh biển sâu (Deep Navy)
        atmGrad.angle = 45f;
        
        CreateTextObj("BankName", atmCard.transform, "VPBank", 20, TextAlignmentOptions.TopLeft, true, new Vector2(20, -20), new Vector2(-20, -20)).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
        CreateTextObj("Visa", atmCard.transform, "VISA", 24, TextAlignmentOptions.TopRight, true, new Vector2(20, -20), new Vector2(-20, -20)).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold | FontStyles.Italic;
        CreateTextObj("CardNo", atmCard.transform, "**** **** **** 6868", 18, TextAlignmentOptions.TopLeft, true, new Vector2(20, -60), new Vector2(20, -60));
        CreateTextObj("BalLabel", atmCard.transform, "Số dư khả dụng", 14, TextAlignmentOptions.BottomLeft, true, new Vector2(20, 60), new Vector2(20, 60)).GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 1f, 0.8f);
        TextMeshProUGUI balanceText = CreateTextObj("BalanceText", atmCard.transform, "15,000,000 VNĐ", 32, TextAlignmentOptions.BottomLeft, true, new Vector2(20, 20), new Vector2(20, 20)).GetComponent<TextMeshProUGUI>();
        balanceText.fontStyle = FontStyles.Bold;
        SetPrivateField(bankApp, "balanceText", balanceText);

        // --- SECTION: KHOẢN VAY ---
        CreateTextObj("LoanTitle", bankContent.transform, "Tín dụng", 18, TextAlignmentOptions.Left).GetComponent<RectTransform>().sizeDelta = new Vector2(360, 30);
        
        GameObject loanCard = CreateCard(bankContent.transform, new Vector2(360, 110), new Color(0.12f, 0.12f, 0.15f), 15, false);
        TextMeshProUGUI loanInfoText = CreateTextObj("LoanInfo", loanCard.transform, "Đang tải...", 16, TextAlignmentOptions.TopLeft, true, new Vector2(15, -15), new Vector2(-15, -15)).GetComponent<TextMeshProUGUI>();
        SetPrivateField(bankApp, "loanInfoText", loanInfoText);

        GameObject btnLayoutObj = CreateUIObject("BtnLayout", bankContent.transform);
        btnLayoutObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 45);
        HorizontalLayoutGroup hLayout = btnLayoutObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 15;
        hLayout.childControlWidth = false; hLayout.childForceExpandWidth = false;
        hLayout.childAlignment = TextAnchor.MiddleCenter;

        GameObject borrowBtnObj = CreateCard(btnLayoutObj.transform, new Vector2(170, 45), new Color(0.1f, 0.6f, 0.3f), 12, false);
        CreateTextObj("Txt", borrowBtnObj.transform, "Vay Tiền", 18, TextAlignmentOptions.Center, true).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        Button borrowBtn = borrowBtnObj.AddComponent<Button>();
        SetPrivateField(bankApp, "borrowButton", borrowBtn);

        GameObject repayBtnObj = CreateCard(btnLayoutObj.transform, new Vector2(170, 45), new Color(0.8f, 0.2f, 0.2f), 12, false);
        CreateTextObj("Txt", repayBtnObj.transform, "Tất Toán", 18, TextAlignmentOptions.Center, true).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        Button repayBtn = repayBtnObj.AddComponent<Button>();
        SetPrivateField(bankApp, "repayButton", repayBtn);
        // --------------------------

        // --------------------------
        // --------------------------
        
        CreateAppIcon(appsGrid.transform, "Ngân Hàng", "bank.png", phoneManager, bankApp);

        // Bills App
        BillsApp billsApp = CreateAppScreen<BillsApp>("BillsAppScreen", screenObj.transform, "Thanh Toán", new Color(0.1f, 0.1f, 0.12f));
        GameObject billsContent = CreateUIObject("Content", billsApp.transform);
        RectTransform billCRect = billsContent.GetComponent<RectTransform>();
        billCRect.anchorMin = new Vector2(0, 0); billCRect.anchorMax = new Vector2(1, 1);
        billCRect.offsetMin = new Vector2(20, 100); billCRect.offsetMax = new Vector2(-20, -100); // Chừa nút Pay ở dưới
        VerticalLayoutGroup billLayout = billsContent.AddComponent<VerticalLayoutGroup>();
        billLayout.spacing = 15; billLayout.childControlHeight = false; billLayout.childForceExpandHeight = false;
        billLayout.childControlWidth = false; billLayout.childForceExpandWidth = false;
        
        GameObject eItem = CreateListItem(billsContent.transform, "⚡ Tiền điện", "500,000", Color.white);
        SetPrivateField(billsApp, "electricityText", eItem.transform.Find("Value").GetComponent<TextMeshProUGUI>());
        
        GameObject wItem = CreateListItem(billsContent.transform, "💧 Tiền nước", "100,000", Color.white);
        SetPrivateField(billsApp, "waterText", wItem.transform.Find("Value").GetComponent<TextMeshProUGUI>());
        
        GameObject rItem = CreateListItem(billsContent.transform, "🏠 Tiền trọ", "2,500,000", Color.white);
        SetPrivateField(billsApp, "rentText", rItem.transform.Find("Value").GetComponent<TextMeshProUGUI>());
        
        // Nút Pay bự ở đáy
        GameObject totalCard = CreateCard(billsApp.transform, new Vector2(360, 55), Color.white, 25); // Bo tròn nhiều
        UIGradient payGrad = totalCard.AddComponent<UIGradient>();
        payGrad.color1 = new Color(1f, 0.4f, 0f); // Cam đậm
        payGrad.color2 = new Color(1f, 0.7f, 0f); // Cam nhạt (Vàng)
        payGrad.angle = -45f;
        
        RectTransform tcRect = totalCard.GetComponent<RectTransform>();
        tcRect.anchorMin = new Vector2(0.5f, 0); tcRect.anchorMax = new Vector2(0.5f, 0);
        tcRect.pivot = new Vector2(0.5f, 0);
        tcRect.anchoredPosition = new Vector2(0, 20);
        CreateTextObj("PayLabel", totalCard.transform, "Thanh toán", 20, TextAlignmentOptions.Center, true).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        
        TextMeshProUGUI totalText = CreateTextObj("TotalValue", billsContent.transform, "3,100,000 VNĐ", 24, TextAlignmentOptions.Right).GetComponent<TextMeshProUGUI>();
        totalText.gameObject.SetActive(false);
        SetPrivateField(billsApp, "totalText", totalText);
        
        CreateAppIcon(appsGrid.transform, "Chi Phí", "bills.png", phoneManager, billsApp);

        // Tasks App
        TasksApp tasksApp = CreateAppScreen<TasksApp>("TasksAppScreen", screenObj.transform, "Nhiệm Vụ", new Color(0.1f, 0.1f, 0.12f));
        GameObject tasksContent = CreateUIObject("Content", tasksApp.transform);
        RectTransform taskCRect = tasksContent.GetComponent<RectTransform>();
        taskCRect.anchorMin = new Vector2(0, 0); taskCRect.anchorMax = new Vector2(1, 1);
        taskCRect.offsetMin = new Vector2(20, 20); taskCRect.offsetMax = new Vector2(-20, -100);
        VerticalLayoutGroup taskLayout = tasksContent.AddComponent<VerticalLayoutGroup>();
        taskLayout.spacing = 15; taskLayout.childControlHeight = false; taskLayout.childForceExpandHeight = false;
        taskLayout.childControlWidth = false; taskLayout.childForceExpandWidth = false;

        CreateTextObj("DateTitle", tasksContent.transform, "Hôm nay", 24, TextAlignmentOptions.Left).GetComponent<RectTransform>().sizeDelta = new Vector2(360, 40);

        CreateTaskItem(tasksContent.transform, "Thức dậy", true);
        CreateTaskItem(tasksContent.transform, "Làm việc kiếm tiền", false);
        CreateTaskItem(tasksContent.transform, "Đóng tiền trọ", false);

        TextMeshProUGUI tasksText = CreateTextObj("TasksText", tasksContent.transform, "", 18, TextAlignmentOptions.TopLeft).GetComponent<TextMeshProUGUI>();
        tasksText.gameObject.SetActive(false);
        SetPrivateField(tasksApp, "tasksText", tasksText);

        CreateAppIcon(appsGrid.transform, "Nhiệm Vụ", "tasks.png", phoneManager, tasksApp);

        // Chat App
        ChatApp chatApp = CreateAppScreen<ChatApp>("ChatAppScreen", screenObj.transform, "Chủ Nợ", new Color(0.1f, 0.1f, 0.12f));
        
        // Khu vực Chat History
        GameObject chatScroll = CreateUIObject("Scroll", chatApp.transform);
        RectTransform chatSRect = chatScroll.GetComponent<RectTransform>();
        chatSRect.anchorMin = new Vector2(0, 0); chatSRect.anchorMax = new Vector2(1, 1);
        chatSRect.offsetMin = new Vector2(20, 80); chatSRect.offsetMax = new Vector2(-20, -100); // Tránh header
        
        VerticalLayoutGroup chatLayout = chatScroll.AddComponent<VerticalLayoutGroup>();
        chatLayout.spacing = 10; chatLayout.childControlHeight = false; chatLayout.childForceExpandHeight = false;
        chatLayout.childControlWidth = false; chatLayout.childForceExpandWidth = false;
        chatLayout.childAlignment = TextAnchor.LowerCenter; // Từ dưới lên

        CreateChatBubble(chatScroll.transform, "Tới tháng rồi, lo đóng tiền trọ đi con!", false, 70, 260);
        CreateChatBubble(chatScroll.transform, "Dạ bác thư thư cho cháu vài ngày", true, 50, 260);
        CreateChatBubble(chatScroll.transform, "Nhanh lên đấy, không tao đuổi!", false, 50, 240);

        // Input Field (Dưới đáy)
        GameObject inputCard = CreateCard(chatApp.transform, new Vector2(360, 45), new Color(0.15f, 0.15f, 0.15f), 22);
        RectTransform inpRect = inputCard.GetComponent<RectTransform>();
        inpRect.anchorMin = new Vector2(0.5f, 0); inpRect.anchorMax = new Vector2(0.5f, 0);
        inpRect.pivot = new Vector2(0.5f, 0);
        inpRect.anchoredPosition = new Vector2(0, 20); // Cách đáy 20px
        
        CreateTextObj("Placeholder", inputCard.transform, "Nhập tin nhắn...", 15, TextAlignmentOptions.Left, true, new Vector2(20, 0), new Vector2(-50, 0)).GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.6f, 0.6f);
        
        // Nút Send giả
        GameObject sendBtn = CreateCard(inputCard.transform, new Vector2(35, 35), new Color(0.0f, 0.47f, 1f), 17);
        RectTransform sendRect = sendBtn.GetComponent<RectTransform>();
        sendRect.anchorMin = new Vector2(1, 0.5f); sendRect.anchorMax = new Vector2(1, 0.5f);
        sendRect.pivot = new Vector2(1, 0.5f);
        sendRect.anchoredPosition = new Vector2(-5, 0);
        CreateTextObj("Icon", sendBtn.transform, "➤", 16, TextAlignmentOptions.Center, true);

        TextMeshProUGUI chatText = CreateTextObj("ChatText", chatScroll.transform, "", 16, TextAlignmentOptions.TopLeft).GetComponent<TextMeshProUGUI>();
        chatText.gameObject.SetActive(false); // Ẩn đi vì chưa gắn logic thật, chỉ demo UI đẹp
        SetPrivateField(chatApp, "chatLogText", chatText);

        CreateAppIcon(appsGrid.transform, "Tin Nhắn", "chat.png", phoneManager, chatApp);

        // 5. Shop App (S-Market)
        ShopApp shopApp = CreateAppScreen<ShopApp>("ShopAppScreen", screenObj.transform, "S-Market", new Color(0.12f, 0.05f, 0.02f));
        GameObject shopContent = CreateUIObject("Content", shopApp.transform);
        RectTransform scRect = shopContent.GetComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0, 0); scRect.anchorMax = new Vector2(1, 1);
        scRect.offsetMin = new Vector2(20, 20); scRect.offsetMax = new Vector2(-20, -100);
        VerticalLayoutGroup shopLayout = shopContent.AddComponent<VerticalLayoutGroup>();
        shopLayout.spacing = 15; shopLayout.childControlHeight = false; shopLayout.childForceExpandHeight = false;
        shopLayout.childControlWidth = false; shopLayout.childForceExpandWidth = false;

        CreateShopItem(shopContent.transform, "Tụ điện", 50000, shopApp);
        CreateShopItem(shopContent.transform, "Dây đồng", 20000, shopApp);
        CreateShopItem(shopContent.transform, "Băng keo đen", 10000, shopApp);

        CreateAppIcon(appsGrid.transform, "Shopee", "shop.png", phoneManager, shopApp);

        // 6. Inventory App (Kho Đồ)
        InventoryApp invApp = CreateAppScreen<InventoryApp>("InventoryAppScreen", screenObj.transform, "Kho Đồ", new Color(0.1f, 0.05f, 0.15f));
        GameObject invContent = CreateUIObject("Content", invApp.transform);
        RectTransform icRect = invContent.GetComponent<RectTransform>();
        icRect.anchorMin = new Vector2(0, 0); icRect.anchorMax = new Vector2(1, 1);
        icRect.offsetMin = new Vector2(20, 20); icRect.offsetMax = new Vector2(-20, -100);
        
        TextMeshProUGUI invTxt = CreateTextObj("InvText", invContent.transform, "", 16, TextAlignmentOptions.TopLeft).GetComponent<TextMeshProUGUI>();
        invTxt.gameObject.SetActive(true);
        invApp.inventoryText = invTxt;
        
        CreateAppIcon(appsGrid.transform, "Kho Đồ", "inventory.png", phoneManager, invApp);

        // Đẩy các phần tử phần cứng lên trên cùng để che App (Z-Order)
        island.transform.SetAsLastSibling();
        statusTime.transform.SetAsLastSibling();
        statusIcons.transform.SetAsLastSibling();
        homeInd.transform.SetAsLastSibling();

        // Ẩn điện thoại lúc đầu
        phoneContainer.SetActive(false);
    }

    private Sprite LoadSprite(string fileName)
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "PhoneUI", fileName);
        if (System.IO.File.Exists(path))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
        }
        return null;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private GameObject CreateTextObj(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, bool stretch = false, Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        GameObject go = CreateUIObject(name, parent);
        if (stretch)
        {
            StretchRect(go.GetComponent<RectTransform>());
            go.GetComponent<RectTransform>().offsetMin = offsetMin;
            go.GetComponent<RectTransform>().offsetMax = offsetMax;
        }
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return go;
    }

    private T CreateAppScreen<T>(string name, Transform parent, string title, Color headerColor) where T : BaseApp
    {
        GameObject appObj = CreateUIObject(name, parent);
        StretchRect(appObj.GetComponent<RectTransform>());
        Image bg = appObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 1f); // Nền app đen (Dark Mode chuẩn)

        // Header (Cao hơn để chứa Dynamic Island)
        GameObject header = CreateUIObject("Header", appObj.transform);
        RectTransform hRect = header.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0, 1); hRect.anchorMax = new Vector2(1, 1);
        hRect.pivot = new Vector2(0.5f, 1);
        hRect.sizeDelta = new Vector2(0, 90);
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = headerColor;

        // Title (Đẩy xuống dưới Island)
        GameObject titleObj = CreateTextObj("Title", header.transform, title, 20, TextAlignmentOptions.Center);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0); titleRect.anchorMax = new Vector2(1, 0);
        titleRect.pivot = new Vector2(0.5f, 0);
        titleRect.anchoredPosition = new Vector2(0, 15);
        titleRect.sizeDelta = new Vector2(200, 30);
        titleObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Nút Back (thay cho nút Home bự chà bá)
        GameObject backBtnObj = CreateUIObject("BackBtn", header.transform);
        RectTransform btnRect = backBtnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0, 0); btnRect.anchorMax = new Vector2(0, 0);
        btnRect.pivot = new Vector2(0, 0);
        btnRect.anchoredPosition = new Vector2(15, 15);
        btnRect.sizeDelta = new Vector2(60, 30);
        
        Button btn = backBtnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => FindFirstObjectByType<PhoneManager>().ShowHomeScreen());
        Image btnImg = backBtnObj.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0); // Trong suốt để bắt click

        CreateTextObj("Txt", backBtnObj.transform, "< Back", 18, TextAlignmentOptions.Left, true).GetComponent<TextMeshProUGUI>().color = new Color(0.2f, 0.6f, 1f); // Màu xanh iOS

        // Separator Line (Viền phân cách Header rất mờ)
        GameObject line = CreateUIObject("Line", header.transform);
        RectTransform lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 0); lineRect.anchorMax = new Vector2(1, 0);
        lineRect.pivot = new Vector2(0.5f, 0);
        lineRect.sizeDelta = new Vector2(0, 1);
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(1f, 1f, 1f, 0.05f); 

        T appComp = appObj.AddComponent<T>();
        SetPrivateField(appComp, "appScreen", appObj);
        
        appObj.SetActive(false);
        return appComp;
    }

    private void CreateAppIcon(Transform parent, string label, string iconFileName, PhoneManager phoneManager, BaseApp targetApp)
    {
        // Vỏ nút
        GameObject iconBtn = CreateUIObject(label + "Btn", parent);
        Image btnImg = iconBtn.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0); // Trong suốt để bắt Raycast Click
        
        // Mask để bo góc Icon (Kích thước gốc theo cellSize)
        GameObject maskObj = CreateUIObject("IconMask", iconBtn.transform);
        StretchRect(maskObj.GetComponent<RectTransform>());
        Image maskImg = maskObj.AddComponent<Image>();
        maskImg.sprite = RoundedSpriteGenerator.GenerateRoundedRect(100, 100, 25, Color.white);
        maskImg.type = Image.Type.Sliced;
        Mask mask = maskObj.AddComponent<Mask>();
        mask.showMaskGraphic = false; // Chỉ lấy khuôn, che nền trắng

        // Ảnh nội dung
        GameObject imgObj = CreateUIObject("Image", maskObj.transform);
        StretchRect(imgObj.GetComponent<RectTransform>());
        Image img = imgObj.AddComponent<Image>();
        
        Sprite iconSprite = LoadSprite(iconFileName);
        if (iconSprite != null)
        {
            img.sprite = iconSprite;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Nền xám dự phòng
        }

        // Bắt sự kiện Click lên toàn bộ Icon
        Button btn = iconBtn.AddComponent<Button>();
        btn.onClick.AddListener(() => phoneManager.OpenApp(targetApp));

        GameObject txt = CreateTextObj("Label", iconBtn.transform, label, 13, TextAlignmentOptions.Center);
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0, 0); txtRect.anchorMax = new Vector2(1, 0);
        txtRect.pivot = new Vector2(0.5f, 1);
        txtRect.anchoredPosition = new Vector2(0, -8); // Khoảng cách chữ tới icon
        txtRect.sizeDelta = new Vector2(120, 30); // Cho nhãn text rộng ra để không bị vỡ chữ
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    private void CreateShopItem(Transform parent, string title, float price, ShopApp shopApp)
    {
        GameObject item = CreateCard(parent, new Vector2(360, 70), new Color(0.15f, 0.15f, 0.18f), 12, false);
        
        CreateTextObj("Title", item.transform, title, 18, TextAlignmentOptions.Left, true, new Vector2(20, 0), new Vector2(100, 0));
        
        GameObject valObj = CreateTextObj("Value", item.transform, price.ToString("N0") + "đ", 18, TextAlignmentOptions.Right, true, new Vector2(-110, 0), new Vector2(-110, 0));
        valObj.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.5f, 0f);

        // Buy Button
        GameObject btnObj = CreateCard(item.transform, new Vector2(75, 40), Color.white, 8, false);
        UIGradient btnGrad = btnObj.AddComponent<UIGradient>();
        btnGrad.color1 = new Color(1f, 0.4f, 0f);
        btnGrad.color2 = new Color(1f, 0.7f, 0f);
        btnGrad.angle = 90f;
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0.5f); btnRect.anchorMax = new Vector2(1, 0.5f);
        btnRect.pivot = new Vector2(1, 0.5f); btnRect.anchoredPosition = new Vector2(-15, 0);
        
        CreateTextObj("Txt", btnObj.transform, "Mua", 16, TextAlignmentOptions.Center, true).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        
        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(() => shopApp.BuyItem(title, price));
    }

    private GameObject CreateCard(Transform parent, Vector2 size, Color bgColor, int radius = 15, bool useEffects = true)
    {
        GameObject card = CreateUIObject("Card", parent);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Image bg = card.AddComponent<Image>();
        bg.sprite = RoundedSpriteGenerator.GenerateRoundedRect(100, 100, radius, Color.white);
        bg.type = Image.Type.Sliced;
        bg.color = bgColor;
        
        if (useEffects)
        {
            // Premium Effect: Viền Outline siêu mờ ảo giác kính
            UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            outline.effectDistance = new Vector2(1, -1);

            // Premium Effect: Đổ bóng xịn
            Shadow shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
            shadow.effectDistance = new Vector2(0, -3);
        }

        return card;
    }

    private GameObject CreateListItem(Transform parent, string title, string value, Color valueColor)
    {
        GameObject item = CreateCard(parent, new Vector2(360, 60), new Color(0.12f, 0.12f, 0.15f), 12, false);
        CreateTextObj("Title", item.transform, title, 18, TextAlignmentOptions.Left, true, new Vector2(20, 0), new Vector2(20, 0));
        GameObject valObj = CreateTextObj("Value", item.transform, value, 18, TextAlignmentOptions.Right, true, new Vector2(-20, 0), new Vector2(-20, 0));
        valObj.GetComponent<TextMeshProUGUI>().color = valueColor;
        return item;
    }

    private GameObject CreateTaskItem(Transform parent, string title, bool isDone)
    {
        GameObject item = CreateCard(parent, new Vector2(360, 50), new Color(0.15f, 0.15f, 0.18f), 12, false);
        
        // Checkbox icon
        GameObject checkObj = CreateUIObject("Check", item.transform);
        RectTransform checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f); checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.pivot = new Vector2(0, 0.5f); checkRect.anchoredPosition = new Vector2(15, 0);
        checkRect.sizeDelta = new Vector2(24, 24);
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.sprite = RoundedSpriteGenerator.GenerateRoundedRect(50, 50, 25, Color.white);
        checkImg.type = Image.Type.Sliced;
        checkImg.color = isDone ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.3f, 0.3f, 0.35f);

        GameObject titleObj = CreateTextObj("Title", item.transform, title, 18, TextAlignmentOptions.Left, true, new Vector2(50, 0), new Vector2(-20, 0));
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.color = isDone ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
        
        if (isDone)
        {
            // Thay vì gạch ngang (lỗi font fontStyle = FontStyles.Strikethrough), chỉ cần làm mờ text
            titleTxt.alpha = 0.5f;
        }

        return item;
    }

    private void CreateChatBubble(Transform parent, string text, bool isMine, int height = 50, int width = 250)
    {
        // Container full width để VerticalLayoutGroup xếp
        GameObject container = CreateUIObject("BubbleContainer", parent);
        RectTransform contRect = container.GetComponent<RectTransform>();
        contRect.sizeDelta = new Vector2(360, height);
        
        // Bubble thật sự (neo trái hoặc phải bên trong container)
        GameObject card = CreateCard(container.transform, new Vector2(width, height), isMine ? Color.white : new Color(0.18f, 0.18f, 0.2f), 18, false);
        if (isMine)
        {
            UIGradient cGrad = card.AddComponent<UIGradient>();
            cGrad.color1 = new Color(0.0f, 0.45f, 1f); // Xanh dương đậm
            cGrad.color2 = new Color(0.2f, 0.7f, 1f); // Xanh dương sáng
            cGrad.angle = 45f;
        }

        RectTransform cRect = card.GetComponent<RectTransform>();
        cRect.anchorMin = isMine ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
        cRect.anchorMax = isMine ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
        cRect.pivot = isMine ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
        cRect.anchoredPosition = Vector2.zero;

        CreateTextObj("Text", card.transform, text, 15, isMine ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, true, new Vector2(15, 0), new Vector2(-15, 0));
    }
}

// Trigger recompile
