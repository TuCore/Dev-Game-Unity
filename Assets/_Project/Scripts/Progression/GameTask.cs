using System;
using UnityEngine;

[Serializable]
public class GameTask
{
    [SerializeField] private string id;
    [SerializeField] private string title;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(1)] private int targetProgress = 1;
    [SerializeField] private int currentProgress;
    [SerializeField] private bool completed;

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public int TargetProgress => Mathf.Max(1, targetProgress);
    public int CurrentProgress => Mathf.Clamp(currentProgress, 0, TargetProgress);
    public bool Completed => completed;
    public float NormalizedProgress => TargetProgress <= 0 ? 1f : (float)CurrentProgress / TargetProgress;

    public GameTask(string id, string title, string description, int targetProgress)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.targetProgress = Mathf.Max(1, targetProgress);
        currentProgress = 0;
        completed = false;
    }

    public bool AddProgress(int amount)
    {
        if (completed)
        {
            return false;
        }

        currentProgress = Mathf.Clamp(currentProgress + Mathf.Max(1, amount), 0, TargetProgress);
        if (currentProgress >= TargetProgress)
        {
            completed = true;
        }

        return true;
    }

    public bool Complete()
    {
        if (completed)
        {
            return false;
        }

        currentProgress = TargetProgress;
        completed = true;
        return true;
    }

    public void ResetProgress()
    {
        currentProgress = 0;
        completed = false;
    }
}
