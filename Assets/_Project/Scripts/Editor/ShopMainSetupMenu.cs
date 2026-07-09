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

        // 3. Load Vỏ Phòng Ngủ (Shell) - Phục hồi scale 1000 theo đúng chuẩn VietnamStreet của bạn!
        GameObject roomObj = InstantiateModel("Assets/_Project/Art/Models/Bedroom/Bedroom/source/Meshy_AI_Saigon_Bedroom_Shell__0709181251_image-to-3d-texture.fbx", "Bedroom_Shell", new Vector3(0f, 5.578611f, 0f), Quaternion.Euler(-90, 0, 0), new Vector3(1000f, 1000f, 1000f));
        // Xóa TẤT CẢ Collider của phòng ngủ để nhân vật không bị văng lên mái nhà!
        foreach (Collider col in roomObj.GetComponentsInChildren<Collider>())
        {
            GameObject.DestroyImmediate(col);
        }

        // 4. Load Nội Thất (Đồ vật) - Scale 350 cho vừa với phòng 1000
        
        InstantiateModel("Assets/_Project/Art/Models/Bedroom/MetalWardrobeCabine/source/Meshy_AI_Metal_Wardrobe_Cabine_0709181835_image-to-3d-texture.fbx", "Wardrobe", new Vector3(-6.3f, 3.6f, 6.28f), Quaternion.Euler(-90, 180, 0), new Vector3(600f, 350f, 350f));
        InstantiateModel("Assets/_Project/Art/Models/Bedroom/BedWooden/source/Meshy_AI_Single_Bed_Wooden_Fra_0709181935_image-to-3d-texture.fbx", "Bed", new Vector3(-6.18f, 1.22f, -2.15f), Quaternion.Euler(-90, 0, 0), new Vector3(600f, 550f, 350f));
        InstantiateModel("Assets/_Project/Art/Models/Bedroom/WallMountedFan/source/Meshy_AI_Wall_Mounted_Fan_0709181901_image-to-3d-texture.fbx", "Fan", new Vector3(-8.58f, 7.14f, -2.24f), Quaternion.Euler(-90, 90, 0), new Vector3(100f, 100f, 100f));

        InstantiateModel("Assets/_Project/Art/Models/Bedroom/StudyDesk/source/Meshy_AI_Study_Desk_with_Lapto_0709182025_image-to-3d-texture.fbx", "StudyDesk", new Vector3(-0.43f, 2.09f, 6.02f), Quaternion.Euler(-90, 180, 0), new Vector3(250f, 200f, 200f));
        InstantiateModel("Assets/_Project/Art/Models/Bedroom/MiniFridgeSanyo/source/Meshy_AI_Mini_Fridge_Sanyo_0709181817_image-to-3d-texture.fbx", "Fridge", new Vector3(7.18f, 3.08f, -5.9f), Quaternion.Euler(-90, 0, 0), new Vector3(350f, 300f, 300f));

        InstantiateModel("Assets/_Project/Art/Models/Bedroom/SwingDoor/source/Meshy_AI_Vietnamese_Swing_Door_0709181310_image-to-3d-texture.fbx", "Door", new Vector3(6.26f, 1.3f, -2.5f), Quaternion.Euler(-90, 90, 0), new Vector3(350f, 350f, 350f));


        // 4. Tạo Sàn Tàng Hình 
        GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorObj.name = "Floor_Collider_Invisible";
        // Căn đúng mốc Y = 0 tuyệt đối theo ý bạn
        floorObj.transform.position = new Vector3(0, -0.05f, 0); 
        // Làm cho sàn tàng hình thật mỏng (Y = 0.1)
        floorObj.transform.localScale = new Vector3(100, 0.1f, 100);
        floorObj.GetComponent<MeshRenderer>().enabled = false; // Tàng hình

        // 5. Khởi tạo Nhân vật FPP (Player) từ Prefab
        GameObject playerObj = null;
        GameObject playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
        
        if (playerPrefab != null)
        {
            playerObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(playerPrefab);
            playerObj.name = "Player";
            playerObj.transform.position = new Vector3(0, 0.1f, 0); 
            playerObj.transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log("[ShopMainSetupMenu] Đã dùng Player Prefab!");
        }
        else
        {
            Debug.LogWarning("[ShopMainSetupMenu] Không tìm thấy Player.prefab tại 'Assets/_Project/Prefabs/Player.prefab'. Đang tạo Player cơ bản thay thế. (Hãy kéo Player từ VietnamStreet thành Prefab để dùng cho xịn nhé!)");
            
            playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(0, 0.1f, 0); 
            playerObj.transform.rotation = Quaternion.Euler(0, 0, 0); 
            playerObj.tag = "Player";
            
            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.85f, 0);
            
            PlayerController pController = playerObj.AddComponent<PlayerController>();
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(playerObj.transform);
            groundCheckObj.transform.localPosition = new Vector3(0, 0, 0); 
            
            UnityEditor.SerializedObject serializedPController = new UnityEditor.SerializedObject(pController);
            serializedPController.FindProperty("groundCheck").objectReferenceValue = groundCheckObj.transform;
            serializedPController.ApplyModifiedProperties();

            // Cài đặt Camera
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.transform.SetParent(playerObj.transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0); 
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            
            PlayerCamera pCam = cameraObj.AddComponent<PlayerCamera>();
            UnityEditor.SerializedObject serializedPCam = new UnityEditor.SerializedObject(pCam);
            serializedPCam.FindProperty("playerBody").objectReferenceValue = playerObj.transform;
            serializedPCam.ApplyModifiedProperties();
            
            cameraObj.AddComponent<PlayerInteraction>();
        }

        // 7. Tạo Vùng Tương tác cho Bàn Làm Việc (Workbench)
        GameObject workbenchObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        workbenchObj.name = "Workbench_Trigger";
        // Đặt trên sàn một chút (1.1)
        workbenchObj.transform.position = new Vector3(0, 1.1f, 2.5f); 
        workbenchObj.transform.localScale = new Vector3(1.5f, 1f, 1f);
        workbenchObj.GetComponent<MeshRenderer>().enabled = false; 
        workbenchObj.AddComponent<Workbench>();

        // 7.5. Tạo cửa chuyển cảnh (Door_To_Street)
        GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObj.name = "Door_To_Street";
        doorObj.transform.position = new Vector3(6.26f, 2.6f, -2.5f); // Ghi đè tọa độ bạn đã xếp cho trùng với cánh cửa gỗ
        doorObj.transform.localScale = new Vector3(1f, 2f, 0.5f);
        doorObj.GetComponent<MeshRenderer>().enabled = false; // Tàng hình
        
        // Ensure BoxCollider is a trigger
        Collider doorCol = doorObj.GetComponent<Collider>();
        if (doorCol != null) doorCol.isTrigger = true;

        SceneTransitionDoor transition = doorObj.AddComponent<SceneTransitionDoor>();
        UnityEditor.SerializedObject serializedTransition = new UnityEditor.SerializedObject(transition);
        serializedTransition.FindProperty("targetSceneName").stringValue = "VietnamStreet";
        serializedTransition.FindProperty("interactionPrompt").stringValue = "Bấm [E] để ra phố VietnamStreet";
        serializedTransition.ApplyModifiedProperties();

        // 7.8. Tạo UI Tâm ngắm (Crosshair) để dễ dàng tương tác
        GameObject hudObj = new GameObject("HUD_Canvas");
        Canvas canvas = hudObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        hudObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(hudObj.transform);
        UnityEngine.UI.Image crosshairImg = crosshairObj.AddComponent<UnityEngine.UI.Image>();
        crosshairImg.color = new Color(1, 1, 1, 0.8f); // Trắng hơi mờ
        crosshairImg.rectTransform.anchoredPosition = Vector2.zero;
        crosshairImg.rectTransform.sizeDelta = new Vector2(4, 4); // Chấm nhỏ 4x4 pixel ở giữa màn hình

        // 8. Lưu Scene lại vào thư mục chuẩn
        string sceneDir = "Assets/_Project/Scenes/Gameplay";
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes")) AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/Gameplay")) AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Gameplay");
        
        string scenePath = sceneDir + "/Shop_Main.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        Debug.Log("[AnhThoDien] Đã tạo Scene hoàn hảo 100%! Bấm Play ngay đi bạn!");
    }

    private static GameObject InstantiateModel(string path, string name, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            obj.transform.localScale = scale;

            // Xử lý Material 2 mặt (Double Sided) để luôn thấy tường từ bên trong
            string texPath = path.Replace("source", "textures").Replace(".fbx", ".png");
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                Shader doubleSidedShader = Shader.Find("Unlit/DoubleSidedTexture");
                if (doubleSidedShader == null) doubleSidedShader = Shader.Find("Unlit/Texture"); // Fallback
                
                Material mat = new Material(doubleSidedShader);
                mat.mainTexture = tex;
                foreach (MeshRenderer mr in obj.GetComponentsInChildren<MeshRenderer>())
                {
                    Material[] mats = mr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = mat;
                    }
                    mr.sharedMaterials = mats;
                }
            }

            return obj;
        }
        else
        {
            Debug.LogWarning("[AnhThoDien] Không tìm thấy model FBX tại: " + path);
            return null;
        }
    }

    [MenuItem("AnhThoDien/Thêm Tâm Ngắm (Crosshair) vào màn hình")]
    public static void AddCrosshair()
    {
        if (GameObject.Find("HUD_Canvas") != null)
        {
            Debug.Log("[AnhThoDien] Đã có HUD_Canvas rồi!");
            return;
        }

        GameObject hudObj = new GameObject("HUD_Canvas");
        Canvas canvas = hudObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        hudObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(hudObj.transform);
        UnityEngine.UI.Image crosshairImg = crosshairObj.AddComponent<UnityEngine.UI.Image>();
        crosshairImg.color = new Color(1, 1, 1, 0.8f); 
        crosshairImg.rectTransform.anchoredPosition = Vector2.zero;
        crosshairImg.rectTransform.sizeDelta = new Vector2(4, 4); 

        Debug.Log("[AnhThoDien] Đã thêm tâm ngắm thành công!");
    }
}
#endif
