using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace AnhThoDien.UI
{
    public class TutorialUI : MonoBehaviour
    {
        public GameObject tutorialPanel;
        public Button btnGotIt;
        
        private bool isShowing = false;
        
        private void Start()
        {
            if (btnGotIt != null)
            {
                btnGotIt.onClick.AddListener(OnGotItClicked);
            }
            
            // PlayerPrefs "IsNewGame" is set to 1 when clicking "Bắt Đầu" in Main Menu.
            // "TutorialShown" prevents it from showing again if we reload the scene in the same run.
            if (PlayerPrefs.GetInt("IsNewGame", 0) == 1 && PlayerPrefs.GetInt("TutorialShown", 0) == 0)
            {
                StartCoroutine(ShowTutorialRoutine());
            }
            else
            {
                if (tutorialPanel != null) tutorialPanel.SetActive(false);
            }
        }
        
        private IEnumerator ShowTutorialRoutine()
        {
            // Đợi 1 frame để đảm bảo PlayerCamera (hay các script khác) đã khóa chuột xong
            yield return new WaitForEndOfFrame();
            
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
                isShowing = true;
                
                // Freeze game
                Time.timeScale = 0f;
                
                // Unlock cursor so user can click the button
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                // Mark as shown so it doesn't appear again on reload unless New Game
                PlayerPrefs.SetInt("TutorialShown", 1);
                PlayerPrefs.Save();
            }
        }

        private void Update()
        {
            // Đảm bảo chuột luôn mở khi đang hiện bảng hướng dẫn
            if (isShowing)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void OnGotItClicked()
        {
            isShowing = false;
            
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
            
            // Unfreeze game
            Time.timeScale = 1f;
            
            // Lock cursor back for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
