using DevGameUnity.Player;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevGameUnity.EditorTools
{
    [InitializeOnLoad]
    public static class VietnamStreetFbxSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/DeliveryStreetPrototype.unity";
        private const string FbxPath = "Assets/_Project/Art/Models/VietnamOldTown/VietnamStreet.fbx";
        private const string TextureFolder = "Assets/_Project/Art/Models/VietnamOldTown/textures";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/VietnamStreetTextured";
        private const string AdjustedMaterialFolder = "Assets/_Project/Art/Materials/VietnamStreetAdjusted";
        private const string PlayerMaterialFolder = "Assets/_Project/Art/Materials/PlayerPrototype";
        private const string PlayerName = "First Person Test Player";
        private const string ImportedRootName = "Imported Vietnam Old Town FBX";
        private const string GeneratedStreetName = "Playable Straight Delivery Street";
        private const string OverviewCameraName = "Prototype Camera - street overview";
        private const string AutoSetupKey = "DevGameUnity.VietnamStreetFbxSceneSetup.SoftLightingV6";

        static VietnamStreetFbxSceneSetup()
        {
            EditorApplication.delayCall += AutoSetup;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Dev Game/Use Imported Vietnam Street Only")]
        public static void SetupMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetupScene(scene);
        }

        [DidReloadScripts]
        private static void SetupAfterScriptsReload()
        {
            SessionState.SetBool(AutoSetupKey, false);
            EditorApplication.delayCall += AutoSetup;
        }

        private static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AutoSetupKey, false))
            {
                return;
            }

            if (!System.IO.File.Exists(ScenePath))
            {
                return;
            }

            SessionState.SetBool(AutoSetupKey, true);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetupScene(scene);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            SessionState.SetBool(AutoSetupKey, false);
            EditorApplication.delayCall += AutoSetup;
        }

        private static void SetupScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            DeleteIfFound(GeneratedStreetName);
            DeleteIfFound(OverviewCameraName);

            var importedRoot = ReimportFreshStreetInstance();
            if (importedRoot == null)
            {
                return;
            }

            AdjustDarkStreetMaterials(importedRoot);
            EnsureMeshColliders(importedRoot);
            PlacePlayerOnImportedStreet(importedRoot);
            EnsureBalancedLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("VietnamStreet FBX scene setup complete: generated placeholder street removed, original FBX materials restored, soft lighting applied, player placed on imported street.");
        }

        private static void DeleteIfFound(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject ReimportFreshStreetInstance()
        {
            AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Could not load FBX at {FbxPath}. Copy the original VietnamStreet.fbx into the project first.");
                return null;
            }

            DeleteIfFound(ImportedRootName);
            var root = new GameObject(ImportedRootName);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "VietnamStreet_FBX";
            instance.transform.SetParent(root.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return root;
        }

        private static void EnsureTexturedMaterials(GameObject importedRoot)
        {
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");
            EnsureFolder(MaterialFolder);
            ImportTextureSettings();

            var paving = CreateMaterial("MAT_Street_PavingStones", "PavingStones078_2K_Color.png", "PavingStones078_2K_NormalGL.png", new Color(0.58f, 0.56f, 0.52f), 0.45f);
            var wall = CreateMaterial("MAT_Street_OldWall", "sl2qedtp_2K_Albedo.jpg", "sl2qedtp_2K_Normal.jpg", new Color(0.74f, 0.66f, 0.56f), 0.58f);
            var wood = CreateMaterial("MAT_Street_Wood", "Wood047_PNG_Color.png", "Wood047_PNG_NormalDX.png", new Color(0.48f, 0.30f, 0.16f), 0.55f);
            var metal = CreateMaterial("MAT_Street_CorrugatedMetal", "ton.png", null, new Color(0.58f, 0.58f, 0.55f), 0.32f);
            var shopA = CreateMaterial("MAT_Street_ShopFacade_A", "487078.png", null, new Color(0.75f, 0.54f, 0.36f), 0.48f);
            var shopB = CreateMaterial("MAT_Street_ShopFacade_B", "598528.png", null, new Color(0.46f, 0.64f, 0.70f), 0.48f);
            var shopC = CreateMaterial("MAT_Street_ShopFacade_C", "89456565.png", null, new Color(0.70f, 0.58f, 0.42f), 0.48f);
            var flag = CreateMaterial("MAT_Street_FlagSign", "la-co-viet-nam-vector-1.png", null, Color.white, 0.35f);
            var roof = CreateMaterial("MAT_Street_RoofAndAwning", "images.jpg", null, new Color(0.56f, 0.20f, 0.12f), 0.45f);
            var palette = new[] { wall, shopA, shopB, shopC, wood, metal, roof };

            foreach (var renderer in importedRoot.GetComponentsInChildren<Renderer>(true))
            {
                var sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                {
                    renderer.sharedMaterial = PickMaterial(renderer.name, renderer.gameObject.name, string.Empty, palette, paving, wall, wood, metal, flag, roof);
                    continue;
                }

                var assignedMaterials = new Material[sourceMaterials.Length];
                for (var i = 0; i < assignedMaterials.Length; i++)
                {
                    var sourceMaterialName = sourceMaterials[i] != null ? sourceMaterials[i].name : string.Empty;
                    assignedMaterials[i] = PickMaterial(renderer.name, renderer.gameObject.name, sourceMaterialName, palette, paving, wall, wood, metal, flag, roof);
                }

                renderer.sharedMaterials = assignedMaterials;
            }
        }

        private static void ImportTextureSettings()
        {
            AssetDatabase.ImportAsset(TextureFolder, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            foreach (var normalName in new[] { "PavingStones078_2K_NormalGL.png", "sl2qedtp_2K_Normal.jpg", "Wood047_2K-PNG_NormalGL.png", "Wood047_PNG_NormalDX.png" })
            {
                var path = $"{TextureFolder}/{normalName}";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType == TextureImporterType.NormalMap)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        private static Material CreateMaterial(string name, string albedoName, string normalName, Color fallbackColor, float smoothness)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = fallbackColor;
            material.SetFloat("_Glossiness", smoothness);

            var albedo = LoadTexture(albedoName);
            if (albedo != null)
            {
                material.mainTexture = albedo;
            }

            var normal = string.IsNullOrEmpty(normalName) ? null : LoadTexture(normalName);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetTexture("_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{fileName}");
        }

        private static Material PickMaterial(string rendererName, string objectName, string sourceMaterialName, Material[] palette, Material paving, Material wall, Material wood, Material metal, Material flag, Material roof)
        {
            var name = $"{rendererName} {objectName} {sourceMaterialName}".ToLowerInvariant();
            if (name.Contains("road") || name.Contains("street") || name.Contains("pav") || name.Contains("floor") || name.Contains("ground") || name.Contains("sidewalk"))
            {
                return paving;
            }

            if (name.Contains("wood") || name.Contains("door") || name.Contains("frame"))
            {
                return wood;
            }

            if (name.Contains("metal") || name.Contains("gate") || name.Contains("rail") || name.Contains("fence") || name.Contains("ton"))
            {
                return metal;
            }

            if (name.Contains("roof") || name.Contains("tile") || name.Contains("awning"))
            {
                return roof;
            }

            if (name.Contains("flag") || name.Contains("sign") || name.Contains("banner"))
            {
                return flag;
            }

            if (name.Contains("wall") || name.Contains("plaster") || name.Contains("building"))
            {
                return wall;
            }

            var index = Mathf.Abs(name.GetHashCode()) % palette.Length;
            return palette[index];
        }

        private static void EnsureMeshColliders(GameObject importedRoot)
        {
            foreach (var meshFilter in importedRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null || meshFilter.GetComponent<MeshCollider>() != null)
                {
                    continue;
                }

                var collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private static void AdjustDarkStreetMaterials(GameObject importedRoot)
        {
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");
            EnsureFolder(AdjustedMaterialFolder);

            foreach (var renderer in importedRoot.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !ShouldLiftDarkMaterial(renderer, material))
                    {
                        continue;
                    }

                    materials[i] = CreateLiftedMaterialCopy(renderer, material);
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static bool ShouldLiftDarkMaterial(Renderer renderer, Material material)
        {
            var name = $"{renderer.name} {renderer.gameObject.name} {material.name}".ToLowerInvariant();
            var isDoorOrGate = name.Contains("door") || name.Contains("gate") || name.Contains("shutter") || name.Contains("roll") || name.Contains("grill") || name.Contains("metal");
            var shouldStayDark = name.Contains("wire") || name.Contains("cable") || name.Contains("daydien") || name.Contains("lamp") || name.Contains("dengia") || name.Contains("den") || name.Contains("flag") || name.Contains("sign");
            var color = material.HasProperty("_Color") ? material.color : Color.black;
            var luminance = ColorLuminance(color);
            return !shouldStayDark && ((isDoorOrGate && luminance < 0.34f) || luminance < 0.08f);
        }

        private static Material CreateLiftedMaterialCopy(Renderer renderer, Material source)
        {
            var safeRendererName = MakeSafeAssetName(renderer.gameObject.name);
            var safeMaterialName = MakeSafeAssetName(source.name);
            var path = $"{AdjustedMaterialFolder}/MAT_Lifted_{safeRendererName}_{safeMaterialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.CopyPropertiesFromMaterial(source);
            }

            var sourceName = $"{renderer.name} {renderer.gameObject.name} {source.name}".ToLowerInvariant();
            var color = source.HasProperty("_Color") ? source.color : Color.black;
            var lifted = PickLiftedDarkColor(sourceName, color);
            if (material.HasProperty("_Color"))
            {
                material.color = lifted;
            }
            material.SetFloat("_Glossiness", Mathf.Min(0.28f, material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.24f));

            if (material.HasProperty("_EmissionColor"))
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color PickLiftedDarkColor(string sourceName, Color original)
        {
            if (sourceName.Contains("kinh") || sourceName.Contains("glass"))
            {
                return new Color(0.20f, 0.27f, 0.29f, original.a);
            }

            if (sourceName.Contains("door") || sourceName.Contains("gate") || sourceName.Contains("shutter") || sourceName.Contains("roll") || sourceName.Contains("grill"))
            {
                return new Color(0.11f, 0.15f, 0.13f, original.a);
            }

            return new Color(
                Mathf.Clamp(original.r + 0.10f, 0.10f, 0.18f),
                Mathf.Clamp(original.g + 0.11f, 0.11f, 0.19f),
                Mathf.Clamp(original.b + 0.10f, 0.10f, 0.17f),
                original.a);
        }

        private static float ColorLuminance(Color color)
        {
            return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
        }

        private static string MakeSafeAssetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Material";
            }

            foreach (var character in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(character, '_');
            }

            return value.Replace(' ', '_');
        }

        private static void PlacePlayerOnImportedStreet(GameObject importedRoot)
        {
            var bounds = CalculateBounds(importedRoot);
            var player = GameObject.Find(PlayerName);
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = PlayerName;
            }

            if (!player.TryGetComponent<CharacterController>(out var characterController))
            {
                characterController = player.AddComponent<CharacterController>();
            }

            if (player.TryGetComponent<CapsuleCollider>(out var capsuleCollider))
            {
                Object.DestroyImmediate(capsuleCollider);
            }

            if (player.TryGetComponent<MeshRenderer>(out var rootRenderer))
            {
                Object.DestroyImmediate(rootRenderer);
            }

            if (player.TryGetComponent<MeshFilter>(out var rootMeshFilter))
            {
                Object.DestroyImmediate(rootMeshFilter);
            }

            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 50f;

            player.transform.position = FindStreetSpawn(bounds);
            player.transform.rotation = Quaternion.identity;

            var camera = EnsurePlayerCamera(player);
            EnsureFirstPersonModel(player, camera);
            var controller = player.GetComponent<SimpleFirstPersonController>();
            if (controller == null)
            {
                controller = player.AddComponent<SimpleFirstPersonController>();
            }

            controller.playerCamera = camera;
            DisableOtherCameras(camera);
        }

        private static Vector3 FindStreetSpawn(Bounds bounds)
        {
            Physics.SyncTransforms();

            var zCandidates = new[]
            {
                bounds.min.z + bounds.size.z * 0.14f,
                bounds.min.z + bounds.size.z * 0.24f,
                bounds.center.z,
                bounds.min.z + bounds.size.z * 0.36f
            };

            var xCandidates = new[]
            {
                bounds.center.x,
                bounds.center.x - bounds.size.x * 0.08f,
                bounds.center.x + bounds.size.x * 0.08f,
                bounds.center.x - bounds.size.x * 0.16f,
                bounds.center.x + bounds.size.x * 0.16f
            };

            foreach (var z in zCandidates)
            {
                foreach (var x in xCandidates)
                {
                    var origin = new Vector3(x, bounds.max.y + 15f, z);
                    var hits = Physics.RaycastAll(origin, Vector3.down, bounds.size.y + 40f);
                    var bestY = float.PositiveInfinity;
                    var found = false;

                    foreach (var hit in hits)
                    {
                        if (hit.collider.GetComponentInParent<CharacterController>() != null || hit.normal.y < 0.45f)
                        {
                            continue;
                        }

                        if (hit.point.y < bestY)
                        {
                            bestY = hit.point.y;
                            found = true;
                        }
                    }

                    if (found)
                    {
                        return new Vector3(x, bestY + 0.06f, z);
                    }
                }
            }

            return new Vector3(bounds.center.x, bounds.min.y + 1.15f, bounds.min.z + Mathf.Min(10f, bounds.size.z * 0.18f));
        }

        private static Camera EnsurePlayerCamera(GameObject player)
        {
            var cameraTransform = player.transform.Find("First Person Camera");
            if (cameraTransform == null)
            {
                var cameraObject = new GameObject("First Person Camera");
                cameraObject.transform.SetParent(player.transform);
                cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0.08f);
                cameraObject.transform.localRotation = Quaternion.identity;
                cameraTransform = cameraObject.transform;
            }

            var camera = cameraTransform.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraTransform.gameObject.AddComponent<Camera>();
            }

            camera.tag = "MainCamera";
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            return camera;
        }

        private static void EnsureFirstPersonModel(GameObject player, Camera camera)
        {
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");
            EnsureFolder(PlayerMaterialFolder);

            var cloth = CreateFlatMaterial($"{PlayerMaterialFolder}/MAT_Player_EMS_Cloth.mat", new Color(1.0f, 0.43f, 0.03f));
            var skin = CreateFlatMaterial($"{PlayerMaterialFolder}/MAT_Player_Skin.mat", new Color(0.74f, 0.50f, 0.34f));
            var dark = CreateFlatMaterial($"{PlayerMaterialFolder}/MAT_Player_DarkPants.mat", new Color(0.08f, 0.10f, 0.13f));
            var pack = CreateFlatMaterial($"{PlayerMaterialFolder}/MAT_Player_CargoPack.mat", new Color(0.28f, 0.18f, 0.10f));

            var worldModel = GetOrCreateChild(player.transform, "World Character Model");
            RemoveGeneratedChildren(worldModel.transform);
            CreatePrimitiveChild(worldModel.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.72f, 0.95f, 0.72f), cloth);
            CreatePrimitiveChild(worldModel.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0f), new Vector3(0.38f, 0.38f, 0.38f), skin);
            CreatePrimitiveChild(worldModel.transform, "Delivery Backpack", PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.34f), new Vector3(0.62f, 0.72f, 0.22f), pack);
            CreatePrimitiveChild(worldModel.transform, "Left Leg", PrimitiveType.Cube, new Vector3(-0.16f, 0.35f, 0f), new Vector3(0.18f, 0.70f, 0.22f), dark);
            CreatePrimitiveChild(worldModel.transform, "Right Leg", PrimitiveType.Cube, new Vector3(0.16f, 0.35f, 0f), new Vector3(0.18f, 0.70f, 0.22f), dark);

            var armsRoot = GetOrCreateChild(camera.transform, "First Person Arms");
            armsRoot.transform.localPosition = Vector3.zero;
            armsRoot.transform.localRotation = Quaternion.identity;
            RemoveGeneratedChildren(armsRoot.transform);
            var leftArm = CreatePrimitiveChild(armsRoot.transform, "Left Arm", PrimitiveType.Cube, new Vector3(-0.34f, -0.33f, 0.55f), new Vector3(0.16f, 0.16f, 0.65f), cloth);
            leftArm.transform.localRotation = Quaternion.Euler(10f, -12f, 0f);
            var rightArm = CreatePrimitiveChild(armsRoot.transform, "Right Arm", PrimitiveType.Cube, new Vector3(0.34f, -0.33f, 0.55f), new Vector3(0.16f, 0.16f, 0.65f), cloth);
            rightArm.transform.localRotation = Quaternion.Euler(10f, 12f, 0f);
            CreatePrimitiveChild(armsRoot.transform, "Left Hand", PrimitiveType.Sphere, new Vector3(-0.38f, -0.33f, 0.91f), new Vector3(0.16f, 0.13f, 0.16f), skin);
            CreatePrimitiveChild(armsRoot.transform, "Right Hand", PrimitiveType.Sphere, new Vector3(0.38f, -0.33f, 0.91f), new Vector3(0.16f, 0.13f, 0.16f), skin);
        }

        private static Material CreateFlatMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Glossiness", 0.18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject GetOrCreateChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(parent);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static GameObject CreatePrimitiveChild(Transform parent, string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var child = GameObject.CreatePrimitive(primitiveType);
            child.name = objectName;
            child.transform.SetParent(parent);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;

            if (child.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
            }

            if (child.TryGetComponent<Collider>(out var collider))
            {
                Object.DestroyImmediate(collider);
            }

            return child;
        }

        private static void RemoveGeneratedChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void DisableOtherCameras(Camera activeCamera)
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera == activeCamera)
                {
                    camera.enabled = true;
                    camera.tag = "MainCamera";
                }
                else
                {
                    camera.enabled = false;
                    camera.tag = "Untagged";
                }
            }
        }

        private static void EnsureBalancedLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.62f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.45f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.29f, 0.25f);
            RenderSettings.ambientIntensity = 0.72f;
            RenderSettings.reflectionIntensity = 0.45f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.68f, 0.70f, 0.68f);
            RenderSettings.fogDensity = 0.0032f;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.gameObject.name != "Prototype Sun - Soft Afternoon")
                {
                    light.enabled = false;
                }
            }

            var sunObject = GameObject.Find("Prototype Sun - Soft Afternoon");
            if (sunObject == null)
            {
                sunObject = new GameObject("Prototype Sun - Soft Afternoon");
            }

            var sun = sunObject.GetComponent<Light>();
            if (sun == null)
            {
                sun = sunObject.AddComponent<Light>();
            }

            sun.enabled = true;
            sun.type = LightType.Directional;
            sun.intensity = 0.88f;
            sun.color = new Color(1.0f, 0.90f, 0.76f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.38f;
            sunObject.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, new Vector3(8f, 2f, 20f));
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
