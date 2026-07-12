using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

[System.Serializable]
public class SubtitleEntry
{
    public string speaker;
    public string message;
    public float duration;
    public string sfxName;

    public SubtitleEntry(string speaker, string message, float duration = 4f, string sfxName = null)
    {
        this.speaker = speaker;
        this.message = message;
        this.duration = duration;
        this.sfxName = sfxName;
    }
}

/// <summary>
/// Hệ thống Quản lý Phụ đề (Vietsub) & Lời thoại - Singleton tự động khởi tạo trên Canvas.
/// Hỗ trợ cả Phân cảnh Intro Mở đầu (Màn hình đen + thức dậy trong phòng ngủ + Vietsub dưới đáy màn hình)
/// và hiển thị phụ đề tức thì trong gameplay cho các hệ thống: Khám bệnh FPP, Minigame, Khách hàng, Ve chai, Sự kiện.
/// </summary>
public class SubtitleManager : MonoBehaviour
{
    private static SubtitleManager _instance;
    public static SubtitleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SubtitleManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SubtitleManager");
                    _instance = go.AddComponent<SubtitleManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private GameObject _subtitlePanel;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _messageText;
    private RectTransform _subtitleRect;

    private GameObject _introBlackPanel;
    private CanvasGroup _introBlackGroup;

