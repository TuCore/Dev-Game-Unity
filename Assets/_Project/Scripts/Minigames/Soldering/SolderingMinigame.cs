using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolderingMinigame : MonoBehaviour, IMinigame
{
    public string MinigameName => "Hàn Mạch";
    public bool IsActive => _isActive;

    [Header("References")]
    [SerializeField] private SolderingMinigameUI uiController;

    [Header("Skill Check Count")]
    [SerializeField, Min(1)] private int minJointsToSolder = 1;
    [SerializeField, Min(1)] private int maxJointsToSolder = 7;

    private bool _isActive;
    private int _difficultyLevel;
    private int _totalJointsToSolder;
    private int _currentJointIndex;

    // Theo dõi chất lượng từng cú bấm
    private int _perfectHits;
    private int _greatHits;
    private int _goodHits;
    private int _misses;

    public event System.Action<RepairQuality> OnMinigameCompleted;

    public void Initialize(List<string> faults, int difficultyLevel)
    {
        _difficultyLevel = difficultyLevel;
        // Số mối hàn cần thực hiện tỷ lệ thuận với số lượng lỗi
        _totalJointsToSolder = GetRandomJointCount();
        _currentJointIndex = 0;

        _perfectHits = 0;
        _greatHits = 0;
        _goodHits = 0;
        _misses = 0;
        
        if (uiController != null)
        {
            uiController.SetupMinigame(this, _difficultyLevel);
        }
    }

    private int GetRandomJointCount()
    {
        int minCount = Mathf.Max(1, minJointsToSolder);
        int maxCount = Mathf.Max(minCount, maxJointsToSolder);
        return Random.Range(minCount, maxCount + 1);
    }

    public void StartMinigame()
    {
        _isActive = true;
        if (uiController != null)
        {
            uiController.ShowUI(true);
            StartNextJointCheck();
        }
    }

    public void AbortMinigame()
    {
        _isActive = false;
        if (uiController != null)
        {
            uiController.ShowUI(false);
        }
    }

    public RepairQuality EndMinigame()
    {
        _isActive = false;
        if (uiController != null)
        {
            uiController.ShowUI(false);
        }
        // Tính toán chất lượng dựa trên tỉ lệ Perfect / Good / Miss
        if (_misses > _totalJointsToSolder * 0.5f) // Miss quá nửa
        {
            return RepairQuality.Broken;
        }
        else if (_perfectHits == _totalJointsToSolder) // Hoàn hảo tất cả
        {
            return RepairQuality.Perfect;
        }
        else if (_perfectHits + _greatHits + _goodHits >= _totalJointsToSolder) // Đạt đa số tốt/hoàn hảo
        {
            return RepairQuality.Good;
        }

        return RepairQuality.Passable;
    }

    // Kích hoạt Skill Check cho mối hàn tiếp theo.
    public void StartNextJointCheck()
    {
        if (_currentJointIndex < _totalJointsToSolder)
        {
            if (uiController != null)
            {
                uiController.TriggerSkillCheck(_currentJointIndex + 1, _totalJointsToSolder);
            }
        }
        else
        {
            // Đã hàn xong tất cả các điểm
            RepairQuality finalQuality = EndMinigame();
            OnMinigameCompleted?.Invoke(finalQuality);
        }
    }

    // Hàm callback nhận kết quả Skill Check từ UI gửi về.
    public void ReportSkillCheckResult(string hitType)
    {
        if (hitType == "Perfect")
        {
            _perfectHits++;
            _currentJointIndex++;
        }
        else if (hitType == "Great")
        {
            _greatHits++;
            _currentJointIndex++;
        }
        else if (hitType == "Good")
        {
            _goodHits++;
            _currentJointIndex++;
        }
        else // Miss
        {
            _misses++;
            // Tùy chọn: Cho phép thử lại mối hàn này hoặc bỏ qua tăng index
            _currentJointIndex++;
        }

        // Đợi một khoảng ngắn để người chơi thấy hiệu ứng trước khi qua điểm tiếp theo
        StartCoroutine(NextCheckDelay());
    }

    private IEnumerator NextCheckDelay()
    {
        yield return new WaitForSeconds(0.8f);
        StartNextJointCheck();
    }
}
