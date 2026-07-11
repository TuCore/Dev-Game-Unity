using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Hệ thống Quản lý Âm thanh (AudioManager - Singleton).
/// Điều phối toàn bộ Nhạc nền (Music), Tiếng môi trường (Ambience) và Hiệu ứng âm thanh (SFX).
/// Tự động tồn tại qua các Scene nhờ DontDestroyOnLoad.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class SoundEntry
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Danh sách Âm thanh (Audio Clips)")]
    [SerializeField] private List<SoundEntry> sounds = new List<SoundEntry>();

    [Header("Nguồn phát âm thanh (Audio Sources)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;

    private Dictionary<string, SoundEntry> _soundDict = new Dictionary<string, SoundEntry>();

    private void Awake()
    {
        // Thiết lập Singleton & DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo các AudioSource nếu chưa được gán trên Inspector
        SetupAudioSources();

        // Xây dựng từ điển tra cứu nhanh âm thanh theo tên
        BuildSoundDictionary();
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    private void BuildSoundDictionary()
    {
        _soundDict.Clear();
        foreach (var entry in sounds)
        {
            if (entry != null && entry.clip != null && !string.IsNullOrEmpty(entry.name))
            {
                entry.clip.LoadAudioData();
                _soundDict[entry.name] = entry;
            }
        }
    }

    /// <summary>
    /// Phát âm thanh hiệu ứng (SFX) theo tên (Ví dụ: "Tiếng vô điện", "Tiếng bước chân")
    /// </summary>
    public void PlaySFX(string soundName, float volumeScale = 1f, float pitch = 1f, bool stopPrevious = false, float startOffset = 0f)
    {
        if (TryGetSound(soundName, out SoundEntry entry))
        {
            if (sfxSource == null) return;
            if (stopPrevious && sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
            sfxSource.pitch = pitch;
            if (startOffset > 0f)
            {
                sfxSource.clip = entry.clip;
                sfxSource.volume = entry.volume * volumeScale;
                sfxSource.time = startOffset;
                sfxSource.Play();
            }
            else
            {
                sfxSource.PlayOneShot(entry.clip, entry.volume * volumeScale);
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy SFX có tên: {soundName}");
        }
    }

    /// <summary>
    /// Phát trực tiếp một AudioClip dưới dạng SFX
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f, bool stopPrevious = false, float startOffset = 0f)
    {
        if (clip != null && sfxSource != null)
        {
            if (stopPrevious && sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
            sfxSource.pitch = pitch;
            if (startOffset > 0f)
            {
                sfxSource.clip = clip;
                sfxSource.volume = volumeScale;
                sfxSource.time = startOffset;
                sfxSource.Play();
            }
            else
            {
                sfxSource.PlayOneShot(clip, volumeScale);
            }
        }
    }

    /// <summary>
    /// Phát hoặc đổi nhạc nền (Music)
    /// </summary>
    public void PlayMusic(string soundName, float fadeDuration = 0.5f)
    {
        if (TryGetSound(soundName, out SoundEntry entry))
        {
            if (musicSource != null && musicSource.clip == entry.clip && musicSource.isPlaying) return; // Đang phát đúng bài rồi thì bỏ qua
            
            musicSource.clip = entry.clip;
            musicSource.volume = entry.volume;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Không tìm thấy Music có tên: {soundName}");
        }
    }

    /// <summary>
    /// Dừng nhạc nền
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Phát tiếng môi trường (Ambience - tiếng mưa, tiếng chim hót buổi sáng...)
    /// </summary>
    public void PlayAmbience(string soundName)
    {
        if (TryGetSound(soundName, out SoundEntry entry))
        {
            if (ambienceSource != null && ambienceSource.clip == entry.clip && ambienceSource.isPlaying) return;

            ambienceSource.clip = entry.clip;
            ambienceSource.volume = entry.volume;
            ambienceSource.Play();
        }
    }

    /// <summary>
    /// Dừng tiếng môi trường
    /// </summary>
    public void StopAmbience()
    {
        if (ambienceSource != null && ambienceSource.isPlaying)
        {
            ambienceSource.Stop();
        }
    }

    private bool TryGetSound(string soundName, out SoundEntry entry)
    {
        if (_soundDict.TryGetValue(soundName, out entry))
        {
            return true;
        }

        // Tự động tìm lại dictionary nếu mới thêm vào list khi game đang chạy
        BuildSoundDictionary();
        return _soundDict.TryGetValue(soundName, out entry);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Tiện ích trong Unity Editor: Tự động quét toàn bộ thư mục Audio và nạp vào danh sách Sounds.
    /// Bạn chỉ cần bấm chuột phải vào component AudioManager -> Chọn "Tự động quét & nạp toàn bộ Audio"
    /// </summary>
    [ContextMenu("Tự động quét & nạp toàn bộ Audio (Auto Find All Audio Clips)")]
    private void AutoFindAllAudioClips()
    {
        string audioFolder = "Assets/_Project/Audio";
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { audioFolder });

        HashSet<string> existingNames = new HashSet<string>();
        foreach (var s in sounds)
        {
            if (s != null && !string.IsNullOrEmpty(s.name)) existingNames.Add(s.name);
        }

        int addedCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                string clipName = clip.name;
                if (!existingNames.Contains(clipName))
                {
                    sounds.Add(new SoundEntry { name = clipName, clip = clip, volume = 1f });
                    existingNames.Add(clipName);
                    addedCount++;
                }
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"[AudioManager] Đã nạp thành công {addedCount} tập tin âm thanh mới từ thư mục {audioFolder}!");
    }
#endif
}
