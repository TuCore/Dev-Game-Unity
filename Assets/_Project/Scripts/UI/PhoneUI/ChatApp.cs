using UnityEngine;
using TMPro;

public class ChatApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI chatLogText;

    protected override void OnAppOpened()
    {
        if (chatLogText != null)
        {
            // Tạm thời mock data.
            chatLogText.text = "<color=#ff5555><b>[Chủ nợ]</b></color>\n" +
                               "Ê em trai, tới tháng rồi đấy!\n" +
                               "Lo mà kiếm tiền trả anh, không thì dọn đồ đi nhé!\n\n" +
                               "<color=#55ff55><b>[Bạn]</b></color>\n" +
                               "Dạ dạ anh cho em khất nốt hôm nay, em đang cày cuốc sửa đồ kiếm tiền đây ạ!";
        }
    }
}
