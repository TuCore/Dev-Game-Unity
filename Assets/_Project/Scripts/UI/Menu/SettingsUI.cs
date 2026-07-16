using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace AnhThoDien.UI.Menu
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider sliderMasterVolume;
        [SerializeField] private Slider sliderBrightness;
        [SerializeField] private Slider sliderSensitivity;

        [Header("Close Button")]
        public Button btnClose;

        [Header("Keybinds Scroll View")]
        [SerializeField] private Transform keybindsContainer;
        [SerializeField] private GameObject keybindRowPrefab;

        private string _waitingForKeybind = null;
        private List<GameObject> _spawnedRows = new List<GameObject>();

        private Dictionary<string, string> _displayNames = new Dictionary<string, string>()
        {
            {"Interact", "Tương tác chính"},
            {"Secondary", "Tương tác phụ"},
            {"Jump", "Nhảy"},
            {"Run", "Chạy nhanh"},
            {"Phone", "Mở điện thoại"},
            {"MoveForward", "Đi tới"},
            {"MoveBackward", "Đi lùi"},
            {"MoveLeft", "Sang trái"},
            {"MoveRight", "Sang phải"}
        };

        private void Start()
        {
            // Set up sliders
            if (sliderMasterVolume != null)
            {
                sliderMasterVolume.value = PlayerPrefs.GetFloat("Vol_Master", 1f);
                sliderMasterVolume.onValueChanged.AddListener(OnVolumeChanged);
                OnVolumeChanged(sliderMasterVolume.value);
            }

            if (sliderBrightness != null)
            {
                sliderBrightness.value = PlayerPrefs.GetFloat("Brightness", 1f);
                sliderBrightness.onValueChanged.AddListener(OnBrightnessChanged);
            }

            if (sliderSensitivity != null)
            {
                sliderSensitivity.value = PlayerPrefs.GetFloat("MouseSensitivity", 100f);
                sliderSensitivity.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(CloseSettings);
            }

            // Build the keybinding list
            BuildKeybindList();

            // Automatically add hover/click sounds to elements in Settings
            AddSoundToSelectables();
        }

        private void Update()
        {
            if (_waitingForKeybind != null && Input.anyKeyDown)
            {
                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        if (key != KeyCode.Escape)
                        {
                            CustomInputManager.SetKey(_waitingForKeybind, key);
                        }
                        _waitingForKeybind = null;

                        // Rebuild keybinds UI to show the updated key
                        BuildKeybindList();
                        break;
                    }
                }
            }
        }

        private void BuildKeybindList()
        {
            if (keybindsContainer == null || keybindRowPrefab == null) return;

            // Clear old rows
            foreach (var row in _spawnedRows)
            {
                Destroy(row);
            }
            _spawnedRows.Clear();

            var keys = CustomInputManager.GetAllKeys();
            foreach (var kvp in keys)
            {
                if (!_displayNames.ContainsKey(kvp.Key)) continue;

                string keyName = kvp.Key;
                KeyCode currentKey = kvp.Value;

                GameObject rowInstance = Instantiate(keybindRowPrefab, keybindsContainer);
                _spawnedRows.Add(rowInstance);

                // Find elements inside the prefab
                var labelText = rowInstance.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (labelText != null)
                {
                    labelText.text = _displayNames[keyName];
                }

                var keyButton = rowInstance.transform.Find("KeyButton")?.GetComponent<Button>();
                var keyText = keyButton != null ? keyButton.GetComponentInChildren<TextMeshProUGUI>() : null;

                if (keyButton != null && keyText != null)
                {
                    keyText.text = _waitingForKeybind == keyName ? "[ Bấm phím mới... ]" : currentKey.ToString();
                    keyButton.onClick.AddListener(() =>
                    {
                        _waitingForKeybind = keyName;
                        keyText.text = "[ Bấm phím mới... ]";
                    });
                }
            }

            // Ensure any new buttons get sound effects
            AddSoundToSelectables();
        }

        private void OnVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("Vol_Master", value);
            AudioListener.volume = value;
        }

        private void OnBrightnessChanged(float value)
        {
            PlayerPrefs.SetFloat("Brightness", value);
            BrightnessManager.Instance?.UpdateBrightness();
        }

        private void OnSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", value);
        }

        private void CloseSettings()
        {
            _waitingForKeybind = null;
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }

        private void AddSoundToSelectables()
        {
            Selectable[] selectables = GetComponentsInChildren<Selectable>(true);
            foreach (Selectable sel in selectables)
            {
                if (sel.gameObject.GetComponent<UIButtonSound>() == null)
                {
                    sel.gameObject.AddComponent<UIButtonSound>();
                }
            }
        }
    }
}
