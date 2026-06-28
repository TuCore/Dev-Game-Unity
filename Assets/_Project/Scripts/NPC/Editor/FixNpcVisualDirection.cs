using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevGameUnity.EditorTools
{
    [InitializeOnLoad]
    internal static class FixNpcVisualDirection
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/TripoStreetNpc.prefab";
        private const string SessionKey = "DevGameUnity.FixNpcVisualDirection.V1";

        static FixNpcVisualDirection()
        {
            EditorApplication.delayCall += Fix;
        }

        private static void Fix()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            var npc = PrefabUtility.LoadPrefabContents(PrefabPath);
            var visual = npc.transform.Find("Animated Character Model");
            if (visual == null)
            {
                Debug.LogError("Could not fix NPC direction: Animated Character Model was not found.");
                PrefabUtility.UnloadPrefabContents(npc);
                return;
            }

            visual.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var bounds = CalculateBounds(visual.gameObject);
            visual.position += new Vector3(
                npc.transform.position.x - bounds.center.x,
                npc.transform.position.y - bounds.min.y,
                npc.transform.position.z - bounds.center.z);

            PrefabUtility.SaveAsPrefabAsset(npc, PrefabPath);
            PrefabUtility.UnloadPrefabContents(npc);
            Debug.Log("NPC visual direction corrected to face movement direction.");
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
