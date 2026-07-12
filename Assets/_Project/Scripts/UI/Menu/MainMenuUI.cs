using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnhThoDien.UI.Menu
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Buttons")]
        public Button btnNewGame;
        public Button btnContinue;
        public Button btnSettings;
        public Button btnCollection;
        public Button btnInfo;
        public Button btnQuit;

        [Header("Panels")]
        public GameObject settingsPanel;

        private void Start()
        {
            // Bind buttons
            if (btnNewGame != null) btnNewGame.onClick.AddListener(OnNewGameClicked);
            if (btnContinue != null) btnContinue.onClick.AddListener(OnContinueClicked);
            if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
            if (btnCollection != null) btnCollection.onClick.AddListener(OnCollectionClicked);
            if (btnInfo != null) btnInfo.onClick.AddListener(OnInfoClicked);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);

            // Ensure settings panel is hidden at start
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnNewGameClicked()
        {
            Debug.Log("Starting New Game...");
            // TODO: Call SaveSystem to wipe or create new save
            SceneManager.LoadScene("VietnamStreet");
        }

        private void OnContinueClicked()
        {
            Debug.Log("Continuing Game...");
            // TODO: Call SaveSystem to load and check latest save
        }

        private void OnSettingsClicked()
        {
            Debug.Log("Opening Settings...");
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void OnCollectionClicked()
        {
            Debug.Log("Opening Collection...");
        }

        private void OnInfoClicked()
        {
            Debug.Log("Opening Info...");
        }

        private void OnQuitClicked()
        {
            Debug.Log("Quitting Game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
