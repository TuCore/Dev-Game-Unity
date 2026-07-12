using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using AnhThoDien.UI.Menu;

public class GenerateMainMenuSettings
{
    [MenuItem("Tools/Generate Main Menu Settings Sliders")]
    public static void Generate()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Menu/MainMenu.unity");
        
        var settingsUI = GameObject.FindObjectOfType<SettingsUI>(true);
        if (settingsUI == null) 
        {
            Debug.LogError("SettingsUI not found in scene!");
            return;
        }
        
        var panelObj = settingsUI.gameObject;
        
        // 2. Sliders
        Slider CreateSlider(string name, string labelText, float yPos, float min, float max) {
            var oldSlider = GameObject.Find(name);
            if (oldSlider != null) GameObject.DestroyImmediate(oldSlider);
            
            GameObject container = new GameObject(name);
            container.transform.SetParent(panelObj.transform, false);
            var cRect = container.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(400, 60);
            cRect.anchoredPosition = new Vector2(0, yPos);
            
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            var txt = labelObj.AddComponent<Text>();
            txt.text = labelText;
            txt.fontSize = 20;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            var lRect = labelObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0, 0);
            lRect.anchorMax = new Vector2(0.4f, 1);
            lRect.sizeDelta = Vector2.zero;
            
            GameObject sliderObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObj.transform.SetParent(container.transform, false);
            var sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.4f, 0.3f);
            sRect.anchorMax = new Vector2(1, 0.7f);
            sRect.sizeDelta = Vector2.zero;
            sRect.anchoredPosition = Vector2.zero;
            
            Slider slider = sliderObj.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            
            return slider;
        }
        
        settingsUI.sliderVolume = CreateSlider("Slider_Volume", "Âm lượng", 80, 0f, 1f);
        settingsUI.sliderSensitivity = CreateSlider("Slider_Sensitivity", "Chuột", 0, 10f, 300f);
        settingsUI.sliderBrightness = CreateSlider("Slider_Brightness", "Độ sáng", -80, 0f, 1f);
        
        // Remove old Close Button
        var oldBtn = GameObject.Find("Btn_Close");
        if (oldBtn != null) GameObject.DestroyImmediate(oldBtn);
        
        // Create Close Button
        GameObject btnObj = new GameObject("Btn_Close");
        btnObj.transform.SetParent(panelObj.transform, false);
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var btn = btnObj.AddComponent<Button>();
        
        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300, 60);
        rect.anchoredPosition = new Vector2(0, -180);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var btnTxt = textObj.AddComponent<Text>();
        btnTxt.text = "ĐÓNG LẠI";
        btnTxt.fontSize = 30;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.color = Color.white;
        var txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        
        settingsUI.btnClose = btn;
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("Main Menu Settings Sliders generated successfully!");
    }
}
