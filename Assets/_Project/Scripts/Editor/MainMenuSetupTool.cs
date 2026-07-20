#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using AnhThoDien.UI.Menu;
using TMPro;

namespace AnhThoDien.EditorTools
{
    public class MainMenuSetupTool : EditorWindow
    {
        [MenuItem("AnhThoDien/Setup/Generate Main Menu UI")]
        public static void GenerateMainMenu()
        {
            // 1. Create Canvas
            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // 2. Create Background (Optional, just a dark overlay)
            GameObject bgObj = new GameObject("BackgroundOverlay");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.4f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // 3. Create Menu Container (Left side)
            GameObject menuContainer = new GameObject("MenuContainer");
            menuContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform containerRect = menuContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 0.5f);
            containerRect.sizeDelta = new Vector2(500, 0);
            containerRect.anchoredPosition = new Vector2(100, 0);

            VerticalLayoutGroup vlg = menuContainer.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.spacing = 20;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(menuContainer.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "ANH THỢ ĐIỆN";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.8f, 0.2f); // Warm yellow
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 100);

            // Buttons
            Button btnNewGame = CreateButton(menuContainer.transform, "Btn_NewGame", "BẮT ĐẦU");
            Button btnContinue = CreateButton(menuContainer.transform, "Btn_Continue", "CHƠI TIẾP");
            Button btnSettings = CreateButton(menuContainer.transform, "Btn_Settings", "CÀI ĐẶT");
            Button btnCollection = CreateButton(menuContainer.transform, "Btn_Collection", "BỘ SƯU TẬP");
            Button btnInfo = CreateButton(menuContainer.transform, "Btn_Info", "THÔNG TIN");
            Button btnQuit = CreateButton(menuContainer.transform, "Btn_Quit", "THOÁT");

            // 4. Create Settings Panel
            GameObject settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform settingsRect = settingsPanel.AddComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.sizeDelta = new Vector2(600, 400);
            Image settingsBg = settingsPanel.AddComponent<Image>();
            settingsBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            // Settings Title
            GameObject sTitleObj = new GameObject("SettingsTitle");
            sTitleObj.transform.SetParent(settingsPanel.transform, false);
            TextMeshProUGUI sTitleText = sTitleObj.AddComponent<TextMeshProUGUI>();
            sTitleText.text = "TÙY CHỈNH";
            sTitleText.fontSize = 40;
            sTitleText.alignment = TextAlignmentOptions.Center;
            RectTransform sTitleRect = sTitleObj.GetComponent<RectTransform>();
            sTitleRect.anchorMin = new Vector2(0, 1);
            sTitleRect.anchorMax = new Vector2(1, 1);
            sTitleRect.sizeDelta = new Vector2(0, 60);
            sTitleRect.anchoredPosition = new Vector2(0, -30);

            // Settings close button
            Button btnCloseSettings = CreateButton(settingsPanel.transform, "Btn_Close", "ĐÓNG LẠI");
            RectTransform closeRect = btnCloseSettings.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0);
            closeRect.anchorMax = new Vector2(0.5f, 0);
            closeRect.anchoredPosition = new Vector2(0, 40);

            // Attach Scripts
            MainMenuUI menuUI = canvasObj.AddComponent<MainMenuUI>();
            menuUI.btnNewGame = btnNewGame;
            menuUI.btnContinue = btnContinue;
            menuUI.btnSettings = btnSettings;
            menuUI.btnQuit = btnQuit;
            menuUI.settingsPanel = settingsPanel;

            SettingsUI settingsUI = settingsPanel.AddComponent<SettingsUI>();
            SerializedObject settingsSO = new SerializedObject(settingsUI);
            settingsSO.FindProperty("btnClose").objectReferenceValue = btnCloseSettings;
            settingsSO.ApplyModifiedProperties();

            settingsPanel.SetActive(false); // Hide by default

            // Create EventSystem if needed
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Select Canvas
            Selection.activeGameObject = canvasObj;
            Debug.Log("Generated Main Menu successfully!");
        }

        private static Button CreateButton(Transform parent, string name, string text)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            
            Button btn = btnObj.AddComponent<Button>();
            
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1f, 0.8f, 0f, 1f);
            cb.pressedColor = new Color(0.8f, 0.6f, 0f, 1f);
            cb.colorMultiplier = 1.2f;
            btn.colors = cb;

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 50);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "[ " + text + " ]";
            tmp.fontSize = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            return btn;
        }
    }
}
#endif
