using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DevGameUnity.NPC;

namespace DevGameUnity.EditorTools
{
    public static class SetupTripoStreetNpcsTool
    {
        private const string VietnamStreetScenePath = "Assets/_Project/Scenes/Gameplay/VietnamStreet.unity";
        private struct NpcConfig
        {
            public string displayName;
            public string folderName;
            public string fbxName;
            public Vector3 scenePosition;
            public float sceneRotationY;
            public float walkDistance;
            public float movementSpeed;
        }

        private static readonly NpcConfig[] Npcs = new NpcConfig[]
        {
            new NpcConfig
            {
                displayName = "HK15",
                folderName = "HK15",
                fbxName = "tripo_convert_115dc50a-dd7d-4e15-a8cd-5422833d3436.fbx",
                scenePosition = new Vector3(40.0f, -0.02f, 12.5f),
                sceneRotationY = 90f,
                walkDistance = 25f,
                movementSpeed = 1.65f
            },
            new NpcConfig
            {
                displayName = "Nam1",
                folderName = "Nam 1",
                fbxName = "tripo_convert_026c0aa4-e338-42d5-9279-28294be0778d.fbx",
                scenePosition = new Vector3(-4.0f, -0.02f, 12.5f),
                sceneRotationY = 90f,
                walkDistance = 20f,
                movementSpeed = 1.60f
            },
            new NpcConfig
            {
                displayName = "Nam2",
                folderName = "Nam 2",
                fbxName = "tripo_convert_2a7c9025-602f-42aa-889a-77ed41ccbfbb.fbx",
                scenePosition = new Vector3(65.0f, -0.02f, 11.2f),
                sceneRotationY = -90f,
                walkDistance = 30f,
                movementSpeed = 1.70f
            },
            new NpcConfig
            {
                displayName = "Nam3",
                folderName = "Nam 3",
                fbxName = "tripo_convert_ca50e238-2bc3-4445-8a54-cd40333f8722.fbx",
                scenePosition = new Vector3(32.0f, -0.02f, 13.8f),
                sceneRotationY = -90f,
                walkDistance = 25f,
                movementSpeed = 1.62f
            },
            new NpcConfig
            {
                displayName = "AoBaBa",
                folderName = "Áo bà ba",
                fbxName = "tripo_convert_e7abf254-e7d0-4366-ba99-4fcffb0ee765.fbx",
                scenePosition = new Vector3(85.0f, -0.02f, 12.5f),
                sceneRotationY = -90f,
                walkDistance = 28f,
                movementSpeed = 1.65f
            }
        };

        [MenuItem("Tools/Thêm 5 NPC mới (HK15, Nam 1-3, Áo bà ba) Vào Đường Phố (VietnamStreet)")]
        public static void MenuSetupAndAddAll()
        {
            SetupAll(true);

            if (SceneManager.GetActiveScene().path.Replace("\\", "/") != VietnamStreetScenePath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(VietnamStreetScenePath, OpenSceneMode.Single);
                }
                else
                {
                    return;
                }
            }

            AddAllToSceneIfMissing(true);
        }

        public static void SetupAll(bool isInteractive)
        {
            int successCount = 0;
            for (int i = 0; i < Npcs.Length; i++)
            {
                if (SetupSingleNpc(Npcs[i], isInteractive))
                {
                    successCount++;
                }
            }

            if (isInteractive)
            {
                Debug.Log($"[AnhThoDien] Đã setup thành công {successCount}/{Npcs.Length} Prefab & AnimatorController cho các NPC mới!");
            }
        }

