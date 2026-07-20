using UnityEditor;
using UnityEngine;

public class GameTools
{
    [MenuItem("DevGame/Reset Tutorial Minigame")]
    public static void ResetTutorialMinigame()
    {
        PlayerPrefs.SetInt("HasPlayedFirstMinigame", 0);
        PlayerPrefs.Save();
        Debug.Log("Reset tutorial minigame flag! The next minigame will be Multimeter Diagnosis.");
    }
}
