using UnityEngine;
using System.Collections.Generic;

namespace Minigames.Diagnosis
{
    /// <summary>
    /// Script giả lập (Demo) để chứng minh Minigame có thể chạy độc lập.
    /// Đóng vai trò là hệ thống "Game chính" gọi vào Minigame.
    /// </summary>
    public class DemoMinigameRunner : MonoBehaviour
    {
        [SerializeField] private DiagnosisMinigame minigame;
        
        [Header("Cấu hình giả lập lỗi")]
        [Tooltip("Nếu bật, sẽ tự động chọn ngẫu nhiên các Node lỗi thay vì dùng danh sách cố định.")]
        public bool randomizeFaults = true;
        
        [Tooltip("Số lượng lỗi muốn sinh ra ngẫu nhiên (chỉ dùng khi Randomize Faults = true)")]
        public int randomFaultCount = 2;

        [Tooltip("Danh sách lỗi cố định (chỉ dùng khi Randomize Faults = false)")]
        public List<string> mockFaults = new List<string> { "Node_1", "Node_5" };

        private void Start()
        {
            if (minigame == null)
                minigame = GetComponent<DiagnosisMinigame>();

            // Lắng nghe kết quả trả về từ Minigame
            if (minigame != null)
            {
                minigame.OnMinigameFinished += HandleMinigameFinished;
                Debug.Log("<color=green>[Demo] Đã sẵn sàng! Bấm phím SPACE để gọi khách hàng giả và bắt đầu chơi.</color>");
            }
        }

        private void Update()
        {
            // Nhấn Space để gọi Minigame
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (minigame != null && !minigame.IsActive)
                {
                    Debug.Log("<color=yellow>[Demo] Khởi động Minigame với các lỗi giả lập...</color>");
                    
                    List<string> faultsToInject = mockFaults;
                    if (randomizeFaults)
                    {
                        faultsToInject = GenerateRandomFaults(randomFaultCount);
                    }
                    
                    // Khởi tạo minigame với danh sách lỗi
                    minigame.Initialize(faultsToInject, 2); 
                    
                    // Kích hoạt minigame
                    minigame.StartMinigame();
                }
            }
        }

        private List<string> GenerateRandomFaults(int count)
        {
            List<string> randomFaults = new List<string>();
            List<int> availableIndices = new List<int>();
            
            // Bo mạch hiện tại có 8 nodes
            for (int i = 1; i <= 8; i++)
            {
                availableIndices.Add(i);
            }

            // Xáo trộn mảng (Fisher-Yates) và lấy `count` phần tử
            for (int i = 0; i < count; i++)
            {
                if (availableIndices.Count == 0) break;
                int randomIndex = Random.Range(0, availableIndices.Count);
                randomFaults.Add($"Node_{availableIndices[randomIndex]}");
                availableIndices.RemoveAt(randomIndex);
            }

            Debug.Log($"<color=orange>[Demo] Đã random {count} lỗi tại: {string.Join(", ", randomFaults)}</color>");
            return randomFaults;
        }

        // Hàm này sẽ tự động chạy khi người chơi bấm nút "Hoàn Thành" trong Minigame
        private void HandleMinigameFinished(RepairQuality quality)
        {
            Debug.Log($"<color=cyan>[Demo] Nhận được kết quả từ Minigame: {quality}! Tiền công sẽ được tính toán ở đây.</color>");
            Debug.Log("<color=green>[Demo] Bạn có thể bấm SPACE để chơi ván mới.</color>");
        }

        private void OnDestroy()
        {
            // Tránh memory leak
            if (minigame != null)
            {
                minigame.OnMinigameFinished -= HandleMinigameFinished;
            }
        }
    }
}
