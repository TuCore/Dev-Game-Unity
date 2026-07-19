using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DevGameUnity.NPC;

namespace DevGameUnity.EditorTools
{
    public static class SetupRemindNpcTool
    {
        private const string FbxPath = "Assets/_Project/Art/models/Characters/TripoStreetNpc/Remind/tripo_convert_b50c198b-73bb-43a1-b7be-ba3eb01904f1.fbx";
        private const string ControllerPath = "Assets/_Project/Prefabs/NPC/RemindController.controller";
        private const string PrefabPath = "Assets/_Project/Prefabs/NPC/Remind.prefab";
        private const string VietnamStreetScenePath = "Assets/_Project/Scenes/Gameplay/VietnamStreet.unity";
        [MenuItem("Tools/Thêm Remind Vào Đường Phố (VietnamStreet)")]
        public static void MenuSetupAndAddRemind()
        {
            SetupRemind(true);

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

            AddRemindToSceneIfMissing(true);
        }

        public static void SetupRemind(bool isInteractive)
        {
            string actualFbxPath = FbxPath;
            if (!File.Exists(actualFbxPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/models/Characters/TripoStreetNpc/Remind" });
                if (guids.Length > 0)
                {
                    actualFbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                }
                else
                {
                    if (isInteractive)
                    {
                        EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file model FBX của Remind trong thư mục Assets/_Project/Art/models/Characters/TripoStreetNpc/Remind/", "OK");
                    }
                    return;
                }
            }

            // 1. Cấu hình ModelImporter: bật Loop Time và Bake into Pose (Loop Position XZ) cho animation đi bộ để chống giật lùi
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
                            clips[i].keepOriginalPositionXZ = true; // Bake into Pose XZ: Ngăn xương root dịch chuyển/giật lùi
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
                        Debug.Log("[AnhThoDien] Đã bật Bake into Pose (Loop Position XZ) và Lock Root XZ cho FBX của Remind để triệt tiêu lỗi giật lùi.");
                    }
                }
            }

            // 2. Tìm AnimationClip đi bộ trong file FBX
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
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
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

            // 4. Tạo/Cập nhật Prefab Remind.prefab
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject prefabRoot;
            bool isNewPrefab = false;

            if (existingPrefab != null)
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            }
            else
            {
                prefabRoot = new GameObject("Remind");
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
            // Chuẩn hóa scale VisualOffset về 1.8 (Tổng scale 3 * 1.8 = 5.4x chuẩn ngang bằng các NPC Bao, Khoa, Son)
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
                    modelInst.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    modelChild = modelInst.transform;
                }
            }
            else
            {
                modelChild.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }

            var cc = prefabRoot.GetComponent<CharacterController>();
            if (cc == null) cc = prefabRoot.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.3f, 0); // 0.3 * 3 = 0.9m
            cc.height = 0.6f;                    // 0.6 * 3 = 1.8m
            cc.radius = 0.15f;                   // 0.15 * 3 = 0.45m

            // Xóa Animator gắn nhầm ở Root GameObject nếu có
            var rootAnimator = prefabRoot.GetComponent<Animator>();
            if (rootAnimator != null)
            {
                Object.DestroyImmediate(rootAnimator);
            }

            // Gắn Animator lên modelChild (mô hình FBX con bên trong VisualOffset) để khớp bone path
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
            walker.movementSpeed = 1.65f;
            walker.turnSpeed = 5f;
            walker.arrivalDistance = 0.65f;
            walker.gravity = -24f;
            walker.walkDistance = 22f;
            walker.loopPatrol = true;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            if (!isNewPrefab && existingPrefab != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            else if (isNewPrefab)
            {
                Object.DestroyImmediate(prefabRoot);
            }

            if (isInteractive)
            {
                Debug.Log("[AnhThoDien] Setup Prefab Remind.prefab chuẩn kích thước và Animator thành công.");
            }
        }

        private static void AddRemindToSceneIfMissing(bool isInteractive)
        {
            GameObject existingRemind = GameObject.Find("Remind");
            if (existingRemind != null)
            {
                // Cập nhật triệt để cả Position, Rotation (dọc theo đường phố) và Scale cho instance có sẵn trong Scene
                var rootAnim = existingRemind.GetComponent<Animator>();
                if (rootAnim != null) Object.DestroyImmediate(rootAnim);

                existingRemind.transform.localScale = new Vector3(3f, 3f, 3f);
                existingRemind.transform.position = new Vector3(18.0f, -0.02f, 12.5f);
                // Xoay 90 độ theo trục Y để hướng về trục +X (dọc theo đường phố, thay vì băng qua đường theo +Z)
                existingRemind.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                Transform vo = existingRemind.transform.Find("VisualOffset");
                if (vo != null)
                {
                    vo.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                    Transform mc = vo.Find("Animated Character Model");
                    if (mc != null) mc.localRotation = Quaternion.Euler(0f, -90f, 0f);
                }

                var walker = existingRemind.GetComponent<StreetNpcWalker>();
                if (walker != null)
                {
                    if (walker.animator == null) walker.animator = existingRemind.GetComponentInChildren<Animator>();
                    walker.patrolPointA = existingRemind.transform.position;
                    walker.patrolPointB = existingRemind.transform.position + existingRemind.transform.forward * walker.walkDistance;
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                if (isInteractive)
                {
                    Debug.Log("[AnhThoDien] Đã cập nhật lại thông số, hướng đi và kích thước cho nhân vật Remind có sẵn trong Scene VietnamStreet.");
                    Selection.activeGameObject = existingRemind;
                    if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
                    EditorUtility.DisplayDialog("Thông báo", "Nhân vật Remind trong Scene VietnamStreet đã được cập nhật hoàn chỉnh:\n- Hướng đi: Dọc theo con đường (trục X)\n- Kích thước: Chuẩn ngang bằng các NPC nam (5.4x)\n- Vị trí: " + existingRemind.transform.position, "OK");
                }
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                GameObject remindInst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                remindInst.name = "Remind";
                remindInst.transform.position = new Vector3(18.0f, -0.02f, 12.5f);
                remindInst.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                remindInst.transform.localScale = new Vector3(3f, 3f, 3f);

                Transform vo = remindInst.transform.Find("VisualOffset");
                if (vo != null)
                {
                    vo.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                    Transform mc = vo.Find("Animated Character Model");
                    if (mc != null) mc.localRotation = Quaternion.Euler(0f, -90f, 0f);
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                if (isInteractive)
                {
                    Selection.activeGameObject = remindInst;
                    if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
                    Debug.Log("[AnhThoDien] Đã thêm nhân vật Remind vào Scene VietnamStreet và lưu lại!");
                    EditorUtility.DisplayDialog("Thành công", "Đã thêm nhân vật Remind vào cảnh đường phố (VietnamStreet)!\n- Prefab: Assets/_Project/Prefabs/NPC/Remind.prefab\n- Vị trí: (18.0, -0.02, 12.5) trên vỉa hè\n- Hướng đi: Dọc theo con đường (trục X)\n- Chế độ: Đi qua đi lại tự động (Loop Patrol)", "OK");
                }
                else
                {
                    Debug.Log("[AnhThoDien] Auto Setup: Đã tự động thêm Remind vào VietnamStreet.unity.");
                }
            }
        }
    }
}
