using UnityEngine;
using UnityEngine.UI;

namespace AnhThoDien.UI.Menu
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Settings Sliders")]
        public Slider sliderVolume;
        public Slider sliderSensitivity;
        public Slider sliderBrightness;

        [Header("Controls")]
        public Button btnClose;

        private void Start()
        {
            // Load Settings
            if (sliderVolume != null)
            {
                sliderVolume.value = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
                sliderVolume.onValueChanged.AddListener(OnVolumeChanged);
                OnVolumeChanged(sliderVolume.value); // Apply immediately
            }

            if (sliderSensitivity != null)
            {
                sliderSensitivity.value = PlayerPrefs.GetFloat("MouseSensitivity", 100f);
                sliderSensitivity.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (sliderBrightness != null)
            {
                sliderBrightness.value = PlayerPrefs.GetFloat("GameBrightness", 1f);
                sliderBrightness.onValueChanged.AddListener(OnBrightnessChanged);
            }

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(CloseSettings);
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        private void OnSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", value);
        }

        private void OnBrightnessChanged(float value)
        {
            PlayerPrefs.SetFloat("GameBrightness", value);
        }

        private void CloseSettings()
        {
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }
    }
}
