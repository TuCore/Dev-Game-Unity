using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DevGameUnity.NPC;

public class FixNpcTool : Editor
{
    [MenuItem("Tools/Sửa Lỗi NPC Tự Động")]
    public static void FixNPCs()
    {
        var walkers = FindObjectsOfType<StreetNpcWalker>();
        int fixedCount = 0;

        foreach (var walker in walkers)
        {
            var go = walker.gameObject;
            string npcName = go.name.Replace(" (1)", "").Replace(" (2)", "").Trim();

            // 1. Sửa CharacterController lún đất
            var cc = go.GetComponent<CharacterController>();
            if (cc != null)
            {
                // NPC đang bị phóng to gấp 3 lần (Scale = 3).
                // Do đó để kén va chạm cao 1.8m vừa người, ta phải chia 3:
                cc.center = new Vector3(0, 0.3f, 0); // 0.3 * 3 = 0.9m (ngay bụng)
                cc.height = 0.6f;                    // 0.6 * 3 = 1.8m (chiều cao)
                cc.radius = 0.15f;                   // 0.15 * 3 = 0.45m (bề ngang)
                EditorUtility.SetDirty(cc);
            }

            // 2. Tìm Animator và tự động gán Controller
            var animator = walker.animator;
            if (animator == null) animator = go.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                string searchName = npcName + "Controller";
                string[] guids = AssetDatabase.FindAssets(searchName + " t:AnimatorController");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                    animator.runtimeAnimatorController = controller;
                    EditorUtility.SetDirty(animator);

                    // 3. Tự động tìm state "Walk" và gán animation vào
                    if (controller is AnimatorController ac)
                    {
                        foreach (var layer in ac.layers)
                        {
                            foreach (var state in layer.stateMachine.states)
                            {
                                if (state.state.name == walker.walkState)
                                {
                                    // Tìm AnimationClip từ mô hình FBX của NPC này
                                    string[] modelGuids = AssetDatabase.FindAssets(npcName + " t:Model");
                                    if (modelGuids.Length > 0)
                                    {
                                        var modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[0]);
                                        var assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                                        AnimationClip walkClip = null;
                                        foreach (var asset in assets)
                                        {
                                            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                                            {
                                                walkClip = clip;
                                                break;
                                            }
                                        }
                                        if (walkClip != null && state.state.motion == null)
                                        {
                                            state.state.motion = walkClip;
                                            EditorUtility.SetDirty(ac);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 4. Tự động bật Loop Time cho file FBX
            string[] fbxGuids = AssetDatabase.FindAssets(npcName + " t:Model");
            if (fbxGuids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(fbxGuids[0]);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    var defaultClips = importer.defaultClipAnimations;
                    if (defaultClips != null && defaultClips.Length > 0)
                    {
                        bool needsReimport = false;
                        for (int i = 0; i < defaultClips.Length; i++)
                        {
                            if (!defaultClips[i].loopTime)
                            {
                                defaultClips[i].loopTime = true;
                                needsReimport = true;
                            }
                        }
                        if (needsReimport)
                        {
                            importer.clipAnimations = defaultClips;
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
            
            fixedCount++;
        }

        Debug.Log("Đã sửa tự động thành công cho " + fixedCount + " NPC!");
        EditorUtility.DisplayDialog("Thành công", "Đã sửa hoàn tất cho " + fixedCount + " NPC!\n- Kéo kén va chạm lên mặt đất\n- Gắn Animation đi bộ\n- Bật lặp lại Animation (Loop)", "OK");
    }
}