    private Coroutine _currentSubtitleCoroutine;
    private Coroutine _currentSequenceCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        CreateSubtitleUI();
    }

    private void CreateSubtitleUI()
    {
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("HUD_Canvas");
        if (existingCanvas != null) canvas = existingCanvas.GetComponent<Canvas>();
        else canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null) return;

        // 1. Tạo Intro Black Panel (Màn hình đen lúc thức dậy trong phòng ngủ)
        _introBlackPanel = new GameObject("Intro_BlackScreen");
        _introBlackPanel.transform.SetParent(canvas.transform, false);
        RectTransform blackRect = _introBlackPanel.AddComponent<RectTransform>();
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;

        Image blackImg = _introBlackPanel.AddComponent<Image>();
        blackImg.color = Color.black;

        _introBlackGroup = _introBlackPanel.AddComponent<CanvasGroup>();
        _introBlackGroup.alpha = 0f;
        _introBlackGroup.blocksRaycasts = false;
        _introBlackPanel.SetActive(false);

        // 2. Tạo Subtitle Panel (Khung phụ đề dưới đáy màn hình)
        _subtitlePanel = new GameObject("SubtitlePanel");
        _subtitlePanel.transform.SetParent(canvas.transform, false);
        _subtitleRect = _subtitlePanel.AddComponent<RectTransform>();
        _subtitleRect.anchorMin = new Vector2(0.5f, 0f);
        _subtitleRect.anchorMax = new Vector2(0.5f, 0f);
        _subtitleRect.pivot = new Vector2(0.5f, 0f);
        _subtitleRect.anchoredPosition = new Vector2(0, 30); // Cách đáy 30px
        _subtitleRect.sizeDelta = new Vector2(900, 130);

        Image bg = _subtitlePanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.08f, 0.88f); // Màu đen mờ cơ khí Sài Gòn

        // Speaker Label (Tên nhân vật / âm thanh)
        GameObject speakerObj = new GameObject("SpeakerText");
        speakerObj.transform.SetParent(_subtitlePanel.transform, false);
        RectTransform speakerRect = speakerObj.AddComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0, 1);
        speakerRect.anchorMax = new Vector2(1, 1);
        speakerRect.pivot = new Vector2(0.5f, 1);
        speakerRect.anchoredPosition = new Vector2(0, -10);
        speakerRect.sizeDelta = new Vector2(-40, 32);

        _speakerText = speakerObj.AddComponent<TextMeshProUGUI>();
        _speakerText.fontSize = 24;
        _speakerText.alignment = TextAlignmentOptions.TopLeft;
        _speakerText.color = new Color(1f, 0.84f, 0f, 1f); // Vàng Gold
        _speakerText.fontStyle = FontStyles.Bold;

        // Message Text (Nội dung phụ đề Vietsub)
        GameObject msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(_subtitlePanel.transform, false);
        RectTransform msgRect = msgObj.AddComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0, 0);
        msgRect.anchorMax = new Vector2(1, 1);
        msgRect.offsetMin = new Vector2(20, 15);
        msgRect.offsetMax = new Vector2(-20, -45);

        _messageText = msgObj.AddComponent<TextMeshProUGUI>();
        _messageText.fontSize = 26;
        _messageText.alignment = TextAlignmentOptions.TopLeft;
        _messageText.color = Color.white;
        _messageText.enableWordWrapping = true;

        _subtitlePanel.SetActive(false);
    }

    /// <summary>
    /// Hiển thị một câu phụ đề nhanh trên màn hình
    /// </summary>
    public void ShowSubtitle(string speaker, string message, float duration = 4f, string sfxName = null)
    {
        if (_subtitlePanel == null) CreateSubtitleUI();
        if (_subtitlePanel == null) return;

        if (_currentSubtitleCoroutine != null) StopCoroutine(_currentSubtitleCoroutine);
        _currentSubtitleCoroutine = StartCoroutine(AnimateSubtitle(speaker, message, duration, sfxName));
    }

    /// <summary>
    /// Chạy chuỗi hội thoại / Intro tuần tự (Màn hình đen -> Thức dậy trong phòng ngủ -> Vietsub)
    /// </summary>
    public void PlayIntroSequence(System.Action onComplete = null)
    {
        if (_subtitlePanel == null) CreateSubtitleUI();
        if (_subtitlePanel == null) return;

        if (_currentSequenceCoroutine != null) StopCoroutine(_currentSequenceCoroutine);
        _currentSequenceCoroutine = StartCoroutine(IntroSequenceRoutine(onComplete));
    }

    private IEnumerator IntroSequenceRoutine(System.Action onComplete)
    {
        // 1. Phủ màn hình đen lúc mới vào game (Nhân vật thức dậy trong phòng ngủ)
        if (_introBlackPanel != null && _introBlackGroup != null)
        {
            _introBlackPanel.SetActive(true);
            _introBlackGroup.alpha = 1f;
            _introBlackGroup.blocksRaycasts = true;
        }

        // Phát âm thanh môi trường Sài Gòn buổi sáng
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAmbience("Âm thanh buổi sáng");
        }

        yield return new WaitForSeconds(1f);

        // 2. Chạy lời thoại dẫn dắt bối cảnh (Màn hình vẫn đen)
        yield return StartCoroutine(AnimateSubtitle("Bối Cảnh", "[Tiếng mưa dột lộp bộp trên mái tôn... Tiếng xe máy rộn ràng ngoài hẻm nhỏ]", 3.5f, "Tiếng bước chân"));
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(AnimateSubtitle("Anh Thợ Điện (Tự nhủ)", "Bảy giờ sáng rồi... Lại một ngày mới trong căn phòng trọ chật hẹp kiêm tiệm sửa đồ này.", 4.0f, "Tiếng gõ phím"));
        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(AnimateSubtitle("Anh Thợ Điện (Tự nhủ)", "Hôm nay phải ráng sửa đồ thật cẩn thận, tích góp tiền nâng cấp đồ nghề mới được!", 3.5f, "Tiếng gõ phím"));
        yield return new WaitForSeconds(0.5f);

        // 3. Mở mắt thức dậy (Fade-out màn hình đen trong 2 giây)
        if (_introBlackGroup != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Tiếng mở cửa");
            }
            float fadeTime = 2f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                _introBlackGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }
            _introBlackGroup.blocksRaycasts = false;
            _introBlackPanel.SetActive(false);
        }

        _subtitlePanel.SetActive(false);
        onComplete?.Invoke();
    }

    /// <summary>
    /// Phát chuỗi nhiều câu phụ đề liên tiếp
    /// </summary>
    public void ShowSubtitlesSequence(SubtitleEntry[] entries, System.Action onComplete = null)
    {
        if (_subtitlePanel == null) CreateSubtitleUI();
        if (_subtitlePanel == null || entries == null || entries.Length == 0) return;

        if (_currentSequenceCoroutine != null) StopCoroutine(_currentSequenceCoroutine);
        _currentSequenceCoroutine = StartCoroutine(SequenceRoutine(entries, onComplete));
    }

    private IEnumerator SequenceRoutine(SubtitleEntry[] entries, System.Action onComplete)
    {
        foreach (var entry in entries)
        {
            yield return StartCoroutine(AnimateSubtitle(entry.speaker, entry.message, entry.duration, entry.sfxName));
            yield return new WaitForSeconds(0.3f);
        }
        _subtitlePanel.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator AnimateSubtitle(string speaker, string message, float duration, string sfxName = null)
    {
        _subtitlePanel.SetActive(true);
        _speakerText.text = string.IsNullOrEmpty(speaker) ? "" : $"[{speaker}]";
        _messageText.text = "";

        // Phát âm thanh kèm lời thoại/phụ đề
        if (!string.IsNullOrEmpty(sfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfxName);
        }

        // Hiệu ứng Typewriter gõ chữ từng ký tự
        int totalChars = message.Length;
        float typeDelay = Mathf.Min(0.025f, (duration * 0.4f) / Mathf.Max(1, totalChars));

        for (int i = 0; i <= totalChars; i++)
        {
            _messageText.text = message.Substring(0, i);
            yield return new WaitForSeconds(typeDelay);
        }

        // Giữ phụ đề trên màn hình cho hết thời lượng
        yield return new WaitForSeconds(Mathf.Max(1f, duration * 0.6f));
        _subtitlePanel.SetActive(false);
    }
}
