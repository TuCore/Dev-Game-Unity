using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý thợ phụ NPC — tự động xử lý đơn đồ dễ.
/// Chạy phiên bản giản lược của MinigameManager dựa trên xác suất thành công,
/// không cần input người chơi (GDD mục 2.6, 10).
/// </summary>
public class HiredWorkerSystem : MonoBehaviour
{
    [System.Serializable]
    public class HiredWorker
    {
        public string workerName;
        [Range(0f, 1f)]
        public float skillLevel = 0.5f;        // Xác suất thành công (0-1)
        public float salary;                     // Lương hàng ngày
        public int maxOrdersPerDay = 2;          // Số đơn tối đa/ngày
        public int ordersCompletedToday = 0;
    }

    [Header("Danh sách thợ phụ")]
    [SerializeField] private List<HiredWorker> workers = new List<HiredWorker>();

    [Header("Cấu hình")]
    [SerializeField] private int maxWorkers = 3;  // Số thợ phụ tối đa

    public int WorkerCount => workers.Count;
    public int MaxWorkers => maxWorkers;
    public List<HiredWorker> Workers => new List<HiredWorker>(workers);

    // Events
    public System.Action<HiredWorker> OnWorkerHired;
    public System.Action<HiredWorker, RepairQuality> OnWorkerCompletedOrder;

    /// <summary>
    /// Thuê thợ phụ mới.
    /// </summary>
    public bool HireWorker(string name, float skill, float salary, EconomyManager economy)
    {
        if (workers.Count >= maxWorkers) return false;

        var worker = new HiredWorker
        {
            workerName = name,
            skillLevel = skill,
            salary = salary
        };

        workers.Add(worker);
        OnWorkerHired?.Invoke(worker);
        return true;
    }

    /// <summary>
    /// Thợ phụ tự động xử lý 1 đơn hàng — kết quả dựa trên xác suất skill.
    /// </summary>
    public RepairQuality WorkerProcessOrder(HiredWorker worker)
    {
        if (worker.ordersCompletedToday >= worker.maxOrdersPerDay)
            return RepairQuality.Broken;

        float roll = Random.value;
        RepairQuality result;

        if (roll < worker.skillLevel * 0.3f)
            result = RepairQuality.Perfect;
        else if (roll < worker.skillLevel * 0.7f)
            result = RepairQuality.Good;
        else if (roll < worker.skillLevel)
            result = RepairQuality.Passable;
        else
            result = RepairQuality.Broken;

        worker.ordersCompletedToday++;
        OnWorkerCompletedOrder?.Invoke(worker, result);
        return result;
    }

    /// <summary>
    /// Trả lương toàn bộ thợ phụ cuối ngày.
    /// </summary>
    public void PayDailySalaries(EconomyManager economy)
    {
        foreach (var worker in workers)
        {
            economy.SpendCash(worker.salary);
            worker.ordersCompletedToday = 0; // Reset đơn hàng ngày mới
        }
    }
}
