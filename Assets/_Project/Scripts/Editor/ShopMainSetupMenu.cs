#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class ShopMainSetupMenu
{
    [MenuItem("AnhThoDien/Tạo Scene Phòng Ngủ (Shop_Main)")]
    public static void CreateShopScene()
    {
        // 1. Tạo scene trống mới
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 2. Cài đặt Ánh sáng
        GameObject dirLightObj = new GameObject("Directional Light");
        Light dirLight = dirLightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // 3. Load Model 3D phòng ngủ thực tế của Meshy
        string modelPath = "Assets/_Project/Art/Models/SaiGonEmptyRoom/source/Meshy_AI_Saigon_Empty_Room_Com_0709121931_image-to-3d-texture.fbx";
        GameObject roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (roomPrefab != null)
        {
            GameObject room = PrefabUtility.InstantiatePrefab(roomPrefab) as GameObject;
            room.name = "SaiGonRoomEnvironment";
            
            // Lật ngược phòng (Scale X = -1000) để thấy tường từ bên trong
            room.transform.localScale = new Vector3(-1000f, 1000f, 1000f); 
            // Đặt phòng theo yêu cầu
            room.transform.position = new Vector3(0, 8f, 0); 
            
            // Tự động ép Material (Texture) vào model luôn cho khỏi bị trắng bóc
            string texPath = "Assets/_Project/Art/Models/SaiGonEmptyRoom/textures/Meshy_AI_Saigon_Empty_Room_Com_0709121931_image-to-3d-texture.png";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                // Chuyển sang Unlit/Texture tự động để phòng sáng rực rỡ không bị tối sầm
                Material mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = tex;
                
                string matDir = "Assets/_Project/Art/Models/SaiGonEmptyRoom/Materials";
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Models/SaiGonEmptyRoom/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Art/Models/SaiGonEmptyRoom", "Materials");
                }
                AssetDatabase.CreateAsset(mat, matDir + "/RoomMat.mat");

                foreach (MeshRenderer mr in room.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.sharedMaterial = mat;
                }
            }
        }
        else
        {
            Debug.LogWarning("[AnhThoDien] Không tìm thấy model FBX tại: " + modelPath + ". Bạn có thể kéo tay vào sau.");
        }

        // 4. Tạo Sàn Tàng Hình 
        GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorObj.name = "Floor_Collider_Invisible";
        // Căn đúng mốc Y = 0 tuyệt đối theo ý bạn
        floorObj.transform.position = new Vector3(0, -0.05f, 0); 
        // Làm cho sàn tàng hình thật mỏng (Y = 0.1)
        floorObj.transform.localScale = new Vector3(100, 0.1f, 100);
        floorObj.GetComponent<MeshRenderer>().enabled = false; // Tàng hình

        // 5. Khởi tạo Nhân vật FPP (Player)
        GameObject playerObj = new GameObject("Player");
        // Đặt nhân vật thả rơi từ độ cao Y = 0.1
        playerObj.transform.position = new Vector3(0, 0.1f, 0); 
        playerObj.transform.rotation = Quaternion.Euler(0, 0, 0); // Mũi tên Z cắm thẳng vào trong tường
        playerObj.tag = "Player";
        
        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.7f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.85f, 0);
        
        playerObj.AddComponent<PlayerController>();

        // 6. Cài đặt Camera
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.transform.SetParent(playerObj.transform);
        cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0); // Ngang tầm mắt
        cameraObj.tag = "MainCamera";
        Camera cam = cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        
        PlayerCamera pCam = cameraObj.AddComponent<PlayerCamera>();
        pCam.playerBody = playerObj.transform;
        
        cameraObj.AddComponent<PlayerInteraction>();

        // 7. Tạo Vùng Tương tác cho Bàn Làm Việc (Workbench)
        GameObject workbenchObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        workbenchObj.name = "Workbench_Trigger";
        // Đặt trên sàn một chút (1.1)
        workbenchObj.transform.position = new Vector3(0, 1.1f, 2.5f); 
        workbenchObj.transform.localScale = new Vector3(1.5f, 1f, 1f);
        workbenchObj.GetComponent<MeshRenderer>().enabled = false; 
        workbenchObj.AddComponent<Workbench>();

        // 8. Lưu Scene lại vào thư mục chuẩn
        string sceneDir = "Assets/_Project/Scenes/Gameplay";
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes")) AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/Gameplay")) AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Gameplay");
        
        string scenePath = sceneDir + "/Shop_Main.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        Debug.Log("[AnhThoDien] Đã tạo Scene hoàn hảo 100%! Bấm Play ngay đi bạn!");
    }
}
#endif
