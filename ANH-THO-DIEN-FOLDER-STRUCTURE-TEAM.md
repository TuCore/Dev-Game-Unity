# Cấu trúc Thư mục Dự án — "Anh Thợ Điện"
*Phiên bản tối ưu cho team 5 sinh viên cùng code song song — dựa trên các hệ thống đã định nghĩa trong GDD v2.*

---

## 1. Nguyên tắc tổ chức cho team nhiều người

Khác với làm solo, cấu trúc cho 5 người code cùng lúc phải giải quyết được 2 vấn đề: **(1) chia việc rõ ràng để không ai đụng tay vào file của người khác**, và **(2) giảm xung đột merge trên Git** — đây là nguyên nhân số 1 khiến team sinh viên cãi nhau/mất code khi làm Unity nhóm. Cấu trúc dưới đây áp dụng 3 nguyên tắc:

1. **Mỗi hệ thống = 1 thư mục riêng, có chủ sở hữu (owner) rõ ràng** — map trực tiếp từ các hệ thống đã định nghĩa trong GDD v2 (`MinigameManager`, `CustomerQueue`, `VeChaiSystem`...), không tạo thư mục chung "Scripts/Misc" mơ hồ.
2. **Data-driven qua ScriptableObject** — để các bạn không rành code (nếu có) vẫn đóng góp được nội dung (thêm món đồ, sự kiện, NPC) mà không cần sửa code, tránh dồn hết việc vào người biết code nhiều nhất.
3. **Tách scene theo hệ thống, load Additive** — đây là điểm quan trọng nhất cho team 5 người: nếu cả 5 người cùng sửa 1 file `.unity` duy nhất, Git sẽ liên tục bị conflict vì scene là file khó merge. Tách nhỏ scene theo hệ thống giúp mỗi người làm việc trên file riêng.

---

## 2. Sơ đồ cây thư mục

