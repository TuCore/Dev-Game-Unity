using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using AnhThoDien.UI.Menu;

public class GenerateUISliders
{
    [MenuItem("Tools/Generate Pause Menu Sliders")]
    public static void Generate()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/VietnamStreet.unity");
        
        var canvasObj = GameObject.Find("PauseMenu_Canvas");
        if (canvasObj == null) 
        {
            Debug.LogError("Canvas not found");
            return;
        }
        
        var pauseMenuScript = canvasObj.GetComponent<PauseMenuUI>();
        var panelObj = pauseMenuScript.pausePanel;
        
        // 1. Brightness Overlay
        var oldOverlay = GameObject.Find("BrightnessOverlay");
        if (oldOverlay != null) GameObject.DestroyImmediate(oldOverlay);
        
        GameObject overlayObj = new GameObject("BrightnessOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        overlayObj.transform.SetAsFirstSibling();
        var overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0);
        overlayImg.raycastTarget = false;
        var overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        
        pauseMenuScript.brightnessOverlay = overlayImg;
        
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
        
        pauseMenuScript.sliderVolume = CreateSlider("Slider_Volume", "Âm lượng", 160, 0f, 1f);
        pauseMenuScript.sliderSensitivity = CreateSlider("Slider_Sensitivity", "Chuột", 100, 10f, 300f);
        pauseMenuScript.sliderBrightness = CreateSlider("Slider_Brightness", "Độ sáng", 40, 0f, 1f);
        
        if (pauseMenuScript.btnResume != null) pauseMenuScript.btnResume.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);
        if (pauseMenuScript.btnSave != null) pauseMenuScript.btnSave.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -110);
        if (pauseMenuScript.btnQuit != null) pauseMenuScript.btnQuit.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("Sliders and overlay generated successfully!");
    }
}
