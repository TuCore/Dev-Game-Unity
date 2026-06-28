using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject định nghĩa kiểu mẫu NPC khách hàng.
/// Có field riêng cho lời thoại đặc trưng, không dùng template chung (GDD mục 2.4).
/// </summary>
[CreateAssetMenu(fileName = "New NPC Archetype", menuName = "Anh Tho Dien/NPC Archetype")]
public class NPCArchetype : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string archetypeName;          // VD: "Bà chủ trọ", "Sinh viên", "Khách đại gia"
    [TextArea(2, 4)]
    public string description;

    [Header("Yêu cầu mở khóa")]
    public int requiredReputation = 0;    // Cần bao nhiêu danh tiếng để khách loại này xuất hiện

    [Header("Đặc trưng giao tiếp")]
    [TextArea(1, 3)]
    public List<string> greetingDialogues;    // Lời chào đặc trưng
    [TextArea(1, 3)]
    public List<string> satisfiedDialogues;   // Khi hài lòng
    [TextArea(1, 3)]
    public List<string> unsatisfiedDialogues; // Khi không hài lòng
    [TextArea(1, 3)]
    public List<string> leavingDialogues;     // Khi bỏ đi (hết deadline)

    [Header("Hành vi đặc biệt")]
    public bool canNegotiatePrice = false;    // VD: Sinh viên xin giảm giá
    public float tipMultiplier = 1f;          // Khách đại gia tip nhiều hơn
    public float patienceMultiplier = 1f;     // Hệ số kiên nhẫn (deadline dài/ngắn hơn)

    [Header("Đồ vật thường mang đến sửa")]
    public List<string> preferredItems;       // VD: ["Quạt bàn", "Nồi cơm điện"]
}
