using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SyncWithSceneView : MonoBehaviour
{
    [Header("Bật/Tắt chế độ đi theo Scene View")]
    public bool isSyncing = false;

    [Header("Main Camera của nhân vật")]
    public Transform headCamera;

#if UNITY_EDITOR
    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (isSyncing && !Application.isPlaying)
        {
            Vector3 sceneCamPos = sceneView.camera.transform.position;
            
            if (headCamera != null)
            {
                // Dịch chuyển toàn bộ nhân vật sao cho Camera của nhân vật khớp với Scene Camera
                transform.position = sceneCamPos - (headCamera.position - transform.position);
                
                // Copy góc xoay ngang (Y) cho nhân vật
                Vector3 euler = sceneView.camera.transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0, euler.y, 0);
                
                // Copy góc xoay dọc (X) cho Camera của nhân vật
                headCamera.localRotation = Quaternion.Euler(euler.x, 0, 0);
            }
        }
    }
#endif
}
