using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

        private string _waitingForKeybind = null;
        private Vector2 _scrollPos = Vector2.zero;
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
            // Ẩn toàn bộ UI "Tùy chỉnh" cũ đằng sau để OnGUI không bị đè lên
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            // Xóa component Image nền đằng sau của Unity UI
            if (TryGetComponent<Image>(out var img))
            {
                img.enabled = false;
            }

            // Cập nhật âm lượng ban đầu
            AudioListener.volume = PlayerPrefs.GetFloat("Vol_Master", 1f);
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
                        break;
                    }
                }
            }
        }

        private void OnGUI()
        {
            float width = 450;
            float height = Screen.height - 100;
            float startX = Screen.width / 2 - width / 2;
            float startY = 50;

            GUILayout.BeginArea(new Rect(startX, startY, width, height));
            GUI.Box(new Rect(0, 0, width, height), "");
            
            // Nút Cancel (Thoát) góc trên bên phải
            if (GUI.Button(new Rect(width - 40, 10, 30, 30), "X"))
            {
                CloseSettings();
            }

            // Vùng cuộn (Scroll View)
            GUILayout.BeginArea(new Rect(20, 40, width - 40, height - 60));
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            GUILayout.BeginVertical();
            
            GUILayout.Label("<b>CÀI ĐẶT BỔ SUNG (BETA)</b>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
            GUILayout.Space(10);

            // Master Volume
            GUILayout.Label("Âm lượng tổng: " + (PlayerPrefs.GetFloat("Vol_Master", 1f) * 100f).ToString("F0") + "%");
            float newV = GUILayout.HorizontalSlider(PlayerPrefs.GetFloat("Vol_Master", 1f), 0f, 1f);
            if (Mathf.Abs(newV - PlayerPrefs.GetFloat("Vol_Master", 1f)) > 0.01f)
            {
                PlayerPrefs.SetFloat("Vol_Master", newV);
                AudioListener.volume = newV;
            }
            GUILayout.Space(10);

            // Brightness
            GUILayout.Label("Độ sáng (Brightness): " + PlayerPrefs.GetFloat("Brightness", 1f).ToString("F1"));
            float newB = GUILayout.HorizontalSlider(PlayerPrefs.GetFloat("Brightness", 1f), 0f, 1f);
            if (Mathf.Abs(newB - PlayerPrefs.GetFloat("Brightness", 1f)) > 0.01f)
            {
                PlayerPrefs.SetFloat("Brightness", newB);
                BrightnessManager.Instance?.UpdateBrightness();
            }
            GUILayout.Space(10);

            // Sensitivity
            GUILayout.Label("Độ nhạy chuột: " + PlayerPrefs.GetFloat("MouseSensitivity", 100f).ToString("F0"));
            float newS = GUILayout.HorizontalSlider(PlayerPrefs.GetFloat("MouseSensitivity", 100f), 10f, 300f);
            if (Mathf.Abs(newS - PlayerPrefs.GetFloat("MouseSensitivity", 100f)) > 1f)
            {
                PlayerPrefs.SetFloat("MouseSensitivity", newS);
            }

            GUILayout.Space(20);
            GUILayout.Label("<b>ĐỔI PHÍM ĐIỀU KHIỂN</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(5);
            
            var keys = CustomInputManager.GetAllKeys();
            foreach (var kvp in keys)
            {
                if (!_displayNames.ContainsKey(kvp.Key)) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(_displayNames[kvp.Key], GUILayout.Width(150));
                
                string btnText = _waitingForKeybind == kvp.Key ? "[ Bấm phím mới... ]" : kvp.Value.ToString();
                if (GUILayout.Button(btnText))
                {
                    _waitingForKeybind = kvp.Key;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.EndArea();
        }

        private void CloseSettings()
        {
            _waitingForKeybind = null;
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }
    }
}
