using UnityEngine;
using UnityEngine.UI;

namespace AnhThoDien.UI.HUD
{
    /// <summary>
    /// Hiển thị tâm ngắm (chấm trắng) ở giữa màn hình cho chế độ góc nhìn thứ nhất (FPP).
    /// Thuộc về Hệ thống HUD (Người 2 phụ trách).
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        [SerializeField] private float dotSize = 6f;
        [SerializeField] private Color dotColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private float targetDotSize = 11f;
        [SerializeField] private Color targetDotColor = new Color(0.25f, 1f, 0.55f, 1f);
        [SerializeField] private bool createOnAwake = true;

        private Image _crosshairImage;
        private RectTransform _crosshairRect;

        private void Awake()
        {
            if (createOnAwake)
            {
                CreateCrosshair();
            }
        }

        /// <summary>
        /// Tạo procedural sprite chấm tròn trắng giữa màn hình mà không cần gán hình ảnh thủ công.
        /// Tránh được việc thiếu asset hay conflict meta file.
        /// </summary>
        public void CreateCrosshair()
        {
            if (_crosshairImage != null) return;

            // Tìm hoặc thêm Canvas nếu script được gắn vào một empty GameObject
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Đảm bảo crosshair luôn nổi lên trên
                
                if (GetComponent<CanvasScaler>() == null)
                    gameObject.AddComponent<CanvasScaler>();
                if (GetComponent<GraphicRaycaster>() == null)
                    gameObject.AddComponent<GraphicRaycaster>();
            }

            // Tạo GameObject con chứa dấu chấm
            GameObject dotObj = new GameObject("CrosshairDot", typeof(RectTransform));
            dotObj.transform.SetParent(this.transform, false);

            RectTransform rect = dotObj.GetComponent<RectTransform>();
            _crosshairRect = rect;
            // Căn giữa tuyệt đối
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(dotSize, dotSize);

            // Thêm Image và gán sprite procedural tròn
            _crosshairImage = dotObj.AddComponent<Image>();
            _crosshairImage.sprite = CreateCircleSprite(32); // Kích thước texture 32x32 đủ sắc nét
            _crosshairImage.color = dotColor;
            _crosshairImage.raycastTarget = false; // Không block click chuột
        }

        /// <summary>
        /// Ẩn/Hiện tâm ngắm (ví dụ khi vào Minigame thì ẩn đi, ra FPP thì hiện lại)
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (_crosshairImage != null)
            {
                _crosshairImage.enabled = isVisible;
            }
        }

        // Khử răng cưa và vẽ một hình tròn mềm mại bằng code
        public void SetTargeting(bool isTargeting)
        {
            if (_crosshairImage == null)
            {
                CreateCrosshair();
            }

            if (_crosshairImage != null)
            {
                _crosshairImage.color = isTargeting ? targetDotColor : dotColor;
            }

            if (_crosshairRect != null)
            {
                float size = isTargeting ? targetDotSize : dotSize;
                _crosshairRect.sizeDelta = new Vector2(size, size);
            }
        }

        private Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            
            float radius = size / 2f;
            float center = size / 2f - 0.5f;
            Color clear = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    // Alpha blending mịn ở rìa
                    float alpha = Mathf.Clamp01(radius - dist);
                    texture.SetPixel(x, y, alpha > 0 ? new Color(1f, 1f, 1f, alpha) : clear);
                }
            }
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
