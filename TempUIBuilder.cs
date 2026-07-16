using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using AnhThoDien.UI.Menu;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class BuilderScript
{
    public static void Main()
    {
        // Find MainMenu_Canvas in active scene
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.name != "MainMenu_Canvas")
        {
            Debug.LogError("Could not find MainMenu_Canvas in the active scene!");
            return;
        }

        Transform canvasTransform = canvas.transform;
        
        // 1. Build Collection Panel
        GameObject collectionPanel = BuildPanel(canvasTransform, "CollectionPanel", "BỘ SƯU TẬP");
        BuildTabs(collectionPanel, new string[] { "Chiến tích", "Kho đồ nghề", "Bản vẽ kỹ thuật", "Thành tựu" }, new string[] {
            "Phòng trưng bày những món đồ bạn đã sửa chữa thành công (Đang xây dựng...)",
            "Kho đồ nghề của bạn từ lúc khởi nghiệp (Đang xây dựng...)",
            "Sơ đồ mạch điện và tài liệu kỹ thuật (Đang xây dựng...)",
            "Các huy hiệu và danh hiệu bạn đạt được (Đang xây dựng...)"
        });

        // 2. Build Info Panel
        GameObject infoPanel = BuildPanel(canvasTransform, "InfoPanel", "THÔNG TIN");
        BuildTabs(infoPanel, new string[] { "Sổ tay Thợ điện", "Cốt truyện", "Đội ngũ phát triển", "Thông tin phiên bản" }, new string[] {
            "Ghi chú các quy tắc an toàn, cách sử dụng đồng hồ vạn năng, mẹo phân biệt linh kiện...",
            "Giữa lòng Sài Gòn nhộn nhịp, trong một con hẻm nhỏ... Nơi những món đồ điện cũ kỹ được trao thêm một cơ hội sống. Một chàng trai quyết định cất tấm bằng kỹ sư để ra đời mở tiệm, sống đúng với đam mê mày mò thực tế.",
            "Phát triển bởi: TuCore\nCùng với sự hỗ trợ của các công cụ AI.",
            "Phiên bản: 1.0.0\nTính năng mới:\n- Thêm Bộ Sưu Tập và Thông Tin\n- Cập nhật map VietnamStreetV2\n- Hệ thống Intro mới"
        });

        // Setup MainMenuUI references
        MainMenuUI menuUI = canvas.GetComponent<MainMenuUI>();
        if (menuUI != null)
        {
            menuUI.collectionPanel = collectionPanel;
            menuUI.infoPanel = infoPanel;
            EditorUtility.SetDirty(menuUI);
        }

        // Hide panels by default
        collectionPanel.SetActive(false);
        infoPanel.SetActive(false);

        // Mark scene as dirty so it can be saved
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        Debug.Log("Generated Collection and Info panels successfully!");
    }

    private static GameObject BuildPanel(Transform parent, string name, string title)
    {
        // Panel Container
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000, 700);
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.98f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 50;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.8f, 0.2f);
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = new Vector2(0, 80);
        titleRect.anchoredPosition = new Vector2(0, -40);

        // Close Button
        GameObject btnObj = new GameObject("Btn_Close");
        btnObj.transform.SetParent(panel.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        Button btnClose = btnObj.AddComponent<Button>();
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0);
        btnRect.anchorMax = new Vector2(0.5f, 0);
        btnRect.sizeDelta = new Vector2(250, 60);
        btnRect.anchoredPosition = new Vector2(0, 50);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "[ ĐÓNG LẠI ]";
        tmp.fontSize = 26;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        // Wire close button
        btnClose.onClick.AddListener(() => panel.SetActive(false));
        // Add UIButtonSound dynamically if available later or right now? MainMenuUI adds it on Start

        return panel;
    }

    private static void BuildTabs(GameObject panel, string[] tabNames, string[] tabContents)
    {
        // Tab Buttons Container
        GameObject tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(panel.transform, false);
        RectTransform rowRect = tabRow.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.sizeDelta = new Vector2(-40, 60);
        rowRect.anchoredPosition = new Vector2(0, -120);

        HorizontalLayoutGroup hlg = tabRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.spacing = 10;

        // Content Area (Parent for all tab contents)
        GameObject contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(panel.transform, false);
        RectTransform caRect = contentArea.AddComponent<RectTransform>();
        caRect.anchorMin = new Vector2(0, 0);
        caRect.anchorMax = new Vector2(1, 1);
        caRect.offsetMin = new Vector2(20, 100); // bottom left (padding from close button)
        caRect.offsetMax = new Vector2(-20, -160); // top right (padding from tabs)

        MenuTabController tabController = panel.AddComponent<MenuTabController>();

        for (int i = 0; i < tabNames.Length; i++)
        {
            // Create Tab Button
            GameObject tabBtnObj = new GameObject("Tab_" + tabNames[i]);
            tabBtnObj.transform.SetParent(tabRow.transform, false);
            Image btnImg = tabBtnObj.AddComponent<Image>();
            Button tabBtn = tabBtnObj.AddComponent<Button>();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(tabBtnObj.transform, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = tabNames[i];
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            // Create Tab Content ScrollView
            GameObject tabContent = CreateScrollView(contentArea.transform, "Content_" + tabNames[i], tabContents[i]);
            
            MenuTabController.Tab tab = new MenuTabController.Tab();
            tab.tabButton = tabBtn;
            tab.contentPanel = tabContent;
            tabController.tabs.Add(tab);
        }
    }

    private static GameObject CreateScrollView(Transform parent, string name, string contentText)
    {
        // A simple panel with text for now, can be expanded to full ScrollView if needed
        GameObject contentObj = new GameObject(name);
        contentObj.transform.SetParent(parent, false);
        RectTransform rect = contentObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        Image bg = contentObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = contentText;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(20, 20);
        txtRect.offsetMax = new Vector2(-20, -20);

        return contentObj;
    }
}
