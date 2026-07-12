using UnityEngine;

/// <summary>
/// Công cụ hỗ trợ Test nhanh các tính năng nâng cấp và kỹ năng.
/// Gắn script này vào GameManager hoặc một object bất kỳ trong Scene.
/// </summary>
public class DebugTestingTools : MonoBehaviour
{
    private EconomyManager _economy;
    private SpaceUpgradeSystem _space;
    private ReputationSystem _reputation;
    private ToolUpgradeSystem _tools;
    private PlayerStamina _stamina;

    private void Start()
    {
        // Tự động tìm, nếu không có thì gắn luôn vào GameManager để test
        _economy = FindFirstObjectByType<EconomyManager>();
        if (_economy == null) _economy = gameObject.AddComponent<EconomyManager>();

        _space = FindFirstObjectByType<SpaceUpgradeSystem>();
        if (_space == null) _space = gameObject.AddComponent<SpaceUpgradeSystem>();

        _reputation = FindFirstObjectByType<ReputationSystem>();
        if (_reputation == null) _reputation = gameObject.AddComponent<ReputationSystem>();

        _tools = FindFirstObjectByType<ToolUpgradeSystem>();
        if (_tools == null) _tools = gameObject.AddComponent<ToolUpgradeSystem>();

        _stamina = FindFirstObjectByType<PlayerStamina>();
        if (_stamina == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) _stamina = player.AddComponent<PlayerStamina>();
        }
    }

    private void Update()
    {
        // Khi đè phím Left Alt, chuột sẽ hiện ra để bấm nút
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // --- PHÍM TẮT ĐỂ TEST (KHÔNG CẦN CHUỘT) ---
        if (Input.GetKeyDown(KeyCode.Alpha1) && _economy != null) _economy.AddCash(10000f);
        if (Input.GetKeyDown(KeyCode.Alpha2) && _space != null && _economy != null) _space.BuyAirConditioner(_economy, 2000f);
        if (Input.GetKeyDown(KeyCode.Alpha3) && _space != null && _economy != null) _space.BuySpeaker(_economy, 1500f);
        if (Input.GetKeyDown(KeyCode.Alpha4) && _reputation != null) _reputation.ChangeReputation(100);
        if (Input.GetKeyDown(KeyCode.Alpha5) && _tools != null && _economy != null) _tools.UpgradeSolderingIron(_economy, 500f);
        if (Input.GetKeyDown(KeyCode.Alpha6) && _tools != null && _economy != null) _tools.UpgradeMagnifier(_economy, 500f);
        if (Input.GetKeyDown(KeyCode.Alpha7) && _stamina != null) _stamina.DrainStamina(50f);
        if (Input.GetKeyDown(KeyCode.Alpha8) && _stamina != null) _stamina.RestOvernight();
    }

    private void OnGUI()
    {
        // Tạo một hộp menu ở góc trái màn hình để test
        GUILayout.BeginArea(new Rect(10, 10, 350, 400), GUI.skin.box);
        GUILayout.Label("==== DEBUG TOOLS (Bấm phím 1->8 để gọi) ====", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

        // Hiển thị thông tin
        if (_economy != null) GUILayout.Label($"Tiền: {_economy.CurrentCash}$");
        if (_reputation != null) GUILayout.Label($"Danh tiếng: {_reputation.CurrentReputation}");
        if (_stamina != null) GUILayout.Label($"Thể lực: {_stamina.CurrentStamina}/{_stamina.MaxStamina}");

        GUILayout.Space(10);

        // Nút thêm tiền
        if (GUILayout.Button("[Phím 1] Nhận 10.000$"))
        {
            if (_economy != null) _economy.AddCash(10000f);
        }

        // Nút mua máy lạnh
        if (GUILayout.Button("[Phím 2] Mua Máy Lạnh (2000$)"))
        {
            if (_space != null && _economy != null) _space.BuyAirConditioner(_economy, 2000f);
        }

        // Nút mua loa
        if (GUILayout.Button("[Phím 3] Mua Loa Xịn (1500$)"))
        {
            if (_space != null && _economy != null) _space.BuySpeaker(_economy, 1500f);
        }

        // Nút tăng danh tiếng (Mở khóa đồ sửa)
        if (GUILayout.Button("[Phím 4] Tăng 100 Danh tiếng"))
        {
            if (_reputation != null) _reputation.ChangeReputation(100);
        }

        // Nút nâng cấp mỏ hàn
        if (GUILayout.Button("[Phím 5] Nâng cấp Mỏ Hàn (500$)"))
        {
            if (_tools != null && _economy != null) _tools.UpgradeSolderingIron(_economy, 500f);
        }

        // Nút nâng cấp kính lúp
        if (GUILayout.Button("[Phím 6] Nâng cấp Kính Lúp (500$)"))
        {
            if (_tools != null && _economy != null) _tools.UpgradeMagnifier(_economy, 500f);
        }

        // Nút giảm thể lực
        if (GUILayout.Button("[Phím 7] Trừ 50 Thể lực (Mệt mỏi)"))
        {
            if (_stamina != null) _stamina.DrainStamina(50f);
        }

        // Nút đi ngủ hồi thể lực
        if (GUILayout.Button("[Phím 8] Đi ngủ (Hồi thể lực)"))
        {
            if (_stamina != null) _stamina.RestOvernight();
        }

        GUILayout.EndArea();
    }
}
