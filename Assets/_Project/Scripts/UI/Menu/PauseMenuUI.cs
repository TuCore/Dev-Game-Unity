using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnhThoDien.UI.Menu
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject pausePanel;

        [Header("Buttons")]
        public Button btnSave;
        public Button btnQuit;
        public Button btnResume;

        [Header("Settings Sliders")]
        public Slider sliderVolume;
        public Slider sliderSensitivity;
        public Slider sliderBrightness;

        [Header("Brightness Overlay")]
        public Image brightnessOverlay;

        private bool _isPaused = false;
        private Transform _playerTransform;
        private PlayerCamera _playerCamera;

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);

            if (btnSave != null) btnSave.onClick.AddListener(OnSaveClicked);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);
            if (btnResume != null) btnResume.onClick.AddListener(ResumeGame);

            var player = GameObject.Find("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerCamera = player.GetComponentInChildren<PlayerCamera>();
            }

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
                OnBrightnessChanged(sliderBrightness.value); // Apply immediately
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        public void PauseGame()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);

            // Hiển thị và giải phóng con trỏ chuột
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);

            // Khóa con trỏ chuột lại khi chơi
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnSaveClicked()
        {
            if (_playerTransform != null)
            {
                SaveSystem.SavePlayerPosition(_playerTransform.position);
                Debug.Log("Game Saved via Pause Menu!");
            }
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        private void OnSensitivityChanged(float value)
        {
            if (_playerCamera != null)
            {
                _playerCamera.SetSensitivity(value);
            }
            PlayerPrefs.SetFloat("MouseSensitivity", value);
        }

        private void OnBrightnessChanged(float value)
        {
            if (brightnessOverlay != null)
            {
                // value = 1 (sáng nhất) -> alpha = 0
                // value = 0 (tối nhất) -> alpha = 0.8
                float alpha = Mathf.Lerp(0.8f, 0f, value);
                brightnessOverlay.color = new Color(0, 0, 0, alpha);
            }
            PlayerPrefs.SetFloat("GameBrightness", value);
        }
    }
}
