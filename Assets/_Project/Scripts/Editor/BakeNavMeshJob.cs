using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

[InitializeOnLoad]
public class BakeNavMeshJob
{
    static BakeNavMeshJob()
    {
        EditorApplication.delayCall += DoBake;
    }

    static void DoBake()
    {
        if (SessionState.GetBool("NavMeshBaked", false)) return;
        SessionState.SetBool("NavMeshBaked", true);

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
