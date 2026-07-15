using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TaskHUDUI : MonoBehaviour
{
    private const int MaxVisibleTasks = 3;

    private static TaskHUDUI instance;

    private RectTransform panelRect;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI taskText;
    private Image panelImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        EnsureInstance();
    }

    public static TaskHUDUI EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<TaskHUDUI>();
        if (instance != null)
        {
            return instance;
        }

        GameObject hudObject = new GameObject("TaskHUDUI");
        instance = hudObject.AddComponent<TaskHUDUI>();
        DontDestroyOnLoad(hudObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        TaskManager manager = TaskManager.EnsureInstance();
        manager.OnTasksChanged += Refresh;
        manager.OnTaskCompleted += OnTaskCompleted;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTasksChanged -= Refresh;
            TaskManager.Instance.OnTaskCompleted -= OnTaskCompleted;
        }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "VietnamStreet")
        {
            BuildOrBindUI();
            Refresh();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        panelRect = null;
        titleText = null;
        taskText = null;
        panelImage = null;

        if (scene.name == "VietnamStreet")
        {
            BuildOrBindUI();
            Refresh();
        }
    }

    private void BuildOrBindUI()
    {
        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            return;
        }

        Transform existingPanel = canvas.transform.Find("TaskHUD_Panel");
        GameObject panelObject = existingPanel != null ? existingPanel.gameObject : new GameObject("TaskHUD_Panel", typeof(RectTransform));
        panelObject.transform.SetParent(canvas.transform, false);

        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-24f, -92f);
        panelRect.sizeDelta = new Vector2(430f, 168f);
        panelRect.localScale = Vector3.one;

        panelImage = GetOrAdd<Image>(panelObject);
        panelImage.color = new Color(0.015f, 0.018f, 0.024f, 0.82f);
        panelImage.raycastTarget = false;

        Shadow panelShadow = GetOrAdd<Shadow>(panelObject);
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        panelShadow.effectDistance = new Vector2(0f, -4f);

        UnityEngine.UI.Outline panelOutline = GetOrAdd<UnityEngine.UI.Outline>(panelObject);
        panelOutline.effectColor = new Color(1f, 0.78f, 0.28f, 0.2f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        titleText = FindOrCreateText("TaskHUD_Title", panelObject.transform);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(18f, -44f);
        titleRect.offsetMax = new Vector2(-18f, -12f);
        titleText.text = "NHI\u1ec6M V\u1ee4";
        titleText.fontSize = 21f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.color = new Color(1f, 0.84f, 0.45f, 1f);
        titleText.raycastTarget = false;

        taskText = FindOrCreateText("TaskHUD_List", panelObject.transform);
        RectTransform listRect = taskText.rectTransform;
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.offsetMin = new Vector2(18f, 14f);
        listRect.offsetMax = new Vector2(-18f, -48f);
        taskText.enableWordWrapping = true;
        taskText.enableAutoSizing = true;
        taskText.fontSizeMin = 15f;
        taskText.fontSizeMax = 19f;
        taskText.alignment = TextAlignmentOptions.TopLeft;
        taskText.color = new Color(0.93f, 0.94f, 0.96f, 1f);
        taskText.lineSpacing = 4f;
        taskText.raycastTarget = false;

        panelObject.SetActive(true);
    }

    private void Refresh()
    {
        if (panelRect == null || taskText == null)
        {
            BuildOrBindUI();
        }

        if (panelRect == null || taskText == null || TaskManager.Instance == null)
        {
            return;
        }

        List<GameTask> visibleTasks = TaskManager.Instance.GetVisibleTasks(MaxVisibleTasks);
        if (visibleTasks.Count == 0)
        {
            panelRect.gameObject.SetActive(false);
            return;
        }

        panelRect.gameObject.SetActive(true);
        taskText.text = BuildTaskList(visibleTasks);
    }

    private string BuildTaskList(List<GameTask> visibleTasks)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < visibleTasks.Count; i++)
        {
            GameTask task = visibleTasks[i];
            string icon = task.Completed ? "<color=#5dff83>✓</color>" : "<color=#ffd16a>•</color>";
            string progress = task.TargetProgress > 1 ? $" <color=#9fb0c7>{task.CurrentProgress}/{task.TargetProgress}</color>" : "";
            builder.Append(icon).Append(' ').Append(task.Title).Append(progress);
            if (i < visibleTasks.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private void OnTaskCompleted(GameTask task)
    {
        Refresh();
    }

    private Canvas FindHudCanvas()
    {
        GameObject hudCanvasObject = GameObject.Find("HUD_Canvas");
        Canvas canvas = hudCanvasObject != null ? hudCanvasObject.GetComponent<Canvas>() : null;
        if (canvas != null)
        {
            return canvas;
        }

        return FindFirstObjectByType<Canvas>();
    }

    private TextMeshProUGUI FindOrCreateText(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            TextMeshProUGUI existingText = existing.GetComponent<TextMeshProUGUI>();
            if (existingText != null)
            {
                return existingText;
            }
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
