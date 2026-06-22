# Cấu trúc thư mục dự án (Project Folder Structure)

Đây là tài liệu mô tả chi tiết kiến trúc thư mục dành cho dự án game. Toàn bộ tài nguyên và mã nguồn chính của game được đặt gọn gàng trong thư mục `Assets/_Project/` nhằm phân biệt rõ ràng với các plugin và thư viện bên ngoài.

## Sơ đồ tổng quan

```text
Assets/ (Thư mục gốc)
├── Plugins/ (Nhóm chức năng ngoại vi)
├── ThirdParty/ (Các thư viện bên thứ 3)
└── _Project/ (Thư mục chứa toàn bộ dữ liệu dự án)
    ├── Animations/ (Hoạt ảnh)
    ├── Audio/ (Âm thanh)
    │   ├── Music/ (Nhạc nền)
    │   └── SFX/ (Hiệu ứng âm thanh)
    ├── Materials/ (Vật liệu, Material)
    ├── Models/ (Mô hình 3D)
    ├── Prefabs/ (Các Prefab tạo sẵn)
    │   ├── Characters/ (Nhân vật)
    │   ├── Weapons/ (Vũ khí) - Mới/Quan trọng
    │   ├── Enemies/ (Kẻ địch)
    │   ├── Items/ (Vật phẩm)
    │   └── VFX/ (Hiệu ứng hình ảnh) - Mới/Quan trọng
    ├── Scenes/ (Các màn chơi / Cảnh)
    │   ├── Core/ (Cảnh khởi tạo cốt lõi)
    │   ├── Levels/ (Các màn chơi chính)
    │   └── UI/ (Cảnh giao diện)
    ├── ScriptableObjects/ (Dữ liệu tĩnh)
    │   ├── Items/ - Mới/Quan trọng
    │   ├── Enemies/ - Mới/Quan trọng
    │   ├── Progression/ - Mới/Quan trọng
    │   ├── Weapons/ - Mới/Quan trọng
    │   └── Balancing/ - Mới/Quan trọng
    ├── Scripts/ (Trọng tâm kiến trúc)
    │   ├── Core/ (Quản lý hệ thống cốt lõi)
    │   ├── Player/ (Điều khiển & Chỉ số người chơi)
    │   ├── Weapons/ (Hệ thống vũ khí)
    │   ├── Enemies/ (Trí tuệ nhân tạo & Quản lý kẻ địch)
    │   ├── Inventory/ (Hệ thống túi đồ) - Mới/Quan trọng
    │   ├── Progression/ (Hệ thống tiến trình) - Mới/Quan trọng
    │   ├── Interaction/ (Hệ thống tương tác)
    │   ├── UI/ (Quản lý giao diện)
    │   ├── Economy/ (Hệ thống kinh tế) - Mới/Quan trọng
    │   ├── Utils/ (Các công cụ tiện ích)
    │   └── VFX/ (Quản lý hiệu ứng) - Mới/Quan trọng
    ├── Textures/ (Kết cấu, hình ảnh thô)
    └── UI/ (Tài nguyên giao diện)
        ├── Sprites/ (Hình ảnh 2D cho UI)
        ├── Fonts/ (Phông chữ)
        └── Icons/ (Biểu tượng)
```

## Diễn giải chi tiết nhóm `Scripts/` (Trọng tâm kiến trúc)

Đây là nơi chứa toàn bộ logic lập trình của game, được module hóa thành các phần riêng biệt để dễ dàng bảo trì và mở rộng:

### 1. Core/ (Hệ thống cốt lõi)
- `GameManager.cs`: Quản lý trạng thái chung của toàn bộ game.
- `AudioManager.cs`: Xử lý âm thanh tổng thể.
- `SceneLoader.cs`: Quản lý việc chuyển đổi giữa các Scene mượt mà.
- `EventBus.cs` (*Quan trọng*): Trạm trung chuyển sự kiện, giúp các hệ thống giao tiếp mà không phụ thuộc trực tiếp vào nhau.
- `SaveSystem.cs` (*Quan trọng*): Hệ thống lưu và tải dữ liệu người chơi.
- `TimeManager.cs`: Kiểm soát thời gian trong game.

