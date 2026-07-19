using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

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
    [SerializeField] private AudioSource footstepSource;

    private Dictionary<string, SoundEntry> _soundDict = new Dictionary<string, SoundEntry>();
    private Coroutine _stopSfxCoroutine;

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

        // Tự động nạp toàn bộ âm thanh từ AudioDatabase (cơ chế chống lỗi tên file khi Build)
        LoadFromDatabase();

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
        if (musicSource != null) musicSource.spatialBlend = 0f;

        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
        }
        if (ambienceSource != null) ambienceSource.spatialBlend = 0f;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        if (sfxSource != null) sfxSource.spatialBlend = 0f;

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = false;
            footstepSource.playOnAwake = false;
        }
        if (footstepSource != null) footstepSource.spatialBlend = 0f;
    }

    private void LoadFromDatabase()
    {
        AudioDatabase db = Resources.Load<AudioDatabase>("AudioDatabase");
        if (db != null)
        {
            foreach (var map in db.mappings)
            {
                if (map.clip != null)
                {
                    // Tránh thêm trùng lặp nếu đã có trên Inspector
                    bool exists = false;
                    foreach (var s in sounds)
                    {
                        if (s.name == map.key) { exists = true; break; }
                    }
                    if (!exists)
                    {
                        sounds.Add(new SoundEntry { name = map.key, clip = map.clip, volume = 1f });
                    }
                }
            }
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
    public void PlaySFX(string soundName, float volumeScale = 1f, float pitch = 1f, bool stopPrevious = false, float startOffset = 0f, float duration = 0f)
    {
        if (TryGetSound(soundName, out SoundEntry entry))
        {
            if (!CanPlayInCurrentScene(entry.clip)) return;
            if (sfxSource == null) return;
            if (stopPrevious && sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
            if (_stopSfxCoroutine != null)
            {
                StopCoroutine(_stopSfxCoroutine);
                _stopSfxCoroutine = null;
            }
            sfxSource.pitch = pitch;
            if (startOffset > 0f || duration > 0f || stopPrevious)
            {
                sfxSource.clip = entry.clip;
                sfxSource.volume = entry.volume * volumeScale;
                sfxSource.time = startOffset;
                sfxSource.Play();
                if (duration > 0f)
                {
                    _stopSfxCoroutine = StartCoroutine(StopSfxAfterDuration(duration));
                }
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
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f, bool stopPrevious = false, float startOffset = 0f, float duration = 0f)
    {
        if (clip != null && sfxSource != null && CanPlayInCurrentScene(clip))
        {
            if (stopPrevious && sfxSource.isPlaying)
            {
                sfxSource.Stop();
            }
            if (_stopSfxCoroutine != null)
            {
                StopCoroutine(_stopSfxCoroutine);
                _stopSfxCoroutine = null;
            }
            sfxSource.pitch = pitch;
            if (startOffset > 0f || duration > 0f || stopPrevious)
            {
                sfxSource.clip = clip;
                sfxSource.volume = volumeScale;
                sfxSource.time = startOffset;
                sfxSource.Play();
                if (duration > 0f)
                {
                    _stopSfxCoroutine = StartCoroutine(StopSfxAfterDuration(duration));
                }
            }
            else
            {
                sfxSource.PlayOneShot(clip, volumeScale);
            }
        }
    }

    private static bool CanPlayInCurrentScene(AudioClip clip)
    {
        if (clip == null) return false;

        // Voice cua phan mo dau chi hop le trong IntroScene. Day la lop bao ve
        // cuoi cung neu mot prefab/UI gameplay vo tinh giu tham chieu toi clip Intro.
        bool isIntroVoice = clip.name.StartsWith("Intro_", System.StringComparison.OrdinalIgnoreCase);
        return !isIntroVoice || SceneManager.GetActiveScene().name == "IntroScene";
    }

    private System.Collections.IEnumerator StopSfxAfterDuration(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }

    /// <summary>
    /// Phát tiếng bước chân (dừng gọn tiếng trước đó để không bao giờ bị dội hay vang echo, giảm âm lượng cho êm)
    /// </summary>
    public void PlayFootstep(string soundName, float volumeScale = 1f, float pitch = 1f)
    {
        if (TryGetSound(soundName, out SoundEntry entry))
        {
            if (footstepSource == null) SetupAudioSources();
            if (footstepSource == null) return;
            
            footstepSource.spatialBlend = 0f; // Luôn đảm bảo 2D sound không bị suy giảm theo khoảng cách
            float baseVol = (entry.volume <= 0.05f) ? 1f : entry.volume;
            float finalVolume = Mathf.Clamp(baseVol * volumeScale, 0.1f, 1f);
            
            if (footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
            
            footstepSource.clip = entry.clip;
            footstepSource.volume = finalVolume;
            footstepSource.pitch = pitch;
            footstepSource.Play();
        }
    }

    /// <summary>
    /// Dừng ngay tiếng bước chân khi nhân vật dừng di chuyển
    /// </summary>
    public void StopFootstep()
    {
        if (footstepSource != null && footstepSource.isPlaying)
        {
            footstepSource.Stop();
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
    public void PlayAmbience(string soundName, float volumeScale = 1f)
    {
        if (TryGetSound(soundName, out SoundEntry entry))
        {
            if (ambienceSource != null && ambienceSource.clip == entry.clip && ambienceSource.isPlaying) return;

            ambienceSource.clip = entry.clip;
            ambienceSource.volume = entry.volume * volumeScale;
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
        if (_soundDict.TryGetValue(soundName, out entry))
        {
            return true;
        }

        // Tự động tải từ Resources nếu có
        AudioClip clip = Resources.Load<AudioClip>(soundName);
        if (clip == null) clip = Resources.Load<AudioClip>($"Audio/{soundName}");
        if (clip == null) clip = Resources.Load<AudioClip>($"Audio/SFX/{soundName}");
        if (clip == null) clip = Resources.Load<AudioClip>($"Audio/Ambience/{soundName}");

#if UNITY_EDITOR
        // Trong Editor: tự động tìm và nạp thẳng từ thư mục dự án nếu chưa gán vào Inspector hoặc Resources
        if (clip == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{soundName} t:AudioClip");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                AudioClip foundClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (foundClip != null && foundClip.name == soundName)
                {
                    clip = foundClip;
                    break;
                }
            }
            if (clip == null && guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
#endif

        if (clip != null)
        {
            entry = new SoundEntry { name = soundName, clip = clip, volume = 1f };
            sounds.Add(entry);
            _soundDict[soundName] = entry;
            if (musicSource == null || ambienceSource == null || sfxSource == null) SetupAudioSources();
            return true;
        }

        return false;
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
