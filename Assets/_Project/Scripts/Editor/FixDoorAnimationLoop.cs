using UnityEditor;
using UnityEngine;

public class FixDoorAnimationLoop
{
    [MenuItem("Tools/Fix Door Animation Loop")]
    public static void FixLoop()
    {
        string path = "Assets/_Project/Art/Models/Door/roller-shutters-animation/source/AnimatedRollingDoor.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }
            
            bool changed = false;
            foreach (var clip in clips)
            {
                if (clip.loopTime)
                {
                    clip.loopTime = false;
                    changed = true;
                    Debug.Log("Fixed Loop Time for clip: " + clip.name);
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                Debug.Log("Successfully fixed and reimported AnimatedRollingDoor.fbx!");
            }
            else
            {
                Debug.Log("Loop Time was already false or no clips found.");
            }
        }
        else
        {
            Debug.LogError("Could not find AnimatedRollingDoor.fbx at path: " + path);
        }
    }
}
