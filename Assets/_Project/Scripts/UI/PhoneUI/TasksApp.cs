using UnityEngine;
using TMPro;

public class TasksApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI tasksText;

    protected override void OnAppOpened()
    {
        if (tasksText != null)
        {
            // Tạm thời mock data. Sau này có thể lấy từ TaskManager.
            tasksText.text = "- [x] Mở tiệm sửa chữa\n" +
                             "- [ ] Sửa 3 món đồ hỏng\n" +
                             "- [ ] Đóng tiền trọ trước 18:00\n" +
                             "- [ ] Săn đồ ve chai";
        }
    }
}
