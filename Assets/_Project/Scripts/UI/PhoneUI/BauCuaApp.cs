using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BauCuaApp : BaseApp
{
    private const int AnimalCount = 6;
    private const int DiceCount = 3;

    [Header("Bet Settings")]
    [SerializeField] private float[] chipValues = { 5000f, 10000f, 20000f, 50000f, 100000f };
    [SerializeField] private int startingChipIndex = 1;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI chipText;
    [SerializeField] private TextMeshProUGUI totalBetText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button lowerBetButton;
    [SerializeField] private Button raiseBetButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button rollButton;
    [SerializeField] private Button[] animalButtons;
    [SerializeField] private TextMeshProUGUI[] animalBetTexts;
    [SerializeField] private Image[] animalBackgrounds;
    [SerializeField] private Image[] diceIcons;
    [SerializeField] private TextMeshProUGUI[] diceTexts;
    [SerializeField] private Image[] diceBackgrounds;
    [SerializeField] private Sprite[] animalSprites;

    private readonly string[] _animalNames = { "BẦU", "CUA", "TÔM", "CÁ", "GÀ", "NAI" };
    private readonly float[] _animalBets = new float[AnimalCount];
    private readonly int[] _lastRolls = { -1, -1, -1 };
    private readonly Color[] _animalColors =
    {
        new Color(0.18f, 0.62f, 0.36f),
        new Color(0.82f, 0.16f, 0.16f),
        new Color(0.95f, 0.43f, 0.22f),
        new Color(0.12f, 0.48f, 0.82f),
        new Color(0.9f, 0.65f, 0.16f),
        new Color(0.55f, 0.34f, 0.18f)
    };

    private EconomyManager _economy;
    private bool _controlsBound;
    private bool _initialized;
    private int _chipIndex;

    protected override void OnAppOpened()
    {
        EnsureBound();

        if (!_initialized)
        {
            _chipIndex = Mathf.Clamp(startingChipIndex, 0, chipValues.Length - 1);
            _initialized = true;
            ResetDice();
            ClearBets(false);
            SetStatus("Chọn cửa rồi bấm Lắc.", new Color(0.9f, 0.9f, 0.95f));
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (_economy != null)
        {
            _economy.OnCashChanged -= OnCashChanged;
        }

        _controlsBound = false;
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

        BindButton(lowerBetButton, DecreaseChip);
        BindButton(raiseBetButton, IncreaseChip);
        BindButton(clearButton, () => ClearBets(true));
        BindButton(rollButton, RollDice);

        if (animalButtons != null)
        {
            for (int i = 0; i < animalButtons.Length && i < AnimalCount; i++)
            {
                int animalIndex = i;
                BindButton(animalButtons[i], () => PlaceBet(animalIndex));
            }
        }

        _controlsBound = true;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void PlaceBet(int animalIndex)
    {
        EnsureBound();

        if (animalIndex < 0 || animalIndex >= AnimalCount)
        {
            return;
        }

        if (_economy == null)
        {
            SetStatus("Không tìm thấy ví tiền.", new Color(1f, 0.42f, 0.42f));
            return;
        }

        float chip = CurrentChipValue();
        if (GetTotalStake() + chip > _economy.CurrentCash)
        {
            SetStatus("Không đủ tiền để đặt thêm.", new Color(1f, 0.42f, 0.42f));
            return;
        }

        _animalBets[animalIndex] += chip;
        SetStatus($"Đặt {chip:N0} VNĐ vào {_animalNames[animalIndex]}.", new Color(1f, 0.86f, 0.35f));
        RefreshBetTexts();
    }

    private void DecreaseChip()
    {
        _chipIndex = Mathf.Max(0, _chipIndex - 1);
        RefreshControls();
    }

    private void IncreaseChip()
    {
        _chipIndex = Mathf.Min(chipValues.Length - 1, _chipIndex + 1);
        RefreshControls();
    }

    private void ClearBets(bool showStatus)
    {
        for (int i = 0; i < _animalBets.Length; i++)
        {
            _animalBets[i] = 0f;
        }

        if (showStatus)
        {
            SetStatus("Đã xóa tiền cược.", new Color(0.9f, 0.9f, 0.95f));
        }

        RefreshBetTexts();
    }

    private void RollDice()
    {
        EnsureBound();

        float totalStake = GetTotalStake();
        if (totalStake <= 0f)
        {
            SetStatus("Chọn ít nhất một cửa để cược.", new Color(1f, 0.86f, 0.35f));
            return;
        }

        if (_economy == null)
        {
            SetStatus("Không tìm thấy ví tiền.", new Color(1f, 0.42f, 0.42f));
            return;
        }

        if (!_economy.SpendCash(totalStake))
        {
            SetStatus("Không đủ tiền để lắc.", new Color(1f, 0.42f, 0.42f));
            return;
        }

        int[] counts = new int[AnimalCount];
        for (int i = 0; i < DiceCount; i++)
        {
            int rolledAnimal = Random.Range(0, AnimalCount);
            _lastRolls[i] = rolledAnimal;
            counts[rolledAnimal]++;
        }

        float payout = 0f;
        for (int i = 0; i < AnimalCount; i++)
        {
            if (_animalBets[i] <= 0f || counts[i] <= 0)
            {
                continue;
            }

            payout += _animalBets[i] * (counts[i] + 1);
        }

        if (payout > 0f)
        {
            _economy.AddCash(payout);
        }

        float profit = payout - totalStake;
        UpdateDiceTexts();
        UpdateResultText(totalStake, payout, profit);
        ClearBets(false);
        RefreshBalance();

        if (profit > 0f && ToastNotificationManager.Instance != null)
        {
            ToastNotificationManager.Instance.ShowToast($"Bầu Cua thắng {profit:N0} VNĐ!", 3f);
        }
    }

    private void UpdateResultText(float totalStake, float payout, float profit)
    {
        string diceLine = $"Ra: {_animalNames[_lastRolls[0]]} - {_animalNames[_lastRolls[1]]} - {_animalNames[_lastRolls[2]]}";

        if (profit > 0f)
        {
            SetStatus($"Thắng +{profit:N0} VNĐ.", new Color(0.35f, 1f, 0.5f));
            if (resultText != null)
            {
                resultText.text = $"{diceLine}\nNhận {payout:N0} VNĐ, lời {profit:N0} VNĐ.";
            }
        }
        else if (Mathf.Approximately(profit, 0f))
        {
            SetStatus("Hòa vốn.", new Color(0.9f, 0.9f, 0.95f));
            if (resultText != null)
            {
                resultText.text = $"{diceLine}\nNhận lại {payout:N0} VNĐ.";
            }
        }
        else
        {
            SetStatus($"Thua {Mathf.Abs(profit):N0} VNĐ.", new Color(1f, 0.42f, 0.42f));
            if (resultText != null)
            {
                resultText.text = $"{diceLine}\nĐã cược {totalStake:N0} VNĐ, nhận {payout:N0} VNĐ.";
            }
        }
    }

    private void RefreshAll()
    {
        RefreshBalance();
        RefreshControls();
        RefreshBetTexts();
        UpdateDiceTexts();
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

    private void RefreshControls()
    {
        if (chipText != null)
        {
            chipText.text = $"Mức cược: {CurrentChipValue():N0} VNĐ";
        }

        if (lowerBetButton != null)
        {
            lowerBetButton.interactable = _chipIndex > 0;
        }

        if (raiseBetButton != null)
        {
            raiseBetButton.interactable = _chipIndex < chipValues.Length - 1;
        }
    }

    private void RefreshBetTexts()
    {
        float totalStake = GetTotalStake();
        if (totalBetText != null)
        {
            totalBetText.text = totalStake > 0f ? $"Đang đặt: {totalStake:N0} VNĐ" : "Chạm vào ô để đặt cược";
        }

        for (int i = 0; i < AnimalCount; i++)
        {
            float bet = _animalBets[i];
            if (animalBetTexts != null && i < animalBetTexts.Length && animalBetTexts[i] != null)
            {
                animalBetTexts[i].text = bet > 0f ? $"{bet:N0}" : string.Empty;
                animalBetTexts[i].color = bet > 0f ? new Color(1f, 0.92f, 0.35f) : new Color(0.82f, 0.84f, 0.9f);
            }

            if (animalBackgrounds != null && i < animalBackgrounds.Length && animalBackgrounds[i] != null)
            {
                Color target = i < _animalColors.Length ? _animalColors[i] : Color.gray;
                animalBackgrounds[i].color = bet > 0f ? Color.Lerp(target, Color.white, 0.18f) : target;
            }
        }
    }

    private void UpdateDiceTexts()
    {
        for (int i = 0; i < DiceCount; i++)
        {
            int rolledAnimal = i < _lastRolls.Length ? _lastRolls[i] : -1;
            bool hasResult = rolledAnimal >= 0 && rolledAnimal < AnimalCount;

            if (diceTexts != null && i < diceTexts.Length && diceTexts[i] != null)
            {
                diceTexts[i].text = hasResult ? _animalNames[rolledAnimal] : "?";
            }

            if (diceBackgrounds != null && i < diceBackgrounds.Length && diceBackgrounds[i] != null)
            {
                diceBackgrounds[i].color = hasResult && rolledAnimal < _animalColors.Length
                    ? _animalColors[rolledAnimal]
                    : new Color(0.18f, 0.18f, 0.22f);
            }

            if (diceIcons != null && i < diceIcons.Length && diceIcons[i] != null)
            {
                Sprite sprite = hasResult && animalSprites != null && rolledAnimal < animalSprites.Length
                    ? animalSprites[rolledAnimal]
                    : null;
                diceIcons[i].sprite = sprite;
                diceIcons[i].enabled = sprite != null;
            }
        }
    }

    private void ResetDice()
    {
        for (int i = 0; i < _lastRolls.Length; i++)
        {
            _lastRolls[i] = -1;
        }

        if (resultText != null)
        {
            resultText.text = "Đặt tiền vào Bầu/Cua/Tôm/Cá/Gà/Nai rồi lắc.";
        }
    }

    private float CurrentChipValue()
    {
        if (chipValues == null || chipValues.Length == 0)
        {
            return 10000f;
        }

        _chipIndex = Mathf.Clamp(_chipIndex, 0, chipValues.Length - 1);
        return chipValues[_chipIndex];
    }

    private float GetTotalStake()
    {
        float total = 0f;
        for (int i = 0; i < _animalBets.Length; i++)
        {
            total += _animalBets[i];
        }

        return total;
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
}
