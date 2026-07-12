using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using AnhThoDien.UI;

public class GenerateTutorialUI
{
    [MenuItem("Tools/Generate Tutorial UI")]
    public static void Generate()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/VietnamStreet.unity");
        
        var oldCanvas = GameObject.Find("Tutorial_Canvas");
        if (oldCanvas != null) GameObject.DestroyImmediate(oldCanvas);
        
        // 1. Create Canvas
        GameObject canvasObj = new GameObject("Tutorial_Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // Ensure it's on top of everything
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 2. Add TutorialUI script
        var tutorialUI = canvasObj.AddComponent<TutorialUI>();
        
        // 3. Create Panel (Background overlay)
        GameObject panelObj = new GameObject("TutorialPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        var panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.8f); // Dark semi-transparent
        
        // 4. Create the Notebook/Board Background
        GameObject boardObj = new GameObject("Board");
        boardObj.transform.SetParent(panelObj.transform, false);
        var boardRect = boardObj.AddComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.5f, 0.5f);
        boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(800, 600);
        
        var boardImg = boardObj.AddComponent<Image>();
        boardImg.color = new Color(0.95f, 0.92f, 0.85f, 1f); // Paper-like color
        
        // 5. Create Title Text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(boardObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 100);
        titleRect.anchoredPosition = Vector2.zero;
        
        var titleTxt = titleObj.AddComponent<Text>();
        titleTxt.text = "HƯỚNG DẪN THAO TÁC";
        titleTxt.fontSize = 40;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        
        // 6. Create Content Text
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(boardObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 0.2f);
        contentRect.anchorMax = new Vector2(0.9f, 0.8f);
        contentRect.sizeDelta = Vector2.zero;
        
        var contentTxt = contentObj.AddComponent<Text>();
        contentTxt.text = 
            "<b>[ W ] [ A ] [ S ] [ D ]</b> : Di chuyển nhân vật\n\n" +
            "<b>[ Chuột ]</b> : Xoay góc nhìn\n\n" +
            "<b>[ Chuột Trái ]</b> : Tương tác / Cầm đồ\n\n" +
            "<b>[ Chuột Phải ]</b> : Bỏ đồ xuống / Hủy\n\n" +
            "<b>[ E ]</b> : Phím chức năng phụ\n\n" +
            "<b>[ ESC ]</b> : Tạm dừng & Cài đặt";
        contentTxt.fontSize = 28;
        contentTxt.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        contentTxt.alignment = TextAnchor.MiddleLeft;
        contentTxt.lineSpacing = 1.2f;
        
        // 7. Create Got It Button
        GameObject btnObj = new GameObject("Btn_GotIt");
        btnObj.transform.SetParent(boardObj.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0);
        btnRect.anchorMax = new Vector2(0.5f, 0);
        btnRect.pivot = new Vector2(0.5f, 0);
        btnRect.sizeDelta = new Vector2(250, 60);
        btnRect.anchoredPosition = new Vector2(0, 40);
        
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f); // Greenish
        var btn = btnObj.AddComponent<Button>();
        
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var bTextRect = btnTextObj.AddComponent<RectTransform>();
        bTextRect.anchorMin = Vector2.zero;
        bTextRect.anchorMax = Vector2.one;
        bTextRect.sizeDelta = Vector2.zero;
        
        var btnTxt = btnTextObj.AddComponent<Text>();
        btnTxt.text = "ĐÃ HIỂU!";
        btnTxt.fontSize = 24;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        
        // 8. Hook up references
        tutorialUI.tutorialPanel = panelObj;
        tutorialUI.btnGotIt = btn;
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("Tutorial UI generated successfully!");
    }
}
