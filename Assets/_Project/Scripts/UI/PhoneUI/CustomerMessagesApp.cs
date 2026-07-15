using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomerMessagesApp : BaseApp
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageLogText;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private TextMeshProUGUI refreshButtonText;
    [SerializeField] private Button refreshButton;

    private bool _bound;
    private float _nextDueMessageCheckTime;

    private void OnEnable()
    {
        EnsureBound();
    }

    private void OnDisable()
    {
        if (CustomerMessageLog.HasListeners)
        {
            CustomerMessageLog.OnMessagesChanged -= RefreshOrders;
        }

        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged -= OnTimeChanged;
        }

        _bound = false;
    }

    protected override void OnAppOpened()
    {
        EnsureBound();
        CustomerMessageLog.SyncActiveOrders();
        RefreshOrders();
    }

    private void EnsureBound()
    {
        if (_bound)
        {
            return;
        }

        CustomerMessageLog.SyncActiveOrders();
        CustomerMessageLog.OnMessagesChanged -= RefreshOrders;
        CustomerMessageLog.OnMessagesChanged += RefreshOrders;

        if (DayClock.Instance != null)
        {
            DayClock.Instance.OnTimeChanged -= OnTimeChanged;
            DayClock.Instance.OnTimeChanged += OnTimeChanged;
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(ManualRefresh);
            refreshButton.onClick.AddListener(ManualRefresh);
        }

        _bound = true;
    }

    private void ManualRefresh()
    {
        CustomerMessageLog.SyncActiveOrders();
        CustomerMessageLog.CheckDueMessages();
        RefreshOrders();
    }

    private void OnTimeChanged(float currentHour)
    {
        if (Time.time < _nextDueMessageCheckTime)
        {
            return;
        }

        _nextDueMessageCheckTime = Time.time + 8f;
        CustomerMessageLog.CheckDueMessages();

        if (appScreen != null && appScreen.activeInHierarchy)
        {
            RefreshOrders();
        }
    }

    private void RefreshOrders()
    {
        List<CustomerOrder> orders = CustomerQueue.Instance != null
            ? CustomerQueue.Instance.ActiveOrders
            : new List<CustomerOrder>();
        orders.RemoveAll(order => order == null || order.isPickedUp || order.isFailed);
        bool hasOrders = orders.Count > 0;

        if (titleText != null)
        {
            titleText.text = hasOrders ? $"Đơn khách ({orders.Count})" : "Đơn khách";
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasOrders);
            emptyText.text = "Chưa có đơn sửa nào.\nNhận đồ từ khách để xem món, hạn lấy và trạng thái ở đây.";
        }

        if (messageLogText == null)
        {
            return;
        }

        messageLogText.gameObject.SetActive(hasOrders);
        if (!hasOrders)
        {
            messageLogText.text = string.Empty;
            return;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < orders.Count; i++)
        {
            CustomerOrder order = orders[i];
            string status = GetOrderStatus(order, out string statusColor);
            builder.Append("<color=#ffd166><b>").Append(EscapeRichText(order.customerName)).Append("</b></color>\n");
            builder.Append("<b>Món:</b> ").Append(EscapeRichText(order.itemName)).Append("\n");
            builder.Append("<b>Hẹn lấy:</b> ").Append(FormatAppointment(order));
            builder.Append("  <color=#9aa0aa>(").Append(FormatTimeRemaining(order)).Append(")</color>\n");
            builder.Append("<b>Trạng thái:</b> <color=").Append(statusColor).Append(">");
            builder.Append(status).Append("</color>\n");
            builder.Append("<b>Tiền công:</b> ").Append(order.negotiatedPrice.ToString("N0")).Append(" VNĐ\n");
            if (i < orders.Count - 1)
            {
                builder.Append("<color=#2b3340>--------------------------------</color>\n");
            }
        }

        messageLogText.text = builder.ToString();

        if (refreshButtonText != null)
        {
            refreshButtonText.text = "Cập nhật";
        }
    }

    private string GetOrderStatus(CustomerOrder order, out string color)
    {
        if (order.isCompleted)
        {
            color = "#55ff88";
            return "Đã sửa xong - chờ khách lấy";
        }

        float hoursUntilDue = GetHoursUntilDue(order);
        if (hoursUntilDue <= 0f)
        {
            color = "#ff5555";
            return "Quá hẹn - khách sắp tới";
        }

        if (hoursUntilDue <= 1.5f)
        {
            color = "#ffd166";
            return "Sắp tới giờ hẹn";
        }

        color = "#8bd3ff";
        return "Đang sửa";
    }

    private string FormatAppointment(CustomerOrder order)
    {
        return $"Ngày {order.appointmentDay} - {FormatHour(order.appointmentHour)}";
    }

    private string FormatTimeRemaining(CustomerOrder order)
    {
        float hoursUntilDue = GetHoursUntilDue(order);
        if (hoursUntilDue <= 0f)
        {
            return "quá hẹn";
        }

        if (hoursUntilDue >= 24f)
        {
            int days = Mathf.FloorToInt(hoursUntilDue / 24f);
            int hours = Mathf.CeilToInt(hoursUntilDue - (days * 24f));
            return $"{days} ngày {hours} giờ nữa";
        }

        if (hoursUntilDue >= 1f)
        {
            return $"{Mathf.CeilToInt(hoursUntilDue)} giờ nữa";
        }

        return $"{Mathf.Max(1, Mathf.CeilToInt(hoursUntilDue * 60f))} phút nữa";
    }

    private float GetHoursUntilDue(CustomerOrder order)
    {
        if (DayClock.Instance == null)
        {
            return 999f;
        }

        return ((order.appointmentDay - DayClock.Instance.CurrentDay) * 24f)
            + (order.appointmentHour - DayClock.Instance.CurrentHour);
    }

    private string FormatHour(float hour)
    {
        int wholeHour = Mathf.FloorToInt(hour);
        int minute = Mathf.RoundToInt((hour - wholeHour) * 60f);
        if (minute >= 60)
        {
            wholeHour += 1;
            minute -= 60;
        }

        return $"{wholeHour:00}:{minute:00}";
    }

    private string EscapeRichText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Không rõ";
        }

        return text.Replace("<", string.Empty).Replace(">", string.Empty);
    }
}

