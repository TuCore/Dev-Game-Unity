using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScratchLotteryApp : BaseApp
{
    [Header("Lottery Settings")]
    [SerializeField] private float ticketPrice = 10000f;
    [SerializeField, Range(0.25f, 0.9f)] private float revealThreshold = 0.58f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI revealText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private TextMeshProUGUI claimButtonText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button claimButton;
    [SerializeField] private ScratchTicketSurface scratchSurface;

    private EconomyManager _economy;
    private bool _controlsBound;
    private bool _hasActiveTicket;
    private bool _ticketResolved;
    private float _currentPrize;

    protected override void OnAppOpened()
    {
        EnsureBound();
        RefreshBalance();

        if (!_hasActiveTicket)
        {
            SetIdleState();
        }
    }

    private void OnDisable()
    {
        if (_economy != null)
        {
            _economy.OnCashChanged -= OnCashChanged;
        }
    }

    private void EnsureBound()
    {
        if (_controlsBound)
        {
            return;
        }

        _economy = EconomyManager.Instance != null ? EconomyManager.Instance : FindFirstObjectByType<EconomyManager>();
        if (_economy != null)
        {
            _economy.OnCashChanged -= OnCashChanged;
            _economy.OnCashChanged += OnCashChanged;
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyTicket);
            buyButton.onClick.AddListener(BuyTicket);
        }

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(ClaimPrize);
            claimButton.onClick.AddListener(ClaimPrize);
        }

        if (scratchSurface != null)
        {
            scratchSurface.Initialize(this, revealThreshold);
        }

        _controlsBound = true;
    }

    private void BuyTicket()
    {
        EnsureBound();

        if (_economy == null)
        {
            SetStatus("Không tìm thấy ví tiền.", new Color(1f, 0.42f, 0.42f));
            return;
        }

        if (!_economy.SpendCash(ticketPrice))
        {
            SetStatus("Không đủ tiền mua vé.", new Color(1f, 0.42f, 0.42f));
            return;
        }

        _currentPrize = RollPrize();
        _hasActiveTicket = true;
        _ticketResolved = false;

        if (revealText != null)
        {
            revealText.text = _currentPrize > 0f
                ? $"TRÚNG\n{_currentPrize:N0} VNĐ"
                : "CHÚC MAY\nMẮN LẦN SAU";
            revealText.color = _currentPrize > 0f ? new Color(0.1f, 0.55f, 0.18f, 1f) : new Color(0.35f, 0.36f, 0.4f, 1f);
        }

        if (resultText != null)
        {
            resultText.text = "Cào lớp bạc để mở kết quả.";
        }

        SetStatus($"Đã mua vé {ticketPrice:N0} VNĐ", new Color(1f, 0.84f, 0.3f));

        if (scratchSurface != null)
        {
            scratchSurface.ResetTicket();
            scratchSurface.SetScratchEnabled(true);
        }

        SetButtonState(buyButton, buyButtonText, false, "Đã mua");
        SetButtonState(claimButton, claimButtonText, false, "Nhận");
        RefreshBalance();
    }

    internal void OnTicketRevealed(float scratchedRatio)
    {
        if (!_hasActiveTicket || _ticketResolved)
        {
            return;
        }

        _ticketResolved = true;
        if (scratchSurface != null)
        {
            scratchSurface.RevealAll();
            scratchSurface.SetScratchEnabled(false);
        }

        if (_currentPrize > 0f)
        {
            SetStatus("Vé trúng thưởng!", new Color(0.35f, 1f, 0.5f));
            if (resultText != null)
            {
                resultText.text = $"Bạn trúng {_currentPrize:N0} VNĐ.";
            }
            SetButtonState(claimButton, claimButtonText, true, "Nhận");
        }
        else
        {
            SetStatus("Vé không trúng.", new Color(0.9f, 0.9f, 0.95f));
            if (resultText != null)
            {
                resultText.text = "Không trúng thưởng.";
            }
            SetButtonState(claimButton, claimButtonText, true, "Vé mới");
        }
    }

    private void ClaimPrize()
    {
        EnsureBound();

        if (!_ticketResolved)
        {
            return;
        }

        if (_currentPrize > 0f && _economy != null)
        {
            _economy.AddCash(_currentPrize);
            if (ToastNotificationManager.Instance != null)
            {
                ToastNotificationManager.Instance.ShowToast($"Vé cào trúng {_currentPrize:N0} VNĐ!", 3f);
            }
        }

        _currentPrize = 0f;
        _hasActiveTicket = false;
        _ticketResolved = false;
        SetIdleState();
        RefreshBalance();
    }

    private float RollPrize()
    {
        int roll = Random.Range(0, 1000);
        if (roll < 530) return 0f;
        if (roll < 760) return 5000f;
        if (roll < 890) return 10000f;
        if (roll < 960) return 20000f;
        if (roll < 990) return 50000f;
        return 100000f;
    }

    private void SetIdleState()
    {
        if (revealText != null)
        {
            revealText.text = "VÉ CÀO\nMAY MẮN";
            revealText.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        }

        if (resultText != null)
        {
            resultText.text = $"Giá vé: {ticketPrice:N0} VNĐ";
        }

        SetStatus("Sẵn sàng", new Color(0.92f, 0.92f, 0.95f));
        SetButtonState(buyButton, buyButtonText, true, "Mua vé");
        SetButtonState(claimButton, claimButtonText, false, "Nhận");

        if (scratchSurface != null)
        {
            scratchSurface.ResetTicket();
            scratchSurface.SetScratchEnabled(false);
        }
    }

    private void RefreshBalance()
    {
        if (balanceText == null)
        {
            return;
        }

        float cash = _economy != null ? _economy.CurrentCash : 0f;
        balanceText.text = $"Ví: {cash:N0} VNĐ";
    }

    private void OnCashChanged(float currentCash)
    {
        RefreshBalance();
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.color = color;
    }

    private void SetButtonState(Button button, TextMeshProUGUI label, bool interactable, string text)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }

        if (label != null)
        {
            label.text = text;
            label.alpha = interactable ? 1f : 0.55f;
        }
    }
}

