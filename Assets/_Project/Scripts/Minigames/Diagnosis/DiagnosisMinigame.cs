using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

        [Tooltip("Text để hiển thị gợi ý thoại tìm lỗi")]
        [SerializeField] private TextMeshProUGUI hintText;

        [Header("Hint Dialogues")]
        [SerializeField] private string[] hintTemplates = {
            "- Khách hàng báo có mùi khét quanh khu vực số {0}.",
            "- Có vẻ đoản mạch ở gần vị trí số {0}.",
            "- Có tiếng xẹt điện khi cắm nguồn gần số {0}.",
            "- Cảm biến báo nhiệt độ cao quanh node {0}."
        };

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
        private bool _usedHint = false;
        private bool _isPlaying = false;

        private void Awake()
        {
            // Đảm bảo tắt UI board khi mới bắt đầu game
            if (minigameBoardUI != null)
                minigameBoardUI.SetActive(false);
        }

        public void Initialize(List<string> faults, int difficultyLevel)
        {
            // Tự động gán ngẫu nhiên nếu danh sách lỗi từ hệ thống truyền vào (ví dụ "Đứt dây", "Cháy tụ") không khớp với tên Node trên mạch (Node_1, Node_2,...)
            bool hasValidNodeIds = false;
            foreach (var f in faults)
            {
                if (f.StartsWith("Node_")) { hasValidNodeIds = true; break; }
            }

            List<string> actualFaults = new List<string>(faults);
            if (!hasValidNodeIds && allNodes.Count > 0)
            {
                actualFaults.Clear();
                int faultCount = Mathf.Clamp(faults.Count > 0 ? faults.Count : difficultyLevel, 1, allNodes.Count);
                List<DiagnosisNode> availableNodes = new List<DiagnosisNode>(allNodes);
                
                for (int i = 0; i < faultCount; i++)
                {
                    if (availableNodes.Count == 0) break;
                    int randomIndex = UnityEngine.Random.Range(0, availableNodes.Count);
                    actualFaults.Add(availableNodes[randomIndex].NodeId);
                    availableNodes.RemoveAt(randomIndex);
                }
                Debug.Log($"<color=cyan>[Diagnosis] Đã tự động quy đổi {faults.Count} lỗi thành {actualFaults.Count} Node bị hỏng ngẫu nhiên trên bo mạch.</color>");
            }

            _totalFaultsToFind = actualFaults.Count;
            _faultsFound = 0;
            _timeElapsed = 0f;
            _currentProbes = 0;
            _usedHint = false;
            _isPlaying = false;

            int nodeNumber = 1;
            List<int> faultyNodeNumbers = new List<int>();

            RectTransform boardRect = minigameBoardUI != null ? minigameBoardUI.GetComponent<RectTransform>() : null;
            float padding = 80f;
            float minDistance = 120f;
            List<Vector2> usedPositions = new List<Vector2>();

            // Thiết lập trạng thái cho tất cả các node dựa trên danh sách lỗi
            foreach (var node in allNodes)
            {
                // Sắp xếp vị trí ngẫu nhiên
                if (boardRect != null)
                {
                    RectTransform nodeRect = node.GetComponent<RectTransform>();
                    if (nodeRect != null)
                    {
                        float boardWidth = boardRect.rect.width;
                        float boardHeight = boardRect.rect.height;
                        
                        // Đề phòng trường hợp width/height = 0 (khi Canvas chưa update), ta fallback
                        if (boardWidth == 0) boardWidth = 800f;
                        if (boardHeight == 0) boardHeight = 600f;
                        
                        float minX = -boardWidth / 2f + padding;
                        float maxX = boardWidth / 2f - padding;
                        float minY = -boardHeight / 2f + padding;
                        float maxY = boardHeight / 2f - padding;
                        
                        Vector2 randomPos = Vector2.zero;
                        bool validPositionFound = false;
                        int maxAttempts = 100;

                        for (int i = 0; i < maxAttempts; i++)
                        {
                            randomPos = new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));
                            
                            bool tooClose = false;
                            foreach (Vector2 pos in usedPositions)
                            {
                                if (Vector2.Distance(randomPos, pos) < minDistance)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }
                            
                            if (!tooClose)
                            {
                                validPositionFound = true;
                                break;
                            }
                        }
                        
                        if (validPositionFound)
                        {
                            nodeRect.anchoredPosition = randomPos;
                            usedPositions.Add(randomPos);
                        }
                    }
                }

                bool hasFault = actualFaults.Contains(node.NodeId);
                node.Setup(node.NodeId, hasFault, nodeNumber);
                
                if (hasFault)
                {
                    faultyNodeNumbers.Add(nodeNumber);
                }
                
                node.OnNodeProbed -= HandleNodeProbed; 
                node.OnNodeProbed += HandleNodeProbed;

                // Mở khóa lại tất cả các nút khi bắt đầu ván mới
                var btn = node.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.interactable = true;
                
                nodeNumber++;
            }
            
            // Tạo Panel Gợi ý nếu chưa có
            if (hintText == null && minigameBoardUI != null)
            {
                // 1. Tạo Panel nền tối
                GameObject panelObj = new GameObject("HintPanel_AutoCreated");
                panelObj.transform.SetParent(minigameBoardUI.transform, false);
                UnityEngine.UI.Image panelBg = panelObj.AddComponent<UnityEngine.UI.Image>();
                panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Màu xanh đen tối
                
                RectTransform panelRt = panelObj.GetComponent<RectTransform>();
                if (panelRt != null)
                {
                    panelRt.anchorMin = new Vector2(0.2f, 0.15f);
                    panelRt.anchorMax = new Vector2(0.8f, 0.85f);
                    panelRt.sizeDelta = Vector2.zero;
                    panelRt.anchoredPosition = Vector2.zero;
                }

                // 2. Tạo Text Nội dung chung
                GameObject textObj = new GameObject("ContentText");
                textObj.transform.SetParent(panelObj.transform, false);
                hintText = textObj.AddComponent<TextMeshProUGUI>();
                hintText.alignment = TextAlignmentOptions.TopLeft;
                hintText.fontSize = 24;
                hintText.color = Color.white;
                
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.anchorMin = new Vector2(0.05f, 0.05f); // Cách lề 5%
                    textRt.anchorMax = new Vector2(0.95f, 0.95f);
                    textRt.sizeDelta = Vector2.zero;
                    textRt.anchoredPosition = Vector2.zero;
                }
            }

            // Tạo gợi ý ngẫu nhiên
            if (hintText != null)
            {
                if (faultyNodeNumbers.Count > 0)
                {
                    List<string> generatedHints = new List<string>();
                    
                    foreach (var faultyNode in allNodes)
                    {
                        if (faultyNode.HasFault)
                        {
                            // Tìm các node khỏe mạnh ở gần node lỗi
                            List<int> nearbyHealthyNodeNumbers = new List<int>();
                            foreach (var otherNode in allNodes)
                            {
                                if (!otherNode.HasFault)
                                {
                                    float localDist = Vector3.Distance(faultyNode.transform.localPosition, otherNode.transform.localPosition);
                                    if (localDist <= 280f) // Cùng khoảng cách với radar
                                    {
                                        nearbyHealthyNodeNumbers.Add(otherNode.NodeNumber);
                                    }
                                }
                            }
                            
                            if (nearbyHealthyNodeNumbers.Count > 0)
                            {
                                // Chọn ngẫu nhiên 1 node khỏe mạnh lân cận làm mồi nhử
                                int randomHealthyNeighbor = nearbyHealthyNodeNumbers[UnityEngine.Random.Range(0, nearbyHealthyNodeNumbers.Count)];
                                string randomTemplate = hintTemplates[UnityEngine.Random.Range(0, hintTemplates.Length)];
                                generatedHints.Add(string.Format(randomTemplate, randomHealthyNeighbor));
                            }
                            else
                            {
                                // Fallback nếu không có node nào ở gần
                                generatedHints.Add($"- Có vấn đề ở khu vực nào đó quanh node {faultyNode.NodeNumber}");
                            }
                        }
                    }
                    
                    // Tạo template nội dung Rich Text
                    string title = "<align=center><color=#FFD700><size=32><b>📋 SỔ TAY CHẨN ĐOÁN</b></size></color></align>\n\n";
                    string instructions = "<b>Cách chơi:</b>\n" +
                                          "• Click vào các node để kiểm tra.\n" +
                                          "• <color=#00FF00>Màu Xanh:</color> Bình thường.\n" +
                                          "• <color=#FF3333>Màu Đỏ:</color> Bắt đúng lỗi!\n" +
                                          "• <color=#FFFF33>Màu Vàng:</color> Radar báo có lỗi nằm rất gần đây.\n\n";
                    string hintsHeader = "<b>Gợi ý từ khách hàng:</b>\n";
                    
                    // Gộp các gợi ý lại
                    hintText.text = title + instructions + hintsHeader + string.Join("\n", generatedHints) + 
                                    "\n\n<align=center><size=20><color=#FF8888><i>(Lưu ý: Mở sổ tay sẽ bị trừ tiền thưởng!)</i></color>\n<i>(Nhấn phím Y để đóng/mở)</i></size></align>";
                    hintText.fontSize = 24;
                }
                else
                {
                    hintText.text = "Bo mạch này có vẻ hoạt động bình thường.";
                }
                
                // Mặc định ẩn Panel Gợi ý
                if (hintText.transform.parent != null && hintText.transform.parent.name == "HintPanel_AutoCreated")
                {
                    hintText.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    hintText.gameObject.SetActive(false);
                }
            }
            
            // Tạo Bảng Hướng Dẫn Đầu Game (Tutorial Panel)
            if (minigameBoardUI != null && minigameBoardUI.transform.Find("TutorialPanel_AutoCreated") == null)
            {
                GameObject tutPanelObj = new GameObject("TutorialPanel_AutoCreated");
                tutPanelObj.transform.SetParent(minigameBoardUI.transform, false);
                tutPanelObj.transform.SetAsLastSibling(); // Ensure it's on top
                UnityEngine.UI.Image tutBg = tutPanelObj.AddComponent<UnityEngine.UI.Image>();
                tutBg.color = new Color(0f, 0f, 0f, 0.95f); // Đen che toàn bộ
                
                RectTransform tutRt = tutPanelObj.GetComponent<RectTransform>();
                if (tutRt != null)
                {
                    tutRt.anchorMin = Vector2.zero;
                    tutRt.anchorMax = Vector2.one;
                    tutRt.sizeDelta = Vector2.zero;
                    tutRt.anchoredPosition = Vector2.zero;
                }

                GameObject tutTextObj = new GameObject("TutText");
                tutTextObj.transform.SetParent(tutPanelObj.transform, false);
                TextMeshProUGUI tutTextComp = tutTextObj.AddComponent<TextMeshProUGUI>();
                tutTextComp.alignment = TextAlignmentOptions.Center;
                tutTextComp.fontSize = 32;
                tutTextComp.color = Color.white;
                tutTextComp.text = "<color=#FFD700><size=45><b>HƯỚNG DẪN KHÁM BỆNH</b></size></color>\n\n" +
                               "<b>Cách chơi:</b>\n" +
                               "• Click vào các chân mạch (node) để kiểm tra lỗi.\n" +
                               "• <color=#00FF00>Màu Xanh:</color> Bình thường.\n" +
                               "• <color=#FF3333>Màu Đỏ:</color> Bắt đúng lỗi!\n" +
                               "• <color=#FFFF33>Màu Vàng:</color> Radar báo có lỗi nằm rất gần đây.\n\n" +
                               "<i><size=24>Mẹo: Nhấn phím Y khi đang chơi để mở Sổ tay gọi điện hỏi khách hàng.\nNhưng lưu ý dùng sổ tay sẽ làm giảm tiền thưởng!</size></i>";
                
                RectTransform tutTextRt = tutTextObj.GetComponent<RectTransform>();
                if (tutTextRt != null)
                {
                    tutTextRt.anchorMin = new Vector2(0.1f, 0.3f);
                    tutTextRt.anchorMax = new Vector2(0.9f, 0.9f);
                    tutTextRt.sizeDelta = Vector2.zero;
                    tutTextRt.anchoredPosition = Vector2.zero;
                }

                GameObject btnObj = new GameObject("StartButton");
                btnObj.transform.SetParent(tutPanelObj.transform, false);
                UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
                btnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f); // Xanh lá đậm
                UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
                
                RectTransform btnRt = btnObj.GetComponent<RectTransform>();
                if (btnRt != null)
                {
                    btnRt.anchorMin = new Vector2(0.35f, 0.1f);
                    btnRt.anchorMax = new Vector2(0.65f, 0.22f);
                    btnRt.sizeDelta = Vector2.zero;
                    btnRt.anchoredPosition = Vector2.zero;
                }

                GameObject btnTextObj = new GameObject("BtnText");
                btnTextObj.transform.SetParent(btnObj.transform, false);
                TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.fontSize = 36;
                btnText.color = Color.white;
                btnText.text = "<b>BẮT ĐẦU</b>";
                
                RectTransform btnTextRt = btnTextObj.GetComponent<RectTransform>();
                if (btnTextRt != null)
                {
                    btnTextRt.anchorMin = Vector2.zero;
                    btnTextRt.anchorMax = Vector2.one;
                    btnTextRt.sizeDelta = Vector2.zero;
                    btnTextRt.anchoredPosition = Vector2.zero;
                }

                // Thêm Event click
                btn.onClick.AddListener(() =>
                {
                    tutPanelObj.SetActive(false);
                    _isPlaying = true;
                });
                
                // Ẩn đi chờ StartMinigame
                tutPanelObj.SetActive(false);
            }
            
            Debug.Log($"[Diagnosis] Đã khởi tạo minigame 2D. Tổng số lỗi ẩn: {_totalFaultsToFind}");
        }

        public void StartMinigame()
        {
            IsActive = true;
            
            // Hiện UI minigame lên màn hình
            if (minigameBoardUI != null)
            {
                minigameBoardUI.SetActive(true); 
                Transform tutPanel = minigameBoardUI.transform.Find("TutorialPanel_AutoCreated");
                if (tutPanel != null)
                {
                    tutPanel.gameObject.SetActive(true);
                    tutPanel.SetAsLastSibling(); // Ensure it's on top of everything
                    _isPlaying = false;
                }
                else
                {
                    _isPlaying = true;
                }
            }

            // Mở khóa chuột để có thể bấm các button trên UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[Diagnosis] Bắt đầu minigame!");
        }

        private void Update()
        {
            if (IsActive && _isPlaying)
            {
                _timeElapsed += Time.deltaTime;
                
                // Nhấn Y để bật/tắt gợi ý
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    if (hintText != null)
                    {
                        if (hintText.transform.parent != null && hintText.transform.parent.name == "HintPanel_AutoCreated")
                        {
                            GameObject panel = hintText.transform.parent.gameObject;
                            panel.SetActive(!panel.activeSelf);
                            if (panel.activeSelf) _usedHint = true;
                        }
                        else
                        {
                            hintText.gameObject.SetActive(!hintText.gameObject.activeSelf);
                            if (hintText.gameObject.activeSelf) _usedHint = true;
                        }
                    }
                }
            }
        }

        private void HandleNodeProbed(DiagnosisNode probedNode)
        {
            if (!IsActive || !_isPlaying) return;

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
            
            // Khóa lại chuột khi tắt minigame
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
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

            // Khóa lại chuột
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

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

            // Khóa lại chuột
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

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
                if (_usedHint)
                    return RepairQuality.Passable; // Dùng hint -> bị trừ tiền thưởng

                if (_timeElapsed <= perfectTimeLimit)
                    return RepairQuality.Perfect; // Hoàn hảo
                else
                    return RepairQuality.Good; // Tốt (chậm mà chắc)
            }

            return RepairQuality.Passable; // Tạm
        }
    }
}
