using System.IO;
using DevGameUnity.Delivery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevGameUnity.EditorTools
{
    [InitializeOnLoad]
    public static class DeliveryStreetSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/DeliveryStreetPrototype.unity";
        private const string FbxPath = "Assets/_Project/Art/Models/VietnamOldTown/VietnamStreet.fbx";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/StreetPrototype";
        private const string TextureFolder = "Assets/_Project/Art/Textures/StreetPrototype";
        private const string AutoBuildKey = "DevGameUnity.DeliveryStreetPrototype.AutoBuilt";

        static DeliveryStreetSceneBuilder()
        {
            // Kept as a manual fallback only. The project now uses the imported
            // VietnamStreet FBX as the actual playable street.
        }

        [MenuItem("Dev Game/Build Delivery Street Prototype")]
        public static void BuildMenu()
        {
            BuildScene(forceRebuild: true);
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (SessionState.GetBool(AutoBuildKey, false) || File.Exists(ScenePath))
            {
                return;
            }

            SessionState.SetBool(AutoBuildKey, true);
            BuildScene(forceRebuild: false);
        }

        private static void BuildScene(bool forceRebuild)
        {
            EnsureFolder("Assets/_Project/Scenes");
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Project/Art/Textures");
            EnsureFolder(TextureFolder);

            if (forceRebuild && File.Exists(ScenePath))
            {
                AssetDatabase.DeleteAsset(ScenePath);
            }

            var mats = CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DeliveryStreetPrototype";

            RenderSettings.skybox = null;
            RenderSettings.ambientLight = new Color(0.64f, 0.68f, 0.72f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.70f, 0.74f, 0.76f);
            RenderSettings.fogDensity = 0.006f;

            var root = new GameObject("Delivery Street Prototype");
            var importedRoot = new GameObject("Imported Vietnam Old Town FBX");
            importedRoot.transform.SetParent(root.transform);
            TryPlaceImportedStreet(importedRoot.transform);

            var streetRoot = new GameObject("Playable Straight Delivery Street");
            streetRoot.transform.SetParent(root.transform);

            CreateCube("Asphalt Road - playable delivery lane", new Vector3(0f, -0.04f, 0f), new Vector3(10f, 0.08f, 170f), mats.asphalt, streetRoot.transform);
            CreateCube("Left Sidewalk", new Vector3(-7f, 0.05f, 0f), new Vector3(3.6f, 0.18f, 170f), mats.sidewalk, streetRoot.transform);
            CreateCube("Right Sidewalk", new Vector3(7f, 0.05f, 0f), new Vector3(3.6f, 0.18f, 170f), mats.sidewalk, streetRoot.transform);
            CreateLaneMarks(streetRoot.transform, mats.roadLine);

            CreateDistrictSide(streetRoot.transform, mats, leftSide: true);
            CreateDistrictSide(streetRoot.transform, mats, leftSide: false);
            CreateGameplayLandmarks(streetRoot.transform, mats);
            CreateStreetProps(streetRoot.transform, mats);
            CreateLightingAndCamera();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Delivery street scene built: {ScenePath}");
        }

        private static void TryPlaceImportedStreet(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (prefab == null)
            {
                var warning = new GameObject("Missing VietnamStreet.fbx - copy it to Assets/_Project/Art/Models/VietnamOldTown");
                warning.transform.SetParent(parent);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "VietnamStreet_FBX";
            instance.transform.SetParent(parent);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        private static MaterialSet CreateMaterials()
        {
            var asphaltTex = CreateTexture("TX_AsphaltNoise.png", new Color(0.10f, 0.10f, 0.10f), new Color(0.20f, 0.20f, 0.19f), 19);
            var plasterTex = CreateTexture("TX_OldPlaster.png", new Color(0.78f, 0.68f, 0.54f), new Color(0.93f, 0.87f, 0.72f), 11);
            var sidewalkTex = CreateTexture("TX_ConcreteTiles.png", new Color(0.43f, 0.43f, 0.40f), new Color(0.62f, 0.61f, 0.56f), 8);
            var roofTex = CreateTexture("TX_RoofTiles.png", new Color(0.48f, 0.12f, 0.08f), new Color(0.70f, 0.22f, 0.12f), 6);
            var metalTex = CreateTexture("TX_PaintedMetal.png", new Color(0.08f, 0.16f, 0.22f), new Color(0.20f, 0.32f, 0.36f), 17);

            return new MaterialSet
            {
                asphalt = CreateMaterial("MAT_Asphalt_Rough", new Color(0.12f, 0.12f, 0.12f), asphaltTex),
                sidewalk = CreateMaterial("MAT_Sidewalk_Tiled", new Color(0.58f, 0.56f, 0.50f), sidewalkTex),
                roadLine = CreateMaterial("MAT_RoadLine_FadedYellow", new Color(1.0f, 0.76f, 0.22f), null),
                plasterA = CreateMaterial("MAT_OldTown_Plaster_Warm", new Color(0.88f, 0.72f, 0.48f), plasterTex),
                plasterB = CreateMaterial("MAT_OldTown_Plaster_Mint", new Color(0.56f, 0.78f, 0.68f), plasterTex),
                plasterC = CreateMaterial("MAT_OldTown_Plaster_Blue", new Color(0.45f, 0.64f, 0.82f), plasterTex),
                plasterD = CreateMaterial("MAT_OldTown_Plaster_Rose", new Color(0.78f, 0.48f, 0.48f), plasterTex),
                roof = CreateMaterial("MAT_RedClay_RoofTiles", new Color(0.62f, 0.18f, 0.10f), roofTex),
                door = CreateMaterial("MAT_DarkWood_Doors", new Color(0.26f, 0.13f, 0.06f), null),
                window = CreateMaterial("MAT_Glass_BlueGreen", new Color(0.18f, 0.48f, 0.58f), null),
                metal = CreateMaterial("MAT_RollingDoor_Metal", new Color(0.16f, 0.24f, 0.28f), metalTex),
                ems = CreateMaterial("MAT_EMS_Orange", new Color(1.0f, 0.48f, 0.02f), null),
                convenience = CreateMaterial("MAT_Convenience_Green", new Color(0.05f, 0.58f, 0.28f), null),
                accentRed = CreateMaterial("MAT_Sign_Red", new Color(0.82f, 0.08f, 0.08f), null),
                cardboard = CreateMaterial("MAT_Cardboard_Boxes", new Color(0.61f, 0.42f, 0.22f), CreateTexture("TX_Cardboard.png", new Color(0.50f, 0.32f, 0.16f), new Color(0.75f, 0.55f, 0.30f), 13))
            };
        }

        private static Texture2D CreateTexture(string fileName, Color a, Color b, int stride)
        {
            var path = $"{TextureFolder}/{fileName}";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (var y = 0; y < 128; y++)
                {
                    for (var x = 0; x < 128; x++)
                    {
                        var checker = ((x / stride) + (y / stride)) % 2 == 0;
                        var noise = Mathf.PerlinNoise(x * 0.13f, y * 0.13f) * 0.18f;
                        var color = Color.Lerp(checker ? a : b, b, noise);
                        texture.SetPixel(x, y, color);
                    }
                }

                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateMaterial(string name, Color color, Texture2D texture)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.mainTexture = texture;
            material.SetFloat("_Glossiness", 0.18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateDistrictSide(Transform parent, MaterialSet mats, bool leftSide)
        {
            var side = leftSide ? -1f : 1f;
            var x = side * 13.2f;
            var startZ = -72f;
            var colors = new[] { mats.plasterA, mats.plasterB, mats.plasterC, mats.plasterD };

            for (var i = 0; i < 9; i++)
            {
                var z = startZ + i * 18f;
                var width = 8f + (i % 3) * 1.4f;
                var height = 5f + (i % 4) * 1.2f;
                var depth = 5f + (i % 2) * 1.4f;
                var building = new GameObject($"{(leftSide ? "Left" : "Right")} House {i + 1:00}");
                building.transform.SetParent(parent);
                var bodyPosition = new Vector3(x, height * 0.5f, z);

                CreateCube("Body", bodyPosition, new Vector3(width, height, depth), colors[i % colors.Length], building.transform);
                CreateCube("Clay roof", bodyPosition + new Vector3(0f, height * 0.55f, 0f), new Vector3(width + 0.8f, 0.45f, depth + 0.8f), mats.roof, building.transform);
                CreateDoorAndWindows(building.transform, mats, side, bodyPosition, width, height, depth);

                var marker = CreateDeliveryMarker(building.transform, mats, side, i, bodyPosition, width);
                marker.displayName = $"{(leftSide ? "L" : "R")}-{i + 1:00} Old Town Door";
                marker.floor = 1 + (i % 3);
            }
        }

        private static void CreateDoorAndWindows(Transform building, MaterialSet mats, float side, Vector3 bodyPosition, float width, float height, float depth)
        {
            var frontX = bodyPosition.x - side * (width * 0.5f + 0.04f);
            var door = CreateCube("Delivery door with bell", new Vector3(frontX, 1.05f, bodyPosition.z), new Vector3(0.12f, 2.1f, 1.25f), mats.door, building);
            door.transform.localRotation = Quaternion.identity;

            for (var floor = 0; floor < Mathf.Max(1, Mathf.RoundToInt(height / 2.2f) - 1); floor++)
            {
                var y = 2.5f + floor * 1.7f;
                CreateCube("Window A", new Vector3(frontX, y, bodyPosition.z - depth * 0.22f), new Vector3(0.14f, 0.8f, 0.9f), mats.window, building);
                CreateCube("Window B", new Vector3(frontX, y, bodyPosition.z + depth * 0.22f), new Vector3(0.14f, 0.8f, 0.9f), mats.window, building);
            }
        }

        private static DeliveryAddressMarker CreateDeliveryMarker(Transform building, MaterialSet mats, float side, int index, Vector3 bodyPosition, float width)
        {
            var markerX = bodyPosition.x - side * (width * 0.5f + 0.42f);
            var markerObject = CreateCube("Delivery interaction marker", new Vector3(markerX, 1.25f, bodyPosition.z), new Vector3(0.18f, 0.18f, 0.18f), mats.ems, building);
            var marker = markerObject.AddComponent<DeliveryAddressMarker>();
            marker.addressId = $"{(side < 0 ? "L" : "R")}-{index + 1:000}";
            marker.canBeBombed = index % 3 == 1;

            var drop = new GameObject("Package drop point");
            drop.transform.SetParent(building);
            drop.transform.position = new Vector3(markerX, 0.2f, bodyPosition.z + 0.9f);
            marker.dropPoint = drop.transform;

            return marker;
        }

        private static void CreateGameplayLandmarks(Transform parent, MaterialSet mats)
        {
            CreateLabeledBuilding("EMS Depot - morning pickup", new Vector3(0f, 3f, -92f), new Vector3(17f, 6f, 9f), mats.ems, mats.metal, parent);
            CreateLabeledBuilding("Cheap Motel Room Start", new Vector3(-18f, 2.7f, -88f), new Vector3(7f, 5.4f, 6f), mats.plasterD, mats.door, parent);
            CreateLabeledBuilding("GS25 Style Convenience Store", new Vector3(18f, 2.6f, -46f), new Vector3(8f, 5.2f, 6f), mats.convenience, mats.window, parent);
            CreateLabeledBuilding("Motorbike Repair Shop", new Vector3(-18f, 2.6f, 32f), new Vector3(8f, 5.2f, 6f), mats.metal, mats.accentRed, parent);
            CreateLabeledBuilding("Street Food Stall - Hu Tieu", new Vector3(18f, 1.7f, 40f), new Vector3(6f, 3.4f, 4f), mats.plasterA, mats.roof, parent);

            for (var i = 0; i < 7; i++)
            {
                CreateCube($"EMS parcel stack {i + 1}", new Vector3(-4f + i * 1.2f, 0.45f, -86f + (i % 2) * 1.1f), new Vector3(0.9f, 0.9f, 0.9f), mats.cardboard, parent);
            }
        }

        private static void CreateLabeledBuilding(string name, Vector3 position, Vector3 scale, Material bodyMat, Material signMat, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent);
            CreateCube("Body", position, scale, bodyMat, group.transform);
            CreateCube("Large readable gameplay sign", position + new Vector3(0f, scale.y * 0.18f, -scale.z * 0.52f), new Vector3(scale.x * 0.75f, 0.8f, 0.16f), signMat, group.transform);
        }

        private static void CreateStreetProps(Transform parent, MaterialSet mats)
        {
            for (var i = 0; i < 14; i++)
            {
                var z = -74f + i * 11f;
                CreateCube($"Street lamp pole {i + 1}", new Vector3(-5.3f, 1.8f, z), new Vector3(0.14f, 3.6f, 0.14f), mats.metal, parent);
                CreateCube($"Street lamp head {i + 1}", new Vector3(-4.9f, 3.65f, z), new Vector3(0.8f, 0.18f, 0.28f), mats.roadLine, parent);
                CreateCube($"Plastic trash bin {i + 1}", new Vector3(5.4f, 0.45f, z + 3.5f), new Vector3(0.7f, 0.9f, 0.7f), i % 2 == 0 ? mats.convenience : mats.accentRed, parent);
            }

            CreateCube("Parked old motorbike placeholder", new Vector3(-3.0f, 0.55f, -84f), new Vector3(1.0f, 0.7f, 2.0f), mats.accentRed, parent);
            CreateCube("Delivery cargo rack placeholder", new Vector3(-3.0f, 1.15f, -83.1f), new Vector3(1.1f, 0.75f, 0.9f), mats.cardboard, parent);
        }

        private static void CreateLaneMarks(Transform parent, Material lineMaterial)
        {
            for (var i = 0; i < 18; i++)
            {
                CreateCube($"Faded center lane mark {i + 1}", new Vector3(0f, 0.025f, -80f + i * 9.5f), new Vector3(0.22f, 0.035f, 4.8f), lineMaterial, parent);
            }
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            if (cube.TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
            }
            return cube;
        }

        private static void CreateLightingAndCamera()
        {
            var sun = new GameObject("Late afternoon sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1.0f, 0.86f, 0.68f);
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var cameraObject = new GameObject("Prototype Camera - street overview");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 18f, -42f);
            cameraObject.transform.rotation = Quaternion.Euler(64f, 0f, 0f);
            camera.fieldOfView = 48f;
            Camera.main?.gameObject.SetActive(false);
            cameraObject.tag = "MainCamera";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private sealed class MaterialSet
        {
            public Material asphalt;
            public Material sidewalk;
            public Material roadLine;
            public Material plasterA;
            public Material plasterB;
            public Material plasterC;
            public Material plasterD;
            public Material roof;
            public Material door;
            public Material window;
            public Material metal;
            public Material ems;
            public Material convenience;
            public Material accentRed;
            public Material cardboard;
        }
    }
}
