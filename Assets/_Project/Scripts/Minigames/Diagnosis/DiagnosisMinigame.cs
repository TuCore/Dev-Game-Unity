using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames.Diagnosis
{
    public class DiagnosisMinigame : MonoBehaviour, IMinigame
    {
        public string MinigameName => "Khám Bệnh (Diagnosis)";
        public bool IsActive { get; private set; }

        public event Action<RepairQuality> OnMinigameCompleted;
        
        // Output API: Trả về kết quả đánh giá (Perfect, Good, Passable, Broken) khi hoàn thành minigame
        public event Action<RepairQuality> OnMinigameFinished;

        [Header("UI Reference")]
        [Tooltip("Kéo GameObject chứa nguyên cái bảng mạch 2D (Panel/Canvas) vào đây để tự bật/tắt")]
        [SerializeField] private GameObject minigameBoardUI;

        [Header("Configuration")]
        [Tooltip("Danh sách các UI Button (DiagnosisNode) trên bảng mạch")]
        [SerializeField] private List<DiagnosisNode> allNodes = new List<DiagnosisNode>();
        
        [Tooltip("Thời gian tối đa để nhận đánh giá Hoàn Hảo (giây)")]
        [SerializeField] private float perfectTimeLimit = 30f;

        [Tooltip("Số lần đo tối đa cho phép. Nhập 0 hoặc số âm nếu không giới hạn.")]
        [SerializeField] private int maxProbesAllowed = 5;

        private int _totalFaultsToFind = 0;
        private int _faultsFound = 0;
        private float _timeElapsed = 0f;
        private int _currentProbes = 0;

        private void Awake()
        {
            // Đảm bảo tắt UI board khi mới bắt đầu game
            if (minigameBoardUI != null)
                minigameBoardUI.SetActive(false);
        }

        public void Initialize(List<string> faults, int difficultyLevel)
        {
            _totalFaultsToFind = faults.Count;
            _faultsFound = 0;
            _timeElapsed = 0f;
            _currentProbes = 0;

            // Thiết lập trạng thái cho tất cả các node dựa trên danh sách lỗi
            foreach (var node in allNodes)
            {
                bool hasFault = faults.Contains(node.NodeId);
                node.Setup(node.NodeId, hasFault);
                
                node.OnNodeProbed -= HandleNodeProbed; 
                node.OnNodeProbed += HandleNodeProbed;

                // Mở khóa lại tất cả các nút khi bắt đầu ván mới
                var btn = node.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.interactable = true;
            }
            
            Debug.Log($"[Diagnosis] Đã khởi tạo minigame 2D. Tổng số lỗi ẩn: {_totalFaultsToFind}");
        }

        public void StartMinigame()
        {
            IsActive = true;
            
            // Hiện UI minigame lên màn hình
            if (minigameBoardUI != null)
                minigameBoardUI.SetActive(true); 

            Debug.Log("[Diagnosis] Bắt đầu minigame!");
        }

        private void Update()
        {
            if (IsActive)
            {
                _timeElapsed += Time.deltaTime;
            }
        }

        private void HandleNodeProbed(DiagnosisNode probedNode)
        {
            if (!IsActive) return;

            _currentProbes++;

            if (probedNode.HasFault)
            {
                _faultsFound++;
                Debug.Log($"[Diagnosis] Tìm thấy lỗi tại {probedNode.NodeId}! ({_faultsFound}/{_totalFaultsToFind})");
            }
            else
            {
                // Thuật toán Dò mìn theo Khoảng cách (Radar)
                // Vì các nút giờ nằm rải rác ngẫu nhiên, ta sẽ cảnh báo nếu có bất kỳ lỗi nào nằm trong bán kính 280px (trên màn hình)
                bool faultNearby = false;
                foreach (var other in allNodes)
                {
                    if (other != probedNode && other.HasFault)
                    {
                        float dist = Vector3.Distance(probedNode.transform.position, other.transform.position);
                        // Do Canvas scale, khoảng cách thực tế sẽ nhỏ hơn sizeDelta, nhưng transform.position là world space.
                        // Ta có thể dùng anchoredPosition nếu cả 2 có RectTransform, nhưng transform.localPosition cũng là 1 cách tốt.
                        float localDist = Vector3.Distance(probedNode.transform.localPosition, other.transform.localPosition);
                        
                        if (localDist <= 280f) // 280 units (pixels) là bán kính radar
                        {
                            faultNearby = true;
                            break;
                        }
                    }
                }

                if (faultNearby)
                {
                    probedNode.SetWarning();
                    Debug.Log($"[Diagnosis] Node {probedNode.NodeId} an toàn, nhưng Radar phát hiện LỖI Ở GẦN ĐÓ (Bán kính 280)!");
                }
            }

            // Nếu đạt giới hạn số lần đo, khóa toàn bộ các Node lại
            if (maxProbesAllowed > 0 && _currentProbes >= maxProbesAllowed)
            {
                Debug.Log("<color=orange>[Diagnosis] Đã hết lượt đo! Khóa bảng mạch.</color>");
                LockAllNodes();
            }
        }

        private void LockAllNodes()
        {
            foreach (var node in allNodes)
            {
                if (node != null)
                {
                    var btn = node.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null) btn.interactable = false;
                }
            }
        }

        /// <summary>
        /// Gọi hàm này khi người chơi chủ động bấm nút "Kết thúc khám" trên UI.
        /// </summary>
        public void FinishDiagnosis()
        {
            if (!IsActive) return;

            IsActive = false;

            RepairQuality quality = EvaluateQuality();

            // Kích hoạt Output API
            OnMinigameFinished?.Invoke(quality);

            // Ẩn bảng mạch đi
            if (minigameBoardUI != null)
                minigameBoardUI.SetActive(false);
            
            CleanupNodes();
            
            Debug.Log($"[Diagnosis] Kết thúc khám. Đánh giá: {quality}. Thời gian: {_timeElapsed:F1}s");
            OnMinigameCompleted?.Invoke(quality);
        }

        public RepairQuality EndMinigame()
        {
            if (!IsActive) return RepairQuality.Broken;

            IsActive = false;
            
            // Ẩn UI sau khi chơi xong
            if (minigameBoardUI != null)
                minigameBoardUI.SetActive(false);

            RepairQuality quality = EvaluateQuality();
            
            CleanupNodes();

            Debug.Log($"[Diagnosis] Kết thúc khám. Đánh giá: {quality}. Thời gian: {_timeElapsed:F1}s");
            OnMinigameCompleted?.Invoke(quality);

            return quality;
        }

        public void AbortMinigame()
        {
            IsActive = false;
            
            if (minigameBoardUI != null)
                minigameBoardUI.SetActive(false);

            CleanupNodes();

            Debug.Log("[Diagnosis] Minigame bị hủy (Abort).");
            OnMinigameCompleted?.Invoke(RepairQuality.Broken);
        }

        private void CleanupNodes()
        {
            foreach (var node in allNodes)
            {
                if (node != null)
                {
                    node.OnNodeProbed -= HandleNodeProbed;
                }
            }
        }

        private RepairQuality EvaluateQuality()
        {
            if (_faultsFound == 0 && _totalFaultsToFind > 0)
                return RepairQuality.Broken; // Hỏng

            if (_faultsFound == _totalFaultsToFind)
            {
                if (_timeElapsed <= perfectTimeLimit)
                    return RepairQuality.Perfect; // Hoàn hảo
                else
                    return RepairQuality.Good; // Tốt (chậm mà chắc)
            }

            return RepairQuality.Passable; // Tạm
        }
    }
}