public class ScratchTicketSurface : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RawImage coverImage;
    [SerializeField] private int textureWidth = 192;
    [SerializeField] private int textureHeight = 96;
    [SerializeField] private int brushRadius = 13;

    private ScratchLotteryApp _owner;
    private Texture2D _scratchTexture;
    private bool[] _scratchedPixels;
    private int _scratchedCount;
    private int _totalPixels;
    private bool _scratchEnabled;
    private float _revealThreshold = 0.58f;

    public void Initialize(ScratchLotteryApp owner, float revealThreshold)
    {
        _owner = owner;
        _revealThreshold = revealThreshold;
        if (coverImage == null)
        {
            coverImage = GetComponent<RawImage>();
        }

        if (coverImage != null)
        {
            coverImage.raycastTarget = _scratchEnabled;
        }
    }

    public void ResetTicket()
    {
        if (coverImage == null)
        {
            coverImage = GetComponent<RawImage>();
        }

        if (_scratchTexture == null)
        {
            _scratchTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            _scratchTexture.wrapMode = TextureWrapMode.Clamp;
            _scratchTexture.filterMode = FilterMode.Bilinear;
        }

        _totalPixels = textureWidth * textureHeight;
        _scratchedPixels = new bool[_totalPixels];
        _scratchedCount = 0;

        Color silverA = new Color(0.72f, 0.74f, 0.78f, 1f);
        Color silverB = new Color(0.92f, 0.94f, 0.96f, 1f);
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float stripe = Mathf.PingPong((x + y) * 0.045f, 1f);
                _scratchTexture.SetPixel(x, y, Color.Lerp(silverA, silverB, stripe * 0.45f));
            }
        }

        _scratchTexture.Apply(false);
        coverImage.texture = _scratchTexture;
        coverImage.color = Color.white;
        coverImage.raycastTarget = true;
        transform.SetAsLastSibling();
    }

    public void SetScratchEnabled(bool enabled)
    {
        _scratchEnabled = enabled;
        if (coverImage != null)
        {
            coverImage.raycastTarget = enabled;
        }

        if (enabled)
        {
            transform.SetAsLastSibling();
        }
    }

    public void RevealAll()
    {
        if (_scratchTexture == null)
        {
            return;
        }

        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                _scratchTexture.SetPixel(x, y, clear);
            }
        }

        _scratchTexture.Apply(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ScratchAt(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ScratchAt(eventData);
    }

    private void ScratchAt(PointerEventData eventData)
    {
        if (!_scratchEnabled || _scratchTexture == null || coverImage == null)
        {
            return;
        }

        RectTransform rect = coverImage.rectTransform;
        Camera eventCamera = eventData.pressEventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        Rect localRect = rect.rect;
        float normalizedX = Mathf.InverseLerp(localRect.xMin, localRect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(localRect.yMin, localRect.yMax, localPoint.y);
        int centerX = Mathf.RoundToInt(normalizedX * (textureWidth - 1));
        int centerY = Mathf.RoundToInt(normalizedY * (textureHeight - 1));

        if (centerX < 0 || centerX >= textureWidth || centerY < 0 || centerY >= textureHeight)
        {
            return;
        }

        Color clear = new Color(1f, 1f, 1f, 0f);
        int radiusSqr = brushRadius * brushRadius;
        bool changed = false;

        for (int y = centerY - brushRadius; y <= centerY + brushRadius; y++)
        {
            if (y < 0 || y >= textureHeight)
            {
                continue;
            }

            for (int x = centerX - brushRadius; x <= centerX + brushRadius; x++)
            {
                if (x < 0 || x >= textureWidth)
                {
                    continue;
                }

                int dx = x - centerX;
                int dy = y - centerY;
                if ((dx * dx) + (dy * dy) > radiusSqr)
                {
                    continue;
                }

                int index = y * textureWidth + x;
                if (_scratchedPixels[index])
                {
                    continue;
                }

                _scratchedPixels[index] = true;
                _scratchedCount++;
                _scratchTexture.SetPixel(x, y, clear);
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        _scratchTexture.Apply(false);
        float scratchedRatio = _totalPixels > 0 ? (float)_scratchedCount / _totalPixels : 0f;
        if (scratchedRatio >= _revealThreshold)
        {
            _owner?.OnTicketRevealed(scratchedRatio);
        }
    }
}
