using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/AudioDatabase")]
public class AudioDatabase : ScriptableObject
{
    [System.Serializable]
    public class AudioMapping
    {
        public string key;
        public AudioClip clip;
    }
    public List<AudioMapping> mappings = new List<AudioMapping>();
}
