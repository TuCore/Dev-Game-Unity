using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class ImportRepairShop : EditorWindow
{
    [MenuItem("Tools/Setup Repair Shop in Vietnam Street")]
    public static void SetupRepairShop()
    {
        string destFolder = Path.Combine(Application.dataPath, "Models", "ModelsRepairShop");

        // Refresh database just in case
        AssetDatabase.Refresh();

        // 1. Create or load the Material and assign textures
        string matPath = "Assets/Models/ModelsRepairShop/VietnameseRepairShop/textures/RepairShopMat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
        }

        string texFolder = "Assets/Models/ModelsRepairShop/VietnameseRepairShop/textures/";
        
        // Load textures
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texFolder + "Meshy_AI_Vietnamese_Repair_Sho_0628192515_image-to-3d-texture.png");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texFolder + "Meshy_AI_Vietnamese_Repair_Sho_0628192515_image-to-3d-texture_normal.png");
        Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(texFolder + "Meshy_AI_Vietnamese_Repair_Sho_0628192515_image-to-3d-texture_metallic.png");
        Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(texFolder + "Meshy_AI_Vietnamese_Repair_Sho_0628192515_image-to-3d-texture_emission.png");

        // Fix Normal Map setting if needed
        if (normal != null)
        {
            string normalPath = AssetDatabase.GetAssetPath(normal);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        // Assign textures to material
        if (albedo != null) mat.SetTexture("_MainTex", albedo);
        if (normal != null) mat.SetTexture("_BumpMap", normal);
        if (metallic != null) mat.SetTexture("_MetallicGlossMap", metallic);
        if (emission != null)
        {
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", Color.white);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // 2. Open scene
        string scenePath = "Assets/_Project/Scenes/Gameplay/VietnamStreet.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 3. Load the FBX
        string fbxPath = "Assets/Models/ModelsRepairShop/VietnameseRepairShop/source/Vietnamese_Repair_Shop.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        if (prefab == null)
        {
            Debug.LogError("Could not find the FBX model at " + fbxPath);
            return;
        }

        // 4. Check if it already exists in the scene
        GameObject existingShop = GameObject.Find(prefab.name);
        if (existingShop != null)
        {
            ApplyTransformAndMaterial(existingShop, mat);
            Debug.Log("Repair Shop updated in the scene.");
            Selection.activeGameObject = existingShop;
            return;
        }

        // 5. Instantiate
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance != null)
        {
            ApplyTransformAndMaterial(instance, mat);
            
            // Attempt to parent to a house block if one exists
            GameObject houseBlock = GameObject.Find("Houses") ?? GameObject.Find("Environment");
            if (houseBlock != null)
            {
                instance.transform.SetParent(houseBlock.transform);
            }

            // Save changes
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Successfully placed and textured Repair Shop in the scene!");
            Selection.activeGameObject = instance;
            
            // Focus on it in Scene View
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }
        else
        {
            Debug.LogError("Failed to instantiate the prefab.");
        }
    }

    private static void ApplyTransformAndMaterial(GameObject instance, Material mat)
    {
        // Apply transform based on the new screenshot
        instance.transform.position = new Vector3(-9.84f, 6.42f, -6.74f);
        instance.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        instance.transform.localScale = new Vector3(910f, 650f, 800f);

        // Apply Material to MeshRenderer
        MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in renderers)
        {
            Material[] mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
            }
            renderer.sharedMaterials = mats;
        }
    }
}
