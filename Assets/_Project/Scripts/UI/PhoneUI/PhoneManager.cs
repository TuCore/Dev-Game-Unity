    using UnityEngine;
    using System.Collections.Generic;

    public class PhoneManager : MonoBehaviour
    {
        public static PhoneManager Instance { get; private set; }

        public bool IsPhoneOpen { get; private set; }

        [Header("UI References")]
        public GameObject phoneContainer;
        public GameObject homeScreen;
        public Transform appsContainer;

        private BaseApp _currentApp;

        private Vector2 _hiddenPos = new Vector2(0, -1200);
        private Vector2 _visiblePos = new Vector2(0, 0);
        private bool _isAnimating = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Nhấn Tab để Bật/Tắt điện thoại
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // Chặn mở điện thoại nếu đang chơi minigame
                MinigameManager mg = FindAnyObjectByType<MinigameManager>();
                if (mg != null && mg.IsMinigameActive && !IsPhoneOpen) return;

                TogglePhone(!IsPhoneOpen);
            }

            if (_isAnimating && phoneContainer != null)
            {
                RectTransform rect = phoneContainer.GetComponent<RectTransform>();
                Vector2 target = IsPhoneOpen ? _visiblePos : _hiddenPos;
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, Time.deltaTime * 15f);
                
                if (Vector2.Distance(rect.anchoredPosition, target) < 5f)
                {
                    rect.anchoredPosition = target;
                    _isAnimating = false;
                    if (!IsPhoneOpen) phoneContainer.SetActive(false);
                }
            }
        }

        public void TogglePhone(bool open)
        {
            IsPhoneOpen = open;
            _isAnimating = true;
            
            if (phoneContainer != null)
            {
                if (IsPhoneOpen)
                {
                    phoneContainer.SetActive(true);
                    RectTransform rect = phoneContainer.GetComponent<RectTransform>();
                    if (rect.anchoredPosition.y > -100) rect.anchoredPosition = _hiddenPos; // Reset vị trí trước khi trượt lên
                }
            }

            if (IsPhoneOpen)
            {
                // Giải phóng chuột để tương tác UI
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ShowHomeScreen();
            }
            else
            {
                // Khoá chuột lại để chơi game
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                CloseCurrentApp();
            }
        }

        public void OpenApp(BaseApp app)
        {
            if (homeScreen != null) homeScreen.SetActive(false);
            if (_currentApp != null) _currentApp.CloseApp();
            
            _currentApp = app;
            _currentApp.OpenApp();
        }

        public void CloseCurrentApp()
        {
            if (_currentApp != null)
            {
                _currentApp.CloseApp();
                _currentApp = null;
            }
        }

        public void ShowHomeScreen()
        {
            CloseCurrentApp();
            if (homeScreen != null) homeScreen.SetActive(true);
        }
    }
