using UnityEngine;

public abstract class BaseApp : MonoBehaviour
{
    [SerializeField] protected GameObject appScreen;

    public virtual void OpenApp()
    {
        if (appScreen != null) appScreen.SetActive(true);
        OnAppOpened();
    }

    public virtual void CloseApp()
    {
        if (appScreen != null) appScreen.SetActive(false);
        OnAppClosed();
    }

    // Cho phép các class con override lại
    protected virtual void OnAppOpened() { }
    protected virtual void OnAppClosed() { }
}
