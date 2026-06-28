using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Random hóa lỗi từ fault pool của từng RepairItemData.
/// Tách biệt khỏi MinigameManager để tái sử dụng cho cả vòng Ve chai.
/// Đảm bảo không có 2 lần sửa giống nhau (GDD mục 2.1).
/// </summary>
public class FaultRandomizer : MonoBehaviour
{
    [Header("Cấu hình")]
    [SerializeField] private int minFaultsPerRepair = 1;
    [SerializeField] private int maxFaultsPerRepair = 3;

    /// <summary>
    /// Chọn ngẫu nhiên một hoặc nhiều lỗi từ fault pool của món đồ.
    /// </summary>
    /// <param name="faultPool">Danh sách tất cả lỗi khả dĩ của món đồ.</param>
    /// <param name="difficultyLevel">Độ khó ảnh hưởng đến số lượng lỗi.</param>
    /// <returns>Danh sách lỗi được chọn cho lượt sửa này.</returns>
    public List<string> RandomizeFaults(List<string> faultPool, int difficultyLevel = 1)
    {
        if (faultPool == null || faultPool.Count == 0)
        {
            Debug.LogWarning("[FaultRandomizer] Fault pool rỗng!");
            return new List<string>();
        }

        // Số lượng lỗi tăng theo độ khó
        int faultCount = Mathf.Clamp(
            Random.Range(minFaultsPerRepair, maxFaultsPerRepair + 1) + (difficultyLevel - 1),
            1,
            faultPool.Count
        );

        // Shuffle và chọn
        List<string> shuffled = new List<string>(faultPool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled.GetRange(0, faultCount);
    }
}