        private static bool SetupSingleNpc(NpcConfig config, bool isInteractive)
        {
            string folderPath = $"Assets/_Project/Art/models/Characters/TripoStreetNpc/{config.folderName}";
            string actualFbxPath = $"{folderPath}/{config.fbxName}";

            if (!File.Exists(actualFbxPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
                if (guids.Length > 0)
                {
                    actualFbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                }
                else
                {
                    if (isInteractive)
                    {
                        EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy file model FBX cho {config.displayName} tại {folderPath}", "OK");
                    }
                    return false;
                }
            }

            // 1. Cấu hình ModelImporter: Bật Loop Time, Loop Pose và Bake into Pose (Loop Position XZ)
            ModelImporter importer = AssetImporter.GetAtPath(actualFbxPath) as ModelImporter;
            if (importer != null)
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }
                if (clips != null && clips.Length > 0)
                {
                    bool needsReimport = false;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (!clips[i].loopTime || !clips[i].loopPose ||
                            !clips[i].keepOriginalPositionXZ || !clips[i].keepOriginalPositionY || !clips[i].keepOriginalOrientation ||
                            !clips[i].lockRootPositionXZ || !clips[i].lockRootHeightY || !clips[i].lockRootRotation)
                        {
                            clips[i].loopTime = true;
                            clips[i].loopPose = true;
                            clips[i].keepOriginalOrientation = true;
                            clips[i].keepOriginalPositionY = true;
                            clips[i].keepOriginalPositionXZ = true;
                            clips[i].lockRootRotation = true;
                            clips[i].lockRootHeightY = true;
                            clips[i].lockRootPositionXZ = true;
                            needsReimport = true;
                        }
                    }
                    if (needsReimport)
                    {
                        importer.clipAnimations = clips;
                        importer.SaveAndReimport();
                        Debug.Log($"[AnhThoDien] Đã bật Bake into Pose và Lock Root cho FBX của {config.displayName}.");
                    }
                }
            }

            // 2. Tìm AnimationClip đi bộ
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(actualFbxPath);
            AnimationClip walkClip = null;
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__") && clip.name != "Take 001")
                {
                    walkClip = clip;
                    break;
                }
            }
            if (walkClip == null)
            {
                foreach (var asset in assets)
                {
                    if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                    {
                        walkClip = clip;
                        break;
                    }
                }
            }

            // 3. Tạo/Cập nhật AnimatorController
            string controllerPath = $"Assets/_Project/Prefabs/NPC/{config.displayName}Controller.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            if (controller != null && controller.layers.Length > 0)
            {
                var rootStateMachine = controller.layers[0].stateMachine;
                AnimatorState walkState = null;
                foreach (var childState in rootStateMachine.states)
                {
                    if (childState.state.name == "Walk")
                    {
                        walkState = childState.state;
                        break;
                    }
                }
                if (walkState == null)
                {
                    walkState = rootStateMachine.AddState("Walk");
                    rootStateMachine.defaultState = walkState;
                }
                if (walkState.motion != walkClip && walkClip != null)
                {
                    walkState.motion = walkClip;
                    EditorUtility.SetDirty(controller);
                    AssetDatabase.SaveAssets();
                }
            }

            // 4. Tạo/Cập nhật Prefab chuẩn kích thước ngang bằng Remind (Scale 3x, VisualOffset 1.8x)
            string prefabPath = $"Assets/_Project/Prefabs/NPC/{config.displayName}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject prefabRoot;
            bool isNewPrefab = false;

            if (existingPrefab != null)
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            }
            else
            {
                prefabRoot = new GameObject(config.displayName);
                isNewPrefab = true;
            }

            prefabRoot.transform.localScale = new Vector3(3f, 3f, 3f);

            Transform visualOffset = prefabRoot.transform.Find("VisualOffset");
            if (visualOffset == null)
            {
                GameObject voObj = new GameObject("VisualOffset");
                visualOffset = voObj.transform;
                visualOffset.SetParent(prefabRoot.transform, false);
                visualOffset.localPosition = Vector3.zero;
                visualOffset.localRotation = Quaternion.identity;
            }
            visualOffset.localScale = new Vector3(1.8f, 1.8f, 1.8f);

            Transform modelChild = visualOffset.Find("Animated Character Model");
            if (modelChild == null)
            {
                GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(actualFbxPath);
                if (fbxAsset != null)
                {
                    GameObject modelInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
                    modelInst.name = "Animated Character Model";
                    modelInst.transform.SetParent(visualOffset, false);
                    modelInst.transform.localPosition = Vector3.zero;
                    modelInst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    modelChild = modelInst.transform;
                }
            }
            else
            {
                modelChild.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            var cc = prefabRoot.GetComponent<CharacterController>();
            if (cc == null) cc = prefabRoot.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.3f, 0); // 0.3 * 3 = 0.9m
            cc.height = 0.6f;                    // 0.6 * 3 = 1.8m
            cc.radius = 0.15f;                   // 0.15 * 3 = 0.45m

            // Xóa Animator gắn nhầm ở Root nếu có
            var rootAnimator = prefabRoot.GetComponent<Animator>();
            if (rootAnimator != null)
            {
                Object.DestroyImmediate(rootAnimator);
            }

            Animator animator = null;
            if (modelChild != null)
            {
                animator = modelChild.GetComponent<Animator>();
                if (animator == null) animator = modelChild.gameObject.AddComponent<Animator>();
            }
            else
            {
                animator = prefabRoot.GetComponentInChildren<Animator>();
                if (animator == null) animator = prefabRoot.AddComponent<Animator>();
            }

            if (controller != null && animator != null) animator.runtimeAnimatorController = controller;
            if (animator != null) animator.applyRootMotion = false;

            var walker = prefabRoot.GetComponent<StreetNpcWalker>();
            if (walker == null) walker = prefabRoot.AddComponent<StreetNpcWalker>();
            walker.animator = animator;
            walker.walkState = "Walk";
            walker.movementSpeed = config.movementSpeed;
            walker.turnSpeed = 5f;
            walker.arrivalDistance = 0.65f;
            walker.gravity = -24f;
            walker.walkDistance = config.walkDistance;
            walker.loopPatrol = true;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            if (!isNewPrefab && existingPrefab != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            else if (isNewPrefab)
            {
                Object.DestroyImmediate(prefabRoot);
            }

            return true;
        }

        private static void AddAllToSceneIfMissing(bool isInteractive)
        {
            int addedCount = 0;
            int updatedCount = 0;

            for (int i = 0; i < Npcs.Length; i++)
            {
                var config = Npcs[i];
                string prefabPath = $"Assets/_Project/Prefabs/NPC/{config.displayName}.prefab";
                GameObject existingGo = GameObject.Find(config.displayName);

                if (existingGo != null)
                {
                    var rootAnim = existingGo.GetComponent<Animator>();
                    if (rootAnim != null) Object.DestroyImmediate(rootAnim);

                    existingGo.transform.localScale = new Vector3(3f, 3f, 3f);
                    existingGo.transform.position = config.scenePosition;
                    existingGo.transform.rotation = Quaternion.Euler(0f, config.sceneRotationY, 0f);

                    Transform vo = existingGo.transform.Find("VisualOffset");
                    if (vo != null)
                    {
                        vo.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                        Transform mc = vo.Find("Animated Character Model");
                        if (mc != null) mc.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    }

                    var walker = existingGo.GetComponent<StreetNpcWalker>();
                    if (walker != null)
                    {
                        if (walker.animator == null) walker.animator = existingGo.GetComponentInChildren<Animator>();
                        walker.patrolPointA = existingGo.transform.position;
                        walker.patrolPointB = existingGo.transform.position + existingGo.transform.forward * walker.walkDistance;
                    }

                    updatedCount++;
                }
                else
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        inst.name = config.displayName;
                        inst.transform.position = config.scenePosition;
                        inst.transform.rotation = Quaternion.Euler(0f, config.sceneRotationY, 0f);
                        inst.transform.localScale = new Vector3(3f, 3f, 3f);

                        Transform vo = inst.transform.Find("VisualOffset");
                        if (vo != null)
                        {
                            vo.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                            Transform mc = vo.Find("Animated Character Model");
                            if (mc != null) mc.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        }

                        addedCount++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            if (isInteractive)
            {
                Debug.Log($"[AnhThoDien] Hoàn tất thêm {addedCount} NPC mới, cập nhật {updatedCount} NPC trong Scene VietnamStreet và đã lưu lại.");
                EditorUtility.DisplayDialog("Thành công",
                    $"Đã hoàn tất cấu hình và thêm 5 NPC vào đường phố VietnamStreet!\n\n" +
                    $"- Thêm mới: {addedCount} NPC\n" +
                    $"- Cập nhật: {updatedCount} NPC\n\n" +
                    $"Tất cả 5 NPC (HK15, Nam1, Nam2, Nam3, AoBaBa) đã được áp dụng chuẩn di chuyển của Remind (Bake pose chống giật lùi, đi tuần tra tự động loopPatrol dọc theo vỉa hè)!",
                    "OK");
            }
            else
            {
                Debug.Log("[AnhThoDien] Auto Setup: Đã tự động thêm/cập nhật 5 NPC mới (HK15, Nam 1-3, Áo bà ba) trong VietnamStreet.unity.");
            }
        }
    }
}
