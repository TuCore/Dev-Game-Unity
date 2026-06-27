using DevGameUnity.Delivery;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevGameUnity.EditorTools
{
    [InitializeOnLoad]
    public static class DeliveryGameplayPrototypeInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/DeliveryStreetPrototype.unity";
        private const string RootName = "Delivery Gameplay Prototype";
        private const string AutoSetupKey = "DevGameUnity.DeliveryGameplayPrototypeInstaller.V1";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/GameplayPrototype";

        static DeliveryGameplayPrototypeInstaller()
        {
            EditorApplication.delayCall += AutoInstall;
        }

        [DidReloadScripts]
        private static void AfterScriptsReload()
        {
            SessionState.SetBool(AutoSetupKey, false);
            EditorApplication.delayCall += AutoInstall;
        }

        [MenuItem("Dev Game/Install Delivery Gameplay Prototype")]
        public static void InstallFromMenu()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Install(scene);
        }

        private static void AutoInstall()
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
            Install(scene);
        }

        private static void Install(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }

            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");
            EnsureFolder(MaterialFolder);

            var root = new GameObject(RootName);
            var bounds = CalculateStreetBounds();
            var depotPosition = FindGroundedPoint(bounds.center.x, bounds.min.z + bounds.size.z * 0.12f, bounds);

            var depot = CreateDepot(root.transform, depotPosition);
            CreateDeliveryPoints(root.transform, bounds);
            var manager = CreateManager(root.transform);
            manager.playerCamera = Camera.main;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Delivery gameplay prototype installed: depot, package boxes, delivery points, and UI manager.");
        }

        private static DeliveryPrototypeManager CreateManager(Transform parent)
        {
            var managerObject = new GameObject("Delivery Prototype Manager");
            managerObject.transform.SetParent(parent);
            return managerObject.AddComponent<DeliveryPrototypeManager>();
        }

        private static DeliveryDepot CreateDepot(Transform parent, Vector3 position)
        {
            var depotRoot = new GameObject("EMS Depot Pickup Zone");
            depotRoot.transform.SetParent(parent);
            depotRoot.transform.position = position;

            var depot = depotRoot.AddComponent<DeliveryDepot>();
            depot.packageCapacity = 5;

            var trigger = depotRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.2f, 2.2f, 3.2f);
            trigger.center = new Vector3(0f, 1.1f, 0f);

            var markerMat = CreateMaterial("MAT_Depot_EMS_Orange", new Color(1.0f, 0.48f, 0.02f));
            var boxMat = CreateMaterial("MAT_Package_Cardboard", new Color(0.62f, 0.42f, 0.22f));
            CreatePrimitive(depotRoot.transform, "EMS Pickup Marker", PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(1.8f, 0.08f, 1.8f), markerMat);
            CreatePrimitive(depotRoot.transform, "EMS Sign", PrimitiveType.Cube, new Vector3(0f, 1.8f, 1.15f), new Vector3(2.4f, 0.58f, 0.12f), markerMat);

            for (var i = 0; i < 5; i++)
            {
                var local = new Vector3(-0.9f + i * 0.45f, 0.28f + (i % 2) * 0.18f, -0.45f + (i % 3) * 0.32f);
                CreatePrimitive(depotRoot.transform, $"Package Stack {i + 1}", PrimitiveType.Cube, local, new Vector3(0.36f, 0.32f, 0.36f), boxMat);
            }

            return depot;
        }

        private static void CreateDeliveryPoints(Transform parent, Bounds bounds)
        {
            var pointMat = CreateMaterial("MAT_DeliveryPoint_Active", new Color(1.0f, 0.48f, 0.02f));
            var labelMat = CreateMaterial("MAT_DeliveryPoint_Label", new Color(0.96f, 0.92f, 0.74f));
            var zStart = bounds.min.z + bounds.size.z * 0.24f;
            var spacing = bounds.size.z * 0.11f;
            var xOffset = Mathf.Clamp(bounds.size.x * 0.28f, 3.0f, 8.0f);

            for (var i = 0; i < 5; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var x = bounds.center.x + side * xOffset;
                var z = zStart + spacing * i;
                var position = FindGroundedPoint(x, z, bounds);
                var pointRoot = new GameObject($"Delivery Point A-{i + 1:000}");
                pointRoot.transform.SetParent(parent);
                pointRoot.transform.position = position;

                var point = pointRoot.AddComponent<DeliveryPoint>();
                point.addressId = $"A-{i + 1:000}";
                point.customerName = $"Khach {i + 1}";
                point.reward = 35000 + i * 5000;

                var trigger = pointRoot.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 1.0f;
                trigger.center = new Vector3(0f, 1.0f, 0f);

                CreatePrimitive(pointRoot.transform, "Delivery Door Marker", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.06f, 0.9f), pointMat);
                CreatePrimitive(pointRoot.transform, "Door Bell", PrimitiveType.Sphere, new Vector3(0f, 1.25f, 0f), new Vector3(0.22f, 0.22f, 0.22f), pointMat);
                CreatePrimitive(pointRoot.transform, "Address Plate", PrimitiveType.Cube, new Vector3(0f, 1.65f, 0f), new Vector3(0.9f, 0.32f, 0.08f), labelMat);
            }
        }

        private static GameObject CreatePrimitive(Transform parent, string objectName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var gameObject = GameObject.CreatePrimitive(primitive);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = localScale;
            if (gameObject.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
            }
            return gameObject;
        }

        private static Vector3 FindGroundedPoint(float x, float z, Bounds bounds)
        {
            Physics.SyncTransforms();
            var origin = new Vector3(x, bounds.max.y + 20f, z);
            var hits = Physics.RaycastAll(origin, Vector3.down, bounds.size.y + 50f);
            var best = new Vector3(x, bounds.min.y + 0.05f, z);
            var bestY = float.PositiveInfinity;

            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<CharacterController>() != null || hit.normal.y < 0.35f)
                {
                    continue;
                }

                if (hit.point.y < bestY)
                {
                    bestY = hit.point.y;
                    best = hit.point + Vector3.up * 0.05f;
                }
            }

            return best;
        }

        private static Bounds CalculateStreetBounds()
        {
            var street = GameObject.Find("Imported Vietnam Old Town FBX") ?? GameObject.Find("VietnamStreet_FBX");
            if (street == null)
            {
                return new Bounds(Vector3.zero, new Vector3(12f, 4f, 60f));
            }

            var renderers = street.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(street.transform.position, new Vector3(12f, 4f, 60f));
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/{name}.mat";
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
