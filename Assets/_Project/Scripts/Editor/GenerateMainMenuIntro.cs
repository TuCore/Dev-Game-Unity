using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using AnhThoDien.UI.Menu;

public class GenerateMainMenuIntro
{
    [MenuItem("Tools/Generate Main Menu Intro")]
    public static void Generate()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Menu/MainMenu.unity");
        
        var mainMenuUI = GameObject.FindObjectOfType<MainMenuUI>(true);
        if (mainMenuUI == null) 
        {
            Debug.LogError("MainMenuUI not found in scene!");
            return;
        }
        
        var canvasObj = mainMenuUI.gameObject;
        if (canvasObj.GetComponent<Canvas>() == null)
        {
            canvasObj = canvasObj.GetComponentInParent<Canvas>().gameObject;
        }
        
        var oldIntro = GameObject.Find("IntroPanel");
        if (oldIntro != null) GameObject.DestroyImmediate(oldIntro);
        
        GameObject introPanelObj = new GameObject("IntroPanel");
        introPanelObj.transform.SetParent(canvasObj.transform, false);
        introPanelObj.transform.SetAsLastSibling(); 
        
        var rect = introPanelObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        var img = introPanelObj.AddComponent<Image>();
        img.color = Color.black;
        
        GameObject textObj = new GameObject("IntroText");
        textObj.transform.SetParent(introPanelObj.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.1f);
        textRect.anchorMax = new Vector2(0.9f, 0.9f);
        textRect.sizeDelta = Vector2.zero;
        
        var txt = textObj.AddComponent<Text>();
        txt.text = "";
        txt.color = Color.white;
        txt.fontSize = 40; 
        txt.alignment = TextAnchor.MiddleCenter;
        
        GameObject skipObj = new GameObject("SkipHint");
        skipObj.transform.SetParent(introPanelObj.transform, false);
        var skipRect = skipObj.AddComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1, 0);
        skipRect.anchorMax = new Vector2(1, 0);
        skipRect.pivot = new Vector2(1, 0);
        skipRect.anchoredPosition = new Vector2(-20, 20);
        skipRect.sizeDelta = new Vector2(400, 50);
        
        var skipTxt = skipObj.AddComponent<Text>();
        skipTxt.text = "Nhấn phím bất kỳ để bỏ qua...";
        skipTxt.color = new Color(1, 1, 1, 0.5f);
        skipTxt.fontSize = 24;
        skipTxt.alignment = TextAnchor.LowerRight;
        skipTxt.fontStyle = FontStyle.Italic;
        
        mainMenuUI.introPanel = introPanelObj;
        mainMenuUI.introText = txt;
        
        introPanelObj.SetActive(false); // Hide by default
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        
        Debug.Log("Intro Panel generated successfully!");
    }
}
