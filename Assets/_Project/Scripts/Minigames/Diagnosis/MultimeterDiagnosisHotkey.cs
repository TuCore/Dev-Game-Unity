using System.Collections.Generic;
using UnityEngine;

namespace Minigames.Diagnosis
{
    public class MultimeterDiagnosisHotkey : MonoBehaviour
    {
        [SerializeField] private KeyCode playKey = KeyCode.Slash;
        [SerializeField] private KeyCode alternatePlayKey = KeyCode.KeypadDivide;
        [SerializeField, Range(1, 5)] private int testDifficulty = 2;

        private MultimeterDiagnosisMinigame _minigame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<MultimeterDiagnosisHotkey>() != null)
            {
                return;
            }

            GameObject hotkeyObject = new GameObject("MultimeterDiagnosisHotkey");
            DontDestroyOnLoad(hotkeyObject);
            hotkeyObject.AddComponent<MultimeterDiagnosisHotkey>();
            Debug.Log("[MultimeterDiagnosisHotkey] Installed. Press / to open the multimeter diagnosis minigame.");
        }

        private void Update()
        {
            if (!WasPlayKeyPressed())
            {
                return;
            }

            TryStartTestMinigame();
        }

        private bool WasPlayKeyPressed()
        {
            if (Input.GetKeyDown(playKey) || Input.GetKeyDown(alternatePlayKey))
            {
                return true;
            }

            string typed = Input.inputString;
            return !string.IsNullOrEmpty(typed) && typed.Contains("/");
        }

        private void TryStartTestMinigame()
        {
            Debug.Log("[MultimeterDiagnosisHotkey] Start requested.");

            MinigameManager manager = FindAnyObjectByType<MinigameManager>();
            if (manager != null && manager.IsMinigameActive)
            {
                ShowToast("Đang có minigame khác chạy rồi.");
                return;
            }

            EnsureMinigame();
            if (_minigame == null)
            {
                ShowToast("Không tạo được minigame đồng hồ đo.");
                return;
            }

            if (_minigame.IsActive)
            {
                ShowToast("Minigame đồng hồ đo đang mở.");
                return;
            }

            List<string> debugFaults = new List<string>
            {
                "Cầu chì nguồn",
                "Cháy tụ",
                "Hỏng IC",
                "Đứt dây",
                "Diode chập"
            };

            _minigame.OnMinigameCompleted -= HandleMinigameCompleted;
            _minigame.OnMinigameCompleted += HandleMinigameCompleted;
            _minigame.Initialize(debugFaults, testDifficulty);
            _minigame.StartMinigame();
        }

        private void EnsureMinigame()
        {
            if (_minigame != null)
            {
                return;
            }

            _minigame = FindAnyObjectByType<MultimeterDiagnosisMinigame>();
            if (_minigame != null)
            {
                return;
            }

            GameObject minigameObject = new GameObject("MultimeterDiagnosisMinigame_DebugRoot");
            DontDestroyOnLoad(minigameObject);
            _minigame = minigameObject.AddComponent<MultimeterDiagnosisMinigame>();
        }

        private void HandleMinigameCompleted(RepairQuality quality)
        {
            if (_minigame != null)
            {
                _minigame.OnMinigameCompleted -= HandleMinigameCompleted;
            }

            ShowToast("Kết quả dò lỗi: " + quality);
        }

        private void ShowToast(string message)
        {
            if (ToastNotificationManager.Instance != null)
            {
                ToastNotificationManager.Instance.ShowToast(message, 2.5f);
            }
            else
            {
                Debug.Log("[MultimeterDiagnosisHotkey] " + message);
            }
        }
    }
}
