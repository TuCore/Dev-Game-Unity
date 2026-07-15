using System.Collections.Generic;
using UnityEngine;
using System;

public static class CustomInputManager
{
    private static Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();

    public static Action OnKeysChanged;

    static CustomInputManager()
    {
        LoadKeys();
    }

    private static void LoadKeys()
    {
        // Default keys
        SetDefaultKey("Interact", KeyCode.E);
        SetDefaultKey("Secondary", KeyCode.F);
        SetDefaultKey("Jump", KeyCode.Space);
        SetDefaultKey("Run", KeyCode.LeftShift);
        SetDefaultKey("Phone", KeyCode.Tab);
        SetDefaultKey("Pause", KeyCode.Escape);
        
        SetDefaultKey("MoveForward", KeyCode.W);
        SetDefaultKey("MoveBackward", KeyCode.S);
        SetDefaultKey("MoveLeft", KeyCode.A);
        SetDefaultKey("MoveRight", KeyCode.D);
        
        // Debug and Minigame tools
        SetDefaultKey("MinigameAction", KeyCode.Space); // Used in soldering
        SetDefaultKey("CleanSpray", KeyCode.R);
        SetDefaultKey("CleanWipe", KeyCode.X);
    }

    private static void SetDefaultKey(string actionName, KeyCode defaultKey)
    {
        if (PlayerPrefs.HasKey("KeyBinding_" + actionName))
        {
            string keyStr = PlayerPrefs.GetString("KeyBinding_" + actionName);
            if (Enum.TryParse(keyStr, out KeyCode parsedKey))
            {
                keys[actionName] = parsedKey;
                return;
            }
        }
        keys[actionName] = defaultKey;
    }

    public static void SetKey(string actionName, KeyCode newKey)
    {
        if (keys.ContainsKey(actionName))
        {
            keys[actionName] = newKey;
            PlayerPrefs.SetString("KeyBinding_" + actionName, newKey.ToString());
            PlayerPrefs.Save();
            OnKeysChanged?.Invoke();
        }
    }

    public static KeyCode GetKeyForAction(string actionName)
    {
        if (keys.TryGetValue(actionName, out KeyCode key))
            return key;
        return KeyCode.None;
    }

    // Wrappers for Input
    public static bool GetKeyDown(string actionName)
    {
        return Input.GetKeyDown(GetKeyForAction(actionName));
    }

    public static bool GetKey(string actionName)
    {
        return Input.GetKey(GetKeyForAction(actionName));
    }

    public static bool GetKeyUp(string actionName)
    {
        return Input.GetKeyUp(GetKeyForAction(actionName));
    }

    public static Dictionary<string, KeyCode> GetAllKeys()
    {
        return new Dictionary<string, KeyCode>(keys);
    }
    
    public static float GetAxisHorizontal()
    {
        float val = 0;
        if (Input.GetKey(GetKeyForAction("MoveLeft"))) val -= 1;
        if (Input.GetKey(GetKeyForAction("MoveRight"))) val += 1;
        return val; // Simple mapping, doesn't do smooth dampening but sufficient
    }
    
    public static float GetAxisVertical()
    {
        float val = 0;
        if (Input.GetKey(GetKeyForAction("MoveBackward"))) val -= 1;
        if (Input.GetKey(GetKeyForAction("MoveForward"))) val += 1;
        return val;
    }
}
