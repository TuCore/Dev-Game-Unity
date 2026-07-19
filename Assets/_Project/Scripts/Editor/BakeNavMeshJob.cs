using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

public class BakeNavMeshJob
{
    [MenuItem("Tools/Navigation/Bake VietnamStreet NavMesh")]
    static void DoBake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        string scenePath = "Assets/_Project/Scenes/Gameplay/VietnamStreet.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface != null)
        {
            Debug.Log("Building NavMesh...");
            surface.BuildNavMesh();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("NavMesh baked successfully and scene saved.");
        }
        else
        {
            Debug.LogError("NavMeshSurface not found.");
        }
    }
}
