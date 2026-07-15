using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class SetupNewDoor 
{
    public static void Run() 
    {
        string fbxPath = "Assets/_Project/Art/Models/Door/roller-shutters-animation/source/AnimatedRollingDoor.fbx";
        
        // 1. Force reimport with Animation enabled
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null) {
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.SaveAndReimport();
        }

        // 2. Find old door
        GameObject map = GameObject.Find("VietnamStreetV2");
        if (map == null) {
            Debug.LogError("Could not find VietnamStreetV2 map.");
            return;
        }

        Transform oldFrame = null;
        foreach (Transform child in map.GetComponentsInChildren<Transform>(true)) {
            if (child.name == "Cube_037_building_02_Door__Window_blind__Nor_0") {
                oldFrame = child;
            }
            if (child.name.StartsWith("Object_")) {
                int underscoreIndex = child.name.IndexOf('_');
                int dotIndex = child.name.IndexOf('.');
                if (dotIndex == -1) dotIndex = child.name.Length;
                
                if (underscoreIndex != -1 && dotIndex > underscoreIndex) {
                    string numStr = child.name.Substring(underscoreIndex + 1, dotIndex - underscoreIndex - 1);
                    if (int.TryParse(numStr, out int num)) {
                        if (num >= 30 && num <= 60) {
                            child.gameObject.SetActive(false); // Hide old slats
                        }
                    }
                }
            }
        }

        if (oldFrame == null) {
            Debug.LogError("Could not find old frame.");
            return;
        }

        // Hide old frame
        oldFrame.gameObject.SetActive(false);

        // 3. Create Animator Controller
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        if (clip == null) {
            Debug.LogError("No animation clip found in new FBX.");
            return;
        }

        string controllerPath = "Assets/_Project/Art/Models/Door/roller-shutters-animation/AnimatedRollingDoorController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var state = controller.layers[0].stateMachine.AddState("Rolling");
            state.motion = clip;
            
            // Set up speed parameter
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            state.speedParameterActive = true;
            state.speedParameter = "Speed";
        }

        // 4. Instantiate new door
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        GameObject newDoor = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        newDoor.name = "AnimatedRollingDoor_Instance";
        newDoor.transform.SetParent(map.transform);
        
        // Copy transform
        newDoor.transform.position = oldFrame.position;
        newDoor.transform.rotation = oldFrame.rotation;
        
        // Try to match scale - wait, oldFrame had a scale from its bone structure. 
        // We'll set it to 1,1,1 and adjust if needed, or copy world scale.
        // Actually, if it looks wrong we can scale it later. I will just copy localScale of oldFrame.
        newDoor.transform.localScale = oldFrame.localScale;

        // 5. Setup Components on new door
        Animator anim = newDoor.GetComponent<Animator>();
        if (anim == null) anim = newDoor.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.Play("Rolling", 0, 0f);
        anim.SetFloat("Speed", 0f);

        // Add BoxCollider for interaction
        BoxCollider col = newDoor.GetComponent<BoxCollider>();
        if (col == null) {
            col = newDoor.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.5f, 0);
            col.size = new Vector3(3f, 3f, 0.5f);
        }

        // Add the updated RollingDoorController
        // We will rewrite RollingDoorController.cs to use Animator.
        if (newDoor.GetComponent<RollingDoorController>() == null) {
            newDoor.AddComponent<RollingDoorController>();
        }

        Debug.Log("SetupNewDoor completed successfully!");
    }
}
