using DevGameUnity.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevGameUnity.EditorTools
{
    [InitializeOnLoad]
    public static class FirstPersonTestPlayerInstaller
    {
        private const string ScenePath = "Assets/_Project/Scenes/DeliveryStreetPrototype.unity";
        private const string PlayerName = "First Person Test Player";
        private const string AutoInstallKey = "DevGameUnity.FirstPersonTestPlayer.AutoInstalled";

        static FirstPersonTestPlayerInstaller()
        {
            EditorApplication.delayCall += AutoInstall;
        }

        [MenuItem("Dev Game/Add First Person Test Player")]
        public static void AddPlayerToOpenScene()
        {
            AddOrUpdatePlayer(EditorSceneManager.GetActiveScene(), saveScene: true);
        }

        private static void AutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AutoInstallKey, false))
            {
                return;
            }

            if (!System.IO.File.Exists(ScenePath))
            {
                return;
            }

            SessionState.SetBool(AutoInstallKey, true);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AddOrUpdatePlayer(scene, saveScene: true);
        }

        private static void AddOrUpdatePlayer(Scene scene, bool saveScene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            var player = GameObject.Find(PlayerName);
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = PlayerName;
                player.transform.position = new Vector3(0f, 1.1f, -78f);
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            if (!player.TryGetComponent<CharacterController>(out var characterController))
            {
                characterController = player.AddComponent<CharacterController>();
            }

            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 50f;

            if (player.TryGetComponent<CapsuleCollider>(out var primitiveCollider))
            {
                Object.DestroyImmediate(primitiveCollider);
            }

            var cameraTransform = player.transform.Find("First Person Camera");
            Camera camera;
            if (cameraTransform == null)
            {
                var cameraObject = new GameObject("First Person Camera");
                cameraObject.transform.SetParent(player.transform);
                cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0.08f);
                cameraObject.transform.localRotation = Quaternion.identity;
                camera = cameraObject.AddComponent<Camera>();
            }
            else
            {
                camera = cameraTransform.GetComponent<Camera>();
                if (camera == null)
                {
                    camera = cameraTransform.gameObject.AddComponent<Camera>();
                }
            }

            camera.tag = "MainCamera";
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;

            var controller = player.GetComponent<SimpleFirstPersonController>();
            if (controller == null)
            {
                controller = player.AddComponent<SimpleFirstPersonController>();
            }

            controller.playerCamera = camera;

            DisableOverviewCameras(camera);
            EnsureSpawnLight(player.transform.position);

            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void DisableOverviewCameras(Camera activeCamera)
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera != activeCamera)
                {
                    camera.enabled = false;
                    camera.tag = "Untagged";
                }
            }
        }

        private static void EnsureSpawnLight(Vector3 playerPosition)
        {
            if (GameObject.Find("Player Spawn Helper Light") != null)
            {
                return;
            }

            var lightObject = new GameObject("Player Spawn Helper Light");
            lightObject.transform.position = playerPosition + new Vector3(0f, 3.5f, -2f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.82f, 0.55f);
        }
    }
}