```text
Assets/
├── Plugins/                          (Asset bên thứ 3, không tự ý sửa)
├── ThirdParty/
└── _Project/
    ├── Scenes/
    │   ├── Core/
    │   │   └── Bootstrap.unity        # Scene khởi động, load các scene khác Additive
    │   ├── Gameplay/
    │   │   ├── Shop_Main.unity         # Không gian tiệm sửa đồ (FPP di chuyển chính)
    │   │   ├── Repair_Workbench.unity   # Bàn sửa đồ — nơi diễn ra Minigame
    │   │   └── Branch02.unity            # Chi nhánh thứ 2 (giai đoạn endgame)
    │   └── Menu/
    │       ├── MainMenu.unity
    │       └── Pause.unity
    │
    ├── Scripts/
    │   ├── Core/                       👤 Người 1
    │   │   ├── GameManager.cs
    │   │   ├── EventBus.cs
    │   │   ├── SaveSystem.cs
    │   │   ├── SceneLoader.cs
    │   │   └── DayClock.cs              # Quản lý giờ trong ngày (7AM mở cửa...)
    │   ├── Utils/                      👤 Người 1
    │   │   ├── Singleton.cs
    │   │   ├── Extensions.cs
    │   │   └── ObjectPool.cs
    │   │
    │   ├── Player/                     👤 Người 2
    │   │   ├── PlayerController.cs
    │   │   ├── PlayerCamera.cs
    │   │   └── PlayerStamina.cs
    │   ├── Interaction/                👤 Người 2
    │   │   ├── IInteractable.cs
    │   │   ├── RaycastInteract.cs
    │   │   ├── PickupItem.cs
    │   │   └── ItemInspect.cs           # Cầm/xoay/soi đồ vật FPP
    │   │
    │   ├── Repair/                     👤 Người 3
    │   │   ├── RepairStateMachine.cs     # Nhận đồ → Đang sửa → Trả đồ
    │   │   ├── FaultRandomizer.cs
    │   │   └── RepairQuality.cs           # enum Hỏng/Tạm/Tốt/Hoàn hảo
    │   ├── Minigames/                  👤 Người 2 + Người 3 (chia đôi — xem mục 3)
    │   │   ├── IMinigame.cs               # Interface chung — CẢ NHÓM thống nhất từ Day 1
    │   │   ├── MinigameManager.cs
    │   │   ├── Diagnosis/                 (Người 2)
    │   │   ├── Soldering/                 (Người 3)
    │   │   ├── Rewiring/                   (Người 3)
    │   │   └── Cleaning/                    (Người 2)
    │   │
    │   ├── Customer/                   👤 Người 4
    │   │   ├── CustomerQueue.cs
    │   │   ├── CustomerOrder.cs
    │   │   └── NPCArchetype.cs
    │   ├── Reputation/                 👤 Người 4
    │   │   └── ReputationSystem.cs
    │   ├── Economy/                    👤 Người 4
    │   │   ├── EconomyManager.cs
    │   │   ├── ShopManager.cs            # Mua dụng cụ/nguyên liệu
    │   │   └── VeChai/
    │   │       └── VeChaiSystem.cs
    │   │
    │   ├── Progression/                👤 Người 5
    │   │   ├── SkillTree.cs
    │   │   ├── ToolUpgradeSystem.cs
    │   │   └── SpaceUpgradeSystem.cs
    │   ├── Events/                     👤 Người 5
    │   │   └── EventManager.cs           # Cúp điện, mưa ngập, Tết
    │   ├── BusinessExpansion/          👤 Người 5
    │   │   ├── HiredWorkerSystem.cs
    │   │   └── BranchManager.cs
    │   │
    │   └── UI/                         (mỗi người tự làm UI hệ thống của mình)
    │       ├── HUD/                       (Người 2 — chỉ số, tương tác)
    │       ├── RepairUI/                  (Người 3 — UI minigame)
    │       ├── CustomerUI/                (Người 4 — đối thoại, hàng đợi khách)
    │       ├── ShopUI/                    (Người 4 — mua sắm)
    │       └── ProgressionUI/             (Người 5 — skill tree, sự kiện, mở rộng)
    │
    ├── Prefabs/
    │   ├── Player/
    │   ├── Items/                       # Đồ điện cần sửa (quạt, nồi cơm, laptop...)
    │   ├── NPC/
    │   ├── Tools/                       # Mỏ hàn, đồng hồ vạn năng, kính lúp
    │   └── UI/
    │
    ├── Data/                            # ScriptableObject — ai cũng có thể thêm, ít conflict vì mỗi file riêng
    │   ├── RepairItems/                  # RepairItemData cho từng món đồ
    │   ├── FaultPools/                    # Bảng lỗi khả dĩ cho từng loại minigame
    │   ├── NPCArchetypes/
    │   ├── GameEvents/
    │   ├── ToolUpgrades/
    │   └── SkillTreeNodes/
    │
    ├── Art/
    │   ├── Models/
    │   ├── Materials/
    │   ├── Textures/
    │   └── Animations/
    │
    ├── Audio/
    │   ├── Music/
    │   ├── SFX/
    │   └── Ambience/                     # Mưa, tiếng rao ve chai, xe cộ
    │
    └── UI/
        ├── Sprites/
        ├── Fonts/
        └── Icons/
```

---

## 3. Bảng phân chia công việc cho 5 người

| # | Vai trò | Thư mục phụ trách | Lý do phân chia |
|---|---|---|---|
| **Người 1** | **Core & Trưởng nhóm kỹ thuật** | `Core/`, `Utils/`, `Scenes/Core/` | Đây là người touch ít file gameplay cụ thể nhất nên hợp vai trò review code, merge nhánh, giải quyết conflict Git cho cả team |
| **Người 2** | **Player & Tương tác FPP + 2 minigame đơn giản** | `Player/`, `Interaction/`, `Minigames/Diagnosis/`, `Minigames/Cleaning/`, `UI/HUD/` | Diagnosis & Cleaning là 2 minigame mang tính "tương tác trực tiếp với vật thể" — gần với hệ Interaction nên hợp giao cho cùng 1 người |
| **Người 3** | **Lõi sửa chữa + 2 minigame phức tạp** | `Repair/`, `Minigames/IMinigame.cs` (đồng chủ trì với Người 2), `Minigames/Soldering/`, `Minigames/Rewiring/`, `UI/RepairUI/` | Soldering & Rewiring là 2 minigame có logic timing/puzzle phức tạp hơn, nên tách riêng người tập trung sâu |
| **Người 4** | **Khách hàng, Danh tiếng & Kinh tế** | `Customer/`, `Reputation/`, `Economy/` (gồm `VeChai/`), `UI/CustomerUI/`, `UI/ShopUI/` | 3 hệ thống này liên kết chặt với nhau (chất lượng sửa → danh tiếng → tệp khách → thu nhập), nên để 1 người nắm để tránh đứt mạch logic |
| **Người 5** | **Tiến triển, Sự kiện & Mở rộng kinh doanh** | `Progression/`, `Events/`, `BusinessExpansion/`, `UI/ProgressionUI/` | Đây là các hệ thống của **giai đoạn sau** (xem GDD mục 14 — làm sau cùng) → người này có thể hỗ trợ làm Data (`Data/`) hoặc UI chung ở giai đoạn đầu trong khi chờ các hệ thống lõi hoàn thiện |