### 2. Player/ (Người chơi)
- `PlayerController.cs`: Đầu vào điều khiển của người chơi.
- `PlayerMovement.cs`: Xử lý di chuyển, nhảy, va chạm.
- `PlayerHealth.cs`: Quản lý máu và sát thương nhận vào.
- `PlayerStats.cs` (*Quan trọng*): Các chỉ số cơ bản của nhân vật.
- `PlayerCamera.cs`: Xử lý góc nhìn camera bám theo nhân vật.

### 3. Weapons/ (Vũ khí)
- `WeaponManager.cs`: Quản lý kho vũ khí mà người chơi đang cầm.
- `GunBase.cs`: Lớp cơ sở cho súng.
- `MeleeBase.cs`: Lớp cơ sở cho vũ khí cận chiến.
- `WeaponModifier.cs` (*Quan trọng*): Nâng cấp/độ chế vũ khí.
- `Projectile.cs`: Xử lý đường đạn.

### 4. Enemies/ (Kẻ địch)
- `EnemyAI.cs`: Logic trí tuệ nhân tạo cơ bản.
- `StateMachine.cs`: Máy trạng thái hữu hạn cho hành vi địch (tuần tra, tấn công, rượt đuổi).
- `EnemyHealth.cs`: Quản lý sinh lực địch.
- `LootDropper.cs`: Cơ chế rớt đồ khi địch chết.
- `EnemyScaler.cs`: Cân bằng sức mạnh địch theo tiến trình game.

### 5. Inventory/ (Túi đồ) - *Mới / Quan trọng*
- `InventoryManager.cs`: Quản lý các khe chứa đồ.
- `ItemBase.cs`: Lớp cơ sở của vật phẩm.
- `LootTable.cs`: Bảng tỷ lệ rớt đồ.
- `RaritySystem.cs`: Hệ thống độ hiếm (Common, Rare, Epic, Legendary).
- `EquipmentSlot.cs`: Khe trang bị trên người nhân vật.

### 6. Progression/ (Tiến trình) - *Mới / Quan trọng*
- `XPSystem.cs`: Điểm kinh nghiệm.
- `LevelManager.cs`: Xử lý lên cấp.
- `SkillTree.cs`: Cây kỹ năng đặc biệt.
- `QuestManager.cs`: Quản lý nhiệm vụ phụ và chính.
- `AchievSystem.cs`: Hệ thống thành tựu đạt được.

### 7. Interaction/ (Tương tác)
- `IInteractable.cs`: Interface chuẩn cho mọi vật thể có thể tương tác.
- `RaycastInteract.cs`: Dùng tia (raycast) để phát hiện vật thể tương tác.
- `PickupItem.cs` (*Quan trọng*): Cấu hình vật thể nhặt được dưới đất.

### 8. UI/ (Giao diện)
- `HUDManager.cs`: Màn hình hiển thị chính (Máu, Giáp, Đạn...).
- `MenuManager.cs`: Quản lý các menu Pause, Main Menu.
- `InventoryUI.cs`: Giao diện túi đồ trực quan.
- `DamagePopup.cs`: Hiệu ứng nhảy số sát thương khi bắn trúng địch.

### 9. Economy/ (Kinh tế) - *Mới / Quan trọng*
- `CurrencyManager.cs`: Quản lý tiền tệ (Vàng, tiền mặt...).
- `ShopManager.cs`: Hệ thống cửa hàng mua bán.
- `CraftingSystem.cs`: Chế tạo đồ vật/vũ khí.

### 10. Utils/ (Tiện ích)
- `Extensions.cs`: Các hàm mở rộng hữu ích.
- `Singleton.cs`: Mẫu thiết kế Singleton dùng chung.
- `ObjectPool.cs`: Hệ thống tái sử dụng object (đạn, vfx) để tối ưu hiệu suất.

### 11. VFX/ (Hiệu ứng) - *Mới / Quan trọng*
- `PooledEffect.cs`: Quản lý sinh/hủy vòng đời hiệu ứng một cách tối ưu.
- `HitEffect.cs`: Hiệu ứng va chạm.
- `MuzzleFlash.cs`: Chớp lửa đầu nòng súng.

---
*Cấu trúc này được thiết kế dựa trên nguyên tắc tách biệt dữ liệu, tối ưu hóa tái sử dụng và mở rộng (Interface-based & Object Pooling), cùng với sự đảm bảo luồng giao tiếp decoupling qua EventBus.*
