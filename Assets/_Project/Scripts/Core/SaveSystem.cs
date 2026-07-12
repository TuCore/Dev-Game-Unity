using UnityEngine;

public static class SaveSystem
{
    public static void SavePlayerPosition(Vector3 position)
    {
        PlayerPrefs.SetFloat("PlayerPosX", position.x);
        PlayerPrefs.SetFloat("PlayerPosY", position.y);
        PlayerPrefs.SetFloat("PlayerPosZ", position.z);
        PlayerPrefs.SetInt("HasSaveGame", 1);
        PlayerPrefs.Save();
        Debug.Log("Game Saved! Player position: " + position);
    }

    public static bool TryLoadPlayerPosition(out Vector3 position)
    {
        if (PlayerPrefs.GetInt("HasSaveGame", 0) == 1)
        {
            float x = PlayerPrefs.GetFloat("PlayerPosX", 0f);
            float y = PlayerPrefs.GetFloat("PlayerPosY", 0f);
            float z = PlayerPrefs.GetFloat("PlayerPosZ", 0f);
            position = new Vector3(x, y, z);
            return true;
        }
        
        position = Vector3.zero;
        return false;
    }
}

