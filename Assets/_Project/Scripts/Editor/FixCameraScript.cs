using UnityEngine;
using UnityEditor;

public class FixCameraScript
{
    [MenuItem("Tools/Fix Player Camera")]
    public static void FixCamera()
    {
        PlayerCamera camScript = Object.FindAnyObjectByType<PlayerCamera>();
        if (camScript == null)
        {
            Debug.LogError("No PlayerCamera found!");
            return;
        }

        GameObject camObj = camScript.gameObject;
        
        // 1. Move camera up
        Vector3 localPos = camObj.transform.localPosition;
        Debug.Log("Old Camera local position: " + localPos);
        localPos.y = 1.6f; // Standard eye level
        localPos.z = 0f; // Make sure it's not offset forward, causing swinging!
        localPos.x = 0f;
        camObj.transform.localPosition = localPos;
        Debug.Log("New Camera local position: " + localPos);

        // 2. Remove colliders on the camera
        Collider[] colliders = camObj.GetComponents<Collider>();
        foreach(var col in colliders)
        {
            Debug.Log("Removing collider from camera: " + col.GetType().Name);
            Object.DestroyImmediate(col);
        }

        Rigidbody rb = camObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("Removing Rigidbody from camera");
            Object.DestroyImmediate(rb);
        }

        // 3. Let's also check the Player Body
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                Debug.Log($"CharacterController: Height={cc.height}, Radius={cc.radius}, Center={cc.center}");
                // Ensure center is valid
                cc.center = new Vector3(0, cc.height / 2f, 0);
            }
        }

        EditorUtility.SetDirty(camObj);
        if (player != null) EditorUtility.SetDirty(player.gameObject);
        
        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Fix applied and scene saved!");
    }
}
