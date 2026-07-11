using UnityEngine;
using UnityEngine.UI;

namespace AnhThoDien.UI.Menu
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Settings")]
        public Slider sliderMusic;
        public Slider sliderAmbience;
        public Slider sliderSFX;

        [Header("Controls")]
        public Button btnClose;

        private void Start()
        {
            // Load saved settings if any
            if (sliderMusic != null)
            {
                sliderMusic.value = PlayerPrefs.GetFloat("Vol_Music", 1f);
                sliderMusic.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sliderAmbience != null)
            {
                sliderAmbience.value = PlayerPrefs.GetFloat("Vol_Ambience", 1f);
                sliderAmbience.onValueChanged.AddListener(OnAmbienceVolumeChanged);
            }

            if (sliderSFX != null)
            {
                sliderSFX.value = PlayerPrefs.GetFloat("Vol_SFX", 1f);
                sliderSFX.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(CloseSettings);
            }
        }

        private void OnMusicVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("Vol_Music", val);
            // TODO: Hook to AudioManager
        }

        private void OnAmbienceVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("Vol_Ambience", val);
            // TODO: Hook to AudioManager
        }

        private void OnSFXVolumeChanged(float val)
        {
            PlayerPrefs.SetFloat("Vol_SFX", val);
            // TODO: Hook to AudioManager
        }

        private void CloseSettings()
        {
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }
    }
}