public struct CustomerMessageEntry
{
    public string Sender;
    public string Body;
    public string TimeLabel;
    public bool IsPlayer;

    public CustomerMessageEntry(string sender, string body, string timeLabel, bool isPlayer)
    {
        Sender = sender;
        Body = body;
        TimeLabel = timeLabel;
        IsPlayer = isPlayer;
    }
}

public static class CustomerMessageLog
{
    private const int MaxMessages = 50;

    private static readonly List<CustomerMessageEntry> _messages = new List<CustomerMessageEntry>();
    private static readonly HashSet<CustomerOrder> _syncedOrders = new HashSet<CustomerOrder>();
    private static readonly HashSet<CustomerOrder> _dueReminderOrders = new HashSet<CustomerOrder>();
    private static readonly HashSet<CustomerOrder> _almostDueReminderOrders = new HashSet<CustomerOrder>();

    public static event System.Action OnMessagesChanged;

    public static bool HasListeners => OnMessagesChanged != null;
    public static IReadOnlyList<CustomerMessageEntry> Messages => _messages;

    public static void AddOrderAccepted(CustomerOrder order)
    {
        if (order == null)
        {
            return;
        }

        _syncedOrders.Add(order);
        AddCustomerMessage(order, $"Em gửi anh món {order.itemName}. Hẹn {FormatAppointment(order)} em qua lấy nha.");
        AddPlayerMessage("Ok, sửa xong anh báo em.");
    }

    public static void AddOrderCompleted(CustomerOrder order)
    {
        if (order == null)
        {
            return;
        }

        AddCustomerMessage(order, $"Em nhận lại {order.itemName} rồi. Cảm ơn anh nha!");
    }

    public static void AddOrderFailed(CustomerOrder order)
    {
        if (order == null)
        {
            return;
        }

        AddCustomerMessage(order, $"Em chờ lâu quá, em lấy lại {order.itemName} mang qua chỗ khác nha.");
    }

    public static void SyncActiveOrders()
    {
        if (CustomerQueue.Instance == null)
        {
            return;
        }

        List<CustomerOrder> orders = CustomerQueue.Instance.ActiveOrders;
        for (int i = 0; i < orders.Count; i++)
        {
            CustomerOrder order = orders[i];
            if (order == null || _syncedOrders.Contains(order))
            {
                continue;
            }

            AddOrderAccepted(order);
        }

        CheckDueMessages();
    }

    public static void CheckDueMessages()
    {
        if (CustomerQueue.Instance == null || DayClock.Instance == null)
        {
            return;
        }

        int currentDay = DayClock.Instance.CurrentDay;
        float currentHour = DayClock.Instance.CurrentHour;
        List<CustomerOrder> orders = CustomerQueue.Instance.ActiveOrders;

        for (int i = 0; i < orders.Count; i++)
        {
            CustomerOrder order = orders[i];
            if (order == null || order.isPickedUp || order.isFailed)
            {
                continue;
            }

            float hoursUntilDue = ((order.appointmentDay - currentDay) * 24f) + (order.appointmentHour - currentHour);
            if (order.isCompleted)
            {
                continue;
            }

            if (hoursUntilDue <= 0f)
            {
                if (_dueReminderOrders.Add(order))
                {
                    AddCustomerMessage(order, $"Tới giờ hẹn rồi anh ơi. {order.itemName} của em xong chưa?");
                }
            }
            else if (hoursUntilDue <= 1.5f)
            {
                if (_almostDueReminderOrders.Add(order))
                {
                    AddCustomerMessage(order, $"Còn khoảng {Mathf.CeilToInt(hoursUntilDue * 60f)} phút nữa tới hẹn, anh sửa kịp không?");
                }
            }
        }
    }

    private static void AddCustomerMessage(CustomerOrder order, string body)
    {
        string sender = string.IsNullOrWhiteSpace(order.customerName) ? "Khách hàng" : order.customerName;
        AddMessage(new CustomerMessageEntry(sender, body, CurrentTimeLabel(), false));
    }

    private static void AddPlayerMessage(string body)
    {
        AddMessage(new CustomerMessageEntry("Bạn", body, CurrentTimeLabel(), true));
    }

    private static void AddMessage(CustomerMessageEntry message)
    {
        _messages.Add(message);
        while (_messages.Count > MaxMessages)
        {
            _messages.RemoveAt(0);
        }

        OnMessagesChanged?.Invoke();
    }

    private static string CurrentTimeLabel()
    {
        if (DayClock.Instance == null)
        {
            return "Bây giờ";
        }

        return $"Ngày {DayClock.Instance.CurrentDay} - {DayClock.Instance.CurrentHour:00}:00";
    }

    private static string FormatAppointment(CustomerOrder order)
    {
        return $"{order.appointmentHour:00}:00 ngày {order.appointmentDay}";
    }
}