> ⚠️ **Quan trọng:** `IMinigame.cs` (interface chung cho 4 minigame) phải được **Người 2 và Người 3 thống nhất cùng nhau ngay buổi họp đầu tiên**, trước khi cả hai bắt đầu code minigame riêng — đây là điểm tiếp xúc duy nhất giữa 2 người, làm rõ sớm sẽ tránh phải sửa lại cả 4 minigame sau này.

---

## 4. Quy ước Git để tránh xung đột khi 5 người code cùng lúc

### 4.1. Branch
```
main                  ← chỉ merge bản ổn định, đã test
 └─ develop            ← nhánh tích hợp chung
     ├─ feature/core-savesystem        (Người 1)
     ├─ feature/player-interaction      (Người 2)
     ├─ feature/repair-soldering         (Người 3)
     ├─ feature/customer-queue            (Người 4)
     └─ feature/progression-skilltree      (Người 5)
```
Mỗi người làm trên branch riêng, mở **Pull Request vào `develop`**, để Người 1 (vai trò kỹ thuật chính) review trước khi merge.

### 4.2. Bật Smart Merge cho file Unity
Vào `Edit > Project Settings > Editor`:
- **Asset Serialization Mode:** chọn **Force Text** (để `.unity`, `.prefab` lưu dạng YAML text, merge được thay vì file nhị phân không thể merge).
- **Version Control Mode:** chọn **Visible Meta Files**.

Cấu hình `UnityYAMLMerge` làm merge tool cho Git (tra theo bản Unity Editor đang dùng) để giảm thiểu việc phải merge tay file scene/prefab.

### 4.3. Quy tắc "1 người 1 scene/prefab tại 1 thời điểm"
Vì đã tách `Scenes/Gameplay/` thành nhiều file nhỏ theo hệ thống (mục 2), mỗi người chủ yếu chỉ động vào scene/prefab của hệ thống mình. Khi cần sửa prefab chung (vd `Player.prefab`), **báo trước trong group chat** để tránh 2 người cùng mở 1 prefab cùng lúc.

### 4.4. `.gitattributes` bắt buộc cho Git LFS
```
*.png filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
```

---

## 5. Lưu ý thực tế cho team sinh viên

- **Họp đầu kỳ bắt buộc 30 phút** để thống nhất: `IMinigame` interface, format `RepairItemData`, và format event của `EventBus` — đây là 3 "hợp đồng" mà cả 5 người đều phụ thuộc vào, sai lệch ở đây sẽ gây phải sửa lại nhiều nhất.
- **Người 1 nên là người hiểu toàn bộ GDD nhất**, vì vai trò review/merge code của tất cả người khác đòi hỏi hiểu được logic tổng thể, không chỉ phần Core.
- Theo đúng roadmap MVP ở GDD mục 14: **ưu tiên Người 2 + Người 3 hoàn thành vertical slice (1 món đồ, 1 minigame) trước**, các hệ thống của Người 4 và Người 5 có thể bắt đầu code song song nhưng nên test tích hợp sau khi vertical slice của Người 2/3 chạy được, để tránh cả team build trên một lõi gameplay chưa kiểm chứng là vui hay không.
