using TMPro;
using UnityEngine;

public class TasksApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI tasksText;

    private bool legacyItemsHidden;

    private void OnEnable()
    {
        TaskManager manager = TaskManager.EnsureInstance();
        manager.OnTasksChanged += RefreshTasks;
    }

    private void OnDisable()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTasksChanged -= RefreshTasks;
        }
    }

    protected override void OnAppOpened()
    {
        RefreshTasks();
    }

    private void RefreshTasks()
    {
        if (tasksText == null)
        {
            return;
        }

        HideLegacyTaskItems();

        tasksText.gameObject.SetActive(true);
        tasksText.enableWordWrapping = true;
        tasksText.enableAutoSizing = true;
        tasksText.fontSizeMin = 13f;
        tasksText.fontSizeMax = 18f;
        tasksText.alignment = TextAlignmentOptions.TopLeft;
        tasksText.rectTransform.sizeDelta = new Vector2(360f, 520f);
        tasksText.text = TaskManager.EnsureInstance().GetPhoneTaskText();
    }

    private void HideLegacyTaskItems()
    {
        if (legacyItemsHidden || tasksText == null || tasksText.transform.parent == null)
        {
            return;
        }

        Transform parent = tasksText.transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == tasksText.transform || child.name == "DateTitle")
            {
                continue;
            }

            if (child.name == "Card")
            {
                child.gameObject.SetActive(false);
            }
        }

        legacyItemsHidden = true;
    }
}
