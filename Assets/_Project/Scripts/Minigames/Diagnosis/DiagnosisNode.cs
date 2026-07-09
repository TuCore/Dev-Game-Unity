using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

namespace Minigames.Diagnosis
{
    public enum NodeState
    {
        Unprobed,
        ProbedNormal,
        ProbedWarning,
        ProbedFault
    }

    /// <summary>
    /// Gắn script này vào UI Button hoặc GameObject 3D (có Collider) đại diện cho điểm đo trên mạch.
    /// </summary>
    public class DiagnosisNode : MonoBehaviour, IPointerClickHandler
    {
        [Header("Node Info")]
        [Tooltip("ID của điểm đo (VD: Node_1) để đối chiếu với lỗi")]
        [SerializeField] private string nodeId;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color unprobedColor = new Color(1f, 1f, 1f, 0.7f); // Trắng đục
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color faultColor = Color.red;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI numberText;

        // Cho 2D UI
        private Image _nodeImage;
        private Button _nodeButton;
        
        // Cho 3D Object
        private MeshRenderer _meshRenderer;

        private NodeState _currentState = NodeState.Unprobed;
        private bool _hasFault = false;

        public string NodeId { get => nodeId; set => nodeId = value; }
        public NodeState CurrentState => _currentState;
        public bool HasFault => _hasFault;
        public int NodeNumber { get => _nodeNumber; }
        
        private int _nodeNumber;

        public event Action<DiagnosisNode> OnNodeProbed;

        private void Awake()
        {
            _nodeImage = GetComponent<Image>();
            _nodeButton = GetComponent<Button>();
            _meshRenderer = GetComponent<MeshRenderer>();
            
            // Nếu có UI Button thì lắng nghe sự kiện
            if (_nodeButton != null)
            {
                _nodeButton.onClick.AddListener(ProbeNode);
            }
        }

        public void Setup(string id, bool hasFault, int number)
        {
            this.nodeId = id;
            this._hasFault = hasFault;
            this._currentState = NodeState.Unprobed;
            this._nodeNumber = number;
            
            // Ép cứng lại màu trắng đục (bỏ qua màu vàng đã lưu trong Scene)
            this.unprobedColor = new Color(1f, 1f, 1f, 0.7f);
            
            if (numberText == null)
            {
                GameObject textObj = new GameObject("NumberText_AutoCreated");
                textObj.transform.SetParent(this.transform, false);
                numberText = textObj.AddComponent<TextMeshProUGUI>();
                numberText.alignment = TextAlignmentOptions.Center;
                numberText.fontSize = 32;
                numberText.color = Color.black;
                numberText.raycastTarget = false; // Tránh block click của Node
                
                RectTransform rt = textObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }
            }
            
            if (numberText != null)
            {
                numberText.text = number.ToString();
            }
            
            UpdateVisuals();
        }

        // Dành cho UI (nếu không dùng Button mà chỉ dùng EventSystem)
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_nodeButton == null) // Tránh gọi 2 lần nếu đã có Button hook
            {
                ProbeNode();
            }
        }

        // Dành cho 3D Object (Yêu cầu có Collider gắn trên object)
        private void OnMouseDown()
        {
            ProbeNode();
        }

        /// <summary>
        /// Xử lý logic khi người chơi click/chạm vào node.
        /// </summary>
        public void ProbeNode()
        {
            if (_currentState != NodeState.Unprobed) return;

            _currentState = _hasFault ? NodeState.ProbedFault : NodeState.ProbedNormal;
            UpdateVisuals();
            OnNodeProbed?.Invoke(this);
        }

        public void SetWarning()
        {
            if (_currentState == NodeState.ProbedNormal)
            {
                _currentState = NodeState.ProbedWarning;
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            Color targetColor = unprobedColor;
            switch (_currentState)
            {
                case NodeState.Unprobed:
                    targetColor = unprobedColor;
                    break;
                case NodeState.ProbedNormal:
                    targetColor = normalColor;
                    break;
                case NodeState.ProbedWarning:
                    targetColor = warningColor;
                    break;
                case NodeState.ProbedFault:
                    targetColor = faultColor;
                    break;
            }

            // Đổi màu cho 2D UI
            if (_nodeImage != null)
            {
                _nodeImage.color = targetColor;
            }
            
            // Đổi màu cho 3D Object
            if (_meshRenderer != null)
            {
                // Thay đổi màu của Material (Tạo ra bản sao của Material để không ảnh hưởng các object khác)
                _meshRenderer.material.color = targetColor;
            }
        }
    }
}
