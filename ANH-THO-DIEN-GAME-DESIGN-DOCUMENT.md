# Game Design Document — "Anh Thợ Điện"
*Bản phác thảo chi tiết v2 — dựa trên bản gốc của bạn, tích hợp các cải tiến chống nhàm và tăng độ gây nghiện dài hạn.*

---

## 1. Pitch ngắn (Logline)

Một tiệm sửa đồ điện nhỏ trong con hẻm Sài Gòn. Dưới góc nhìn thứ nhất, người chơi tự tay cầm từng món đồ hỏng lên, soi từng vết cháy, hàn từng mạch, nối từng dây — và mỗi lần sửa là một lựa chọn: làm nhanh để kịp khách tiếp theo, hay làm kỹ để giữ danh tiếng. Từ một tiệm chỉ sửa được quạt bàn, nồi cơm điện, người chơi dần xây dựng đế chế sửa chữa của riêng mình giữa lòng thành phố.

---

## 2. Triết lý thiết kế (Design Mindset)

Phần này giữ lại toàn bộ tinh thần bản gốc của bạn (FPP, mini-game đa dạng, bản sắc Sài Gòn) và bổ sung các nguyên tắc giải quyết điểm yếu đã chỉ ra ở bản đánh giá trước — **đọc kỹ trước khi code**, vì đây là phần quyết định game có "gây nghiện" được hay không.

### 2.1. Không có 2 lần sửa giống nhau (Chống nhàm bằng Randomization)
Đây là nguyên tắc quan trọng nhất bổ sung vào bản gốc. Cùng là sửa quạt bàn, nhưng vị trí lỗi (tụ điện nào hỏng, dây nào đứt, mạch nào cháy) phải được random hóa mỗi lần — nếu không, minigame sẽ bị học thuộc và biến thành phản xạ máy móc vô hồn sau vài lần lặp.
> **Áp dụng vào code:** Mỗi `RepairItemData` (ScriptableObject) không lưu vị trí lỗi cố định, mà lưu **bảng khả năng lỗi** (fault pool) — khi bắt đầu sửa, hệ thống random chọn 1-2 lỗi từ pool đó cho lượt chơi hiện tại. Tách riêng `FaultRandomizer` khỏi `MinigameManager` để dễ tái sử dụng cho mọi loại đồ vật.

### 2.2. Mỗi lượt sửa là một quyết định, không chỉ là một bài kiểm tra kỹ năng
Bản gốc chỉ có "làm đúng = thắng, làm sai = thua". Cần thêm trục đánh đổi: **sửa nhanh (rủi ro bị trả lại, mất danh tiếng) vs sửa kỹ (tốn thời gian, được tip + danh tiếng cao hơn)**. Điều này biến mỗi đơn sửa thành một lựa chọn chiến lược, không chỉ là phản xạ tay.
> **Áp dụng vào code:** Mỗi minigame trả về một `RepairQuality` (enum: Hỏng / Tạm / Tốt / Hoàn hảo) dựa trên độ chính xác + thời gian hoàn thành, không chỉ pass/fail nhị phân. `RepairQuality` này quyết định tiền công, tip, và điểm danh tiếng nhận được.

### 2.3. Áp lực phải tăng dần theo tiến trình, không phẳng từ đầu đến cuối
Giai đoạn đầu game (1 khách/lượt) đúng với tinh thần "thư thái học nghề" của bản gốc — giữ nguyên. Nhưng giai đoạn sau, khi danh tiếng tăng, phải xuất hiện áp lực **nhiều khách chờ cùng lúc**, buộc người chơi ưu tiên ai trước — đây là nguồn gây nghiện lớn nhất còn thiếu ở bản gốc.
> **Áp dụng vào code:** Tách `CustomerQueue` thành hệ thống độc lập, nhận nhiều `CustomerOrder` đồng thời ở giai đoạn 2-3, mỗi order có deadline riêng. Không hardcode "1 khách tại 1 thời điểm" trong logic — thiết kế ngay từ đầu để dễ mở rộng số khách đồng thời qua config (tránh phải viết lại toàn bộ flow khi muốn tăng độ khó).

### 2.4. Bản sắc địa phương là tài sản cốt lõi, không phải gia vị
Tiếng rao ve chai, mưa dột mái tôn, bà chủ trọ, sinh viên xin giảm giá — đây chính là lợi thế cạnh tranh thật của game so với các game sửa chữa phương Tây (PC Building Simulator, House Flipper). Mọi quyết định thiết kế nội dung (NPC, sự kiện, âm thanh) nên ưu tiên giữ và khai thác sâu thêm chất liệu này, không pha loãng bằng nội dung chung chung.
> **Áp dụng vào code/asset:** `NPCArchetypeData` nên có field riêng cho lời thoại đặc trưng, không dùng template chung. Âm thanh môi trường nên là layer động (mưa to/nhỏ, giờ cao điểm xe cộ) thay vì 1 file ambience cố định.

### 2.5. Có vòng kinh tế chủ động bên cạnh vòng bị động
Bản gốc chỉ có khách tự tìm đến (passive income loop). Bổ sung vòng lặp **"ve chai"** — mua đồ hỏng giá rẻ từ người bán dạo, tự sửa, bán lại lấy lời — cho người chơi một lựa chọn chủ động khi không có khách, đúng với chi tiết "tiếng rao ve chai" đã có sẵn trong ý tưởng gốc nhưng chưa được khai thác thành cơ chế.
> **Áp dụng vào code:** `VeChaiSystem` độc lập, tái sử dụng lại đúng `MinigameManager` và `FaultRandomizer` đã có (đồ ve chai cũng cần sửa) — không cần viết hệ thống sửa chữa riêng, chỉ khác nguồn gốc đơn hàng và không có deadline khách thúc.

### 2.6. Luôn phải có mục tiêu kế tiếp ở mọi giai đoạn (kể cả endgame)
Bản gốc dừng ở "có cửa hàng hoành tráng" — đây là điểm kết thúc động lực. Cần lớp meta sau đó: thuê thợ phụ, mở chi nhánh — để người chơi luôn có lý do quay lại, tương tự thiết kế end-game "mở đại lý riêng" mà chúng ta đã làm ở game shipper trước.
> **Áp dụng vào code:** `BusinessExpansionSystem` ở giai đoạn cuối — thợ phụ là NPC tự động chạy một phiên bản đơn giản hóa của `MinigameManager` (tính theo xác suất thành công dựa trên "kỹ năng" thợ, không cần người chơi chơi minigame hộ).

---

## 3. Bối cảnh & Nhân vật

- **Nhân vật chính:** Thợ sửa đồ điện trẻ, mở tiệm nhỏ trong một con hẻm ở Sài Gòn.
- **Không gian khởi đầu:** Phòng trọ chật hẹp kiêm tiệm sửa đồ, mái tôn, nóng bức.
- **Góc nhìn:** First-Person Perspective (FPP) — cầm, xoay, soi từng món đồ trước khi sửa.
- **Khát vọng dài hạn:** Từ sửa đồ lặt vặt (quạt, nồi cơm điện) → sửa đồ cao cấp (laptop, PC, độ bàn phím cơ) → mở chuỗi tiệm, thuê thợ phụ.

---

## 4. Hệ thống chỉ số đề xuất (Core Stats)

| Chỉ số | Vai trò | Thay đổi khi |
|---|---|---|
| **Tiền mặt (Cash)** | Trả tiền điện/nước/trọ, mua dụng cụ, nhập đồ ve chai | Sửa đồ thành công (+), chi tiêu sinh hoạt (-) |
| **Thể lực (Stamina)** | Ảnh hưởng độ chính xác trong minigame (mệt → tay run, dễ trượt) | Làm việc liên tục (-), ăn uống/nghỉ (+) |
| **Danh tiếng (Reputation)** | Quyết định tệp khách tìm đến (khách thường → khách đại gia) và mở khóa Skill Tree | Chất lượng sửa (Tốt/Hoàn hảo: +, Hỏng: -) |
| **Thời gian (Day Clock)** | Khung giờ mở tiệm, deadline từng đơn khách | Trôi theo thời gian thực trong ngày |

---

## 5. Vòng lặp gameplay lõi (Core Loop) — bản cập nhật

```
7:00 AM — Mở cửa
   → Kéo cửa cuốn (tương tác FPP)
   → Kiểm tra chỉ số (Stamina, Cash, đơn hàng đang chờ)

Tiếp khách
   → NPC gõ cửa / để đồ kèm giấy nhớ
   → Cầm đồ lên, xoay soi để "khám bệnh" sơ bộ
   → [Giai đoạn 2-3] Nếu có nhiều khách cùng lúc → chọn thứ tự ưu tiên xử lý

Sửa chữa (chuyển sang chế độ Minigame)
   → Hệ thống random hóa lỗi cho lượt này (theo mục 2.1)
   → Chọn tốc độ xử lý: Nhanh (rủi ro) hay Kỹ (chắc tay, tốn giờ) (theo mục 2.2)
   → Hoàn thành → nhận kết quả RepairQuality (Hỏng/Tạm/Tốt/Hoàn hảo)

Trả đồ & thu tiền
   → Tiền công theo chất lượng sửa, có thể có tip
   → Cộng/trừ điểm Danh tiếng tương ứng

[Khi không có khách] Tùy chọn vòng phụ "Ve chai"
   → Mua đồ hỏng giá rẻ từ người bán dạo
   → Tự sửa, bán lại lấy lời (theo mục 2.5)

Chiều tối — Quản lý
   → Tính toán thu nhập trong ngày
   → Ăn uống (mì gói/cơm tấm) hồi Stamina
   → Trả tiền điện/nước/trọ
   → Lên "máy tính" mua dụng cụ/nguyên liệu mới

Đêm — Nghỉ ngơi → Qua ngày mới → Lặp lại
```

---

## 6. Hệ thống Mini-game chi tiết (giữ 4 loại gốc + cơ chế random/quality)

| Loại | Cơ chế | Random hóa | Trục Nhanh/Kỹ |
|---|---|---|---|
| **Khám bệnh (Diagnosis)** | Đo tụ điện bằng đồng hồ vạn năng, tìm điểm bất thường (kiểu dò mìn) | Vị trí điểm lỗi đổi mỗi lượt trong 1 lưới cố định | Dò ít điểm = nhanh nhưng dễ bỏ sót lỗi ẩn |
| **Hàn mạch (Soldering)** | Bấm đúng lúc thanh chạy vào vùng xanh | Số lượng điểm cần hàn + vị trí vùng xanh thay đổi theo độ khó món đồ | Vùng xanh hẹp hơn nếu chọn "hàn nhanh", thưởng tốc độ nếu thành công |
| **Nối dây (Rewiring)** | Nối các điểm cùng màu, tránh chéo dây | Sơ đồ vị trí điểm + số màu cần nối random theo món đồ | Nối tắt qua ít điểm hơn (nhanh, rủi ro sai mạch) vs nối đầy đủ sơ đồ (chắc, lâu hơn) |
| **Lắp ráp & Vệ sinh** | Chà rỉ sét, vặn ốc đúng chiều | Số lượng/vị trí điểm rỉ sét, hướng vặn ốc đổi mỗi lượt | Chà sơ (nhanh, dễ bị khách chê) vs chà kỹ (lâu, được đánh giá cao) |

> Mỗi `RepairItemData` (ScriptableObject) chỉ cần khai báo: tên đồ, độ khó, **loại minigame áp dụng**, **fault pool** (bảng lỗi khả dĩ), tiền công cơ bản, yêu cầu danh tiếng tối thiểu. Việc này giữ đúng tinh thần data-driven mà bản gốc đã đề xuất, chỉ mở rộng thêm field fault pool.

---

## 7. Hệ thống khách hàng & Danh tiếng

- **Tệp khách theo cấp danh tiếng** (giữ nguyên ý tưởng gốc, làm rõ cơ chế mở khóa):
  - Danh tiếng thấp: Bà chủ trọ (đồ lặt vặt, trừ tiền nhà), khách hẻm xóm.
  - Danh tiếng trung: Cậu sinh viên (laptop hỏng, xin giảm giá — tạo tình huống thương lượng giá).
  - Danh tiếng cao: Khách đại gia (đồ cao cấp, PC, độ bàn phím cơ — tiền công cao, yêu cầu chất lượng khắt khe hơn).
- **Hàng đợi khách (Customer Queue):** Ở giai đoạn 1 chỉ có 1 khách/lượt (đúng nhịp học nghề chậm của bản gốc). Từ giai đoạn 2 trở đi, queue cho phép 2-3 khách chờ cùng lúc với deadline riêng — người chơi phải chọn ưu tiên xử lý đơn nào trước.
- **Hệ quả bỏ lỡ deadline:** Khách bỏ đi, mất điểm danh tiếng — tạo áp lực thật sự thay vì game chỉ "chờ vô thời hạn".

---

## 8. Hệ thống kinh tế phụ — Vòng lặp "Ve chai"

- Người bán ve chai dạo qua hẻm theo khung giờ ngẫu nhiên trong ngày (gắn với chi tiết tiếng rao đã có trong ý tưởng gốc).
- Người chơi có thể mua đồ hỏng giá rẻ, tự sửa bằng đúng hệ thống minigame hiện có, rồi bán lại (cho khách hoặc qua "máy tính"/chợ online trong game) để lấy lời.
- Vai trò: cho người chơi việc để làm khi không có khách tới, biến thời gian rảnh thành cơ hội thu nhập chủ động, tránh game có những khoảng "chờ" vô nghĩa.

---

## 9. Hệ thống Tiến triển & Nâng cấp (giữ nguyên ý tưởng gốc, bổ sung liên kết hệ thống)

- **Cây kỹ năng (Skill Tree):** Mở khóa loại đồ được sửa theo thứ tự độ khó (quạt, nồi cơm điện → lò vi sóng, tivi → laptop → PC cao cấp/độ bàn phím cơ). Mở khóa gắn với mốc Danh tiếng + kinh nghiệm tích lũy.
- **Nâng cấp đồ nghề:** Mỏ hàn xịn → vùng xanh trong minigame Hàn mạch rộng hơn. Kính lúp/đèn LED → minigame Khám bệnh hiển thị rõ hơn, dễ phát hiện lỗi ẩn.
- **Nâng cấp không gian sống:** Sơn tường, máy lạnh, loa xịn (ảnh hưởng Stamina hồi nhanh hơn/buff nhẹ tinh thần) → tiệm lớn → **chuỗi cửa hàng** (xem mục 10).

---

## 10. Lớp Meta Endgame — Mở rộng kinh doanh

*(Bổ sung mới so với bản gốc — giải quyết vấn đề "hết mục tiêu sau khi giàu")*

- **Thuê thợ phụ (NPC):** Thợ phụ tự động xử lý các đơn đồ dễ (quạt, nồi cơm điện) theo xác suất thành công dựa trên "kỹ năng" của thợ (không cần người chơi chơi minigame hộ) — tạo nguồn thu nhập passive.
- **Mở chi nhánh thứ 2:** Yêu cầu vốn lớn, mở ra một khu vực bản đồ mới (hẻm khác/quận khác), nhân đôi nguồn khách nhưng cũng nhân đôi chi phí vận hành.
- **Mục tiêu phá đảo (tuỳ chọn):** Trở thành chuỗi sửa chữa lớn nhất khu vực — có thể thể hiện qua bảng xếp hạng danh tiếng khu vực hoặc cutscene kết thúc tương tự tinh thần "mở đại lý" ở game shipper.

---

## 11. Sự kiện ngẫu nhiên & theo mùa (giữ nguyên ý tưởng gốc, hệ thống hóa lại)

| Loại sự kiện | Ví dụ | Ảnh hưởng gameplay |
|---|---|---|
| Sự kiện ngẫu nhiên trong ngày | Cúp điện đột ngột | Phải dùng đèn pin điện thoại (giảm tầm nhìn minigame) để kịp deadline |
| Sự kiện theo mùa | Mưa ngập hẻm | Không có khách đến — buộc sống bằng tiền tiết kiệm hoặc dồn vào vòng Ve chai |
| Sự kiện theo mùa | Cao điểm Tết | Lượng khách tăng đột biến, deadline gấp hơn, thưởng danh tiếng cao hơn nếu xử lý tốt |

> Đây là lớp nội dung giúp game không giữ một nhịp độ phẳng suốt từ đầu đến cuối, đúng với đề xuất "nội dung theo mùa/sự kiện" ở phần đánh giá trước.

---

## 12. Vòng đời người chơi / Game Progression (3 giai đoạn)

### Giai đoạn 1 — Học nghề (Sinh tồn chậm)
- **Trạng thái:** Chỉ sửa được quạt, nồi cơm điện. Đồ nghề cơ bản, vùng xanh minigame hẹp.
- **Trải nghiệm:** 1 khách/lượt, nhịp độ thư thái để học cơ chế. Cân đo tiền sinh hoạt từng ngày.
- **Thử thách:** Dễ trượt minigame do đồ nghề kém, dễ chọn sai giữa nhanh/kỹ khi chưa quen.

### Giai đoạn 2 — Phát triển & áp lực tăng dần (Ổn định)
- **Trạng thái:** Mở khóa sửa lò vi sóng, tivi, laptop. Đồ nghề được nâng cấp.
- **Trải nghiệm:** Customer Queue xuất hiện nhiều khách cùng lúc, phải ưu tiên xử lý. Vòng Ve chai trở thành nguồn thu phụ ổn định.
- **Thử thách:** Sự kiện ngẫu nhiên (cúp điện, mưa ngập) bắt đầu xuất hiện thường xuyên hơn.

### Giai đoạn 3 — Đỉnh cao & Mở rộng (Endgame)
- **Trạng thái:** Sửa được PC cao cấp, độ bàn phím cơ. Không gian sống đầy đủ tiện nghi.
- **Trải nghiệm:** Thuê thợ phụ, cân nhắc mở chi nhánh thứ 2. Trọng tâm chuyển từ "tự tay sửa" sang "quản lý vận hành".
- **End-game:** Trở thành chuỗi sửa chữa lớn nhất khu vực (cutscene/bảng xếp hạng kết thúc).

---

## 13. Ghi chú triển khai cho dev (Implementation Notes)

| Hệ thống | Trách nhiệm | Lưu ý |
|---|---|---|
| `MinigameManager` | Điều phối 4 loại minigame, nhận kết quả `RepairQuality` | Thiết kế interface chung `IMinigame` để 4 loại minigame cùng tuân theo 1 chuẩn input/output, dễ thêm loại mới sau này |
| `FaultRandomizer` | Random hóa lỗi từ fault pool của từng `RepairItemData` | Tách biệt khỏi `MinigameManager` để tái sử dụng cho cả vòng Ve chai |
| `RepairItemData` (ScriptableObject) | Khai báo tên đồ, độ khó, loại minigame, fault pool, tiền công, yêu cầu danh tiếng | Đây là trung tâm data-driven — designer thêm đồ mới không cần code |
| `CustomerQueue` | Quản lý nhiều đơn khách đồng thời, deadline riêng từng đơn | Không hardcode số lượng khách tối đa — đọc từ config theo giai đoạn |
| `ReputationSystem` | Tính điểm danh tiếng từ `RepairQuality`, mở khóa tệp khách/skill tree | Nên bắn event qua `EventBus` khi đạt mốc, để UI/Skill Tree tự lắng nghe, không phụ thuộc trực tiếp |
| `VeChaiSystem` | Sinh đơn hàng ve chai theo khung giờ ngẫu nhiên | Tái sử dụng `MinigameManager` + `FaultRandomizer`, chỉ khác nguồn gốc đơn và không deadline khách |
| `EventManager` | Trigger sự kiện ngẫu nhiên/theo mùa (cúp điện, mưa ngập, Tết) | Mỗi sự kiện là 1 `GameEventData` (ScriptableObject) khai báo điều kiện trigger + hiệu ứng gameplay, tránh hardcode if-else event trong code |
| `BusinessExpansionSystem` | Quản lý thợ phụ, chi nhánh ở giai đoạn 3 | Thợ phụ chạy phiên bản giản lược của `MinigameManager` dựa trên xác suất, không cần input người chơi |
| `EconomyManager` | Tổng hợp Cash, chi phí sinh hoạt, giá nâng cấp | Tập trung mọi thay đổi Cash qua đây để dễ balance & log |

---

## 14. Lộ trình MVP / Vertical Slice đề xuất

*Đúng với khuyến nghị "đừng làm toàn bộ ngay từ đầu" — thứ tự ưu tiên triển khai để có bản chơi được sớm nhất:*

1. **Vertical slice tối thiểu:** 1 món đồ (quạt bàn) + 1 minigame (Hàn mạch) + random hóa lỗi cơ bản + 1 ngày chơi đầy đủ (thức dậy → sửa → thu tiền → ngủ).
2. Thêm trục Nhanh/Kỹ vào minigame đã có trước khi mở rộng thêm loại đồ mới.
3. Thêm 3 minigame còn lại + mở rộng số lượng `RepairItemData`.
4. Thêm `CustomerQueue` nhiều khách + `ReputationSystem`.
5. Thêm `VeChaiSystem` và `EventManager` (sự kiện ngẫu nhiên).
6. Thêm `BusinessExpansionSystem` (lớp meta endgame) sau cùng — chỉ khi vòng lặp lõi đã vui và ổn định.

---

## 15. Câu hỏi mở cần quyết định thêm

- **Độ dài 1 ngày trong game:** Real-time hay time-skip giữa các mốc (sáng/chiều/tối)?
- **Số lượng khách tối đa đồng thời ở giai đoạn 3** là bao nhiêu để vẫn cảm thấy "thử thách" mà không "quá tải"?
- **Cơ chế thương lượng giá với khách** (vd cậu sinh viên xin giảm giá) — có nên là một mini-tương tác riêng (chọn đáp án đối thoại) hay chỉ là một lựa chọn đơn giản?
- **Phạm vi bản đồ chi nhánh thứ 2:** Có cần dựng scene mới hoàn toàn, hay tái sử dụng layout với skin khác để tiết kiệm thời gian sản xuất?
- **Nền tảng phát hành:** PC (FPP + chuột bàn phím phù hợp tốt nhất) hay có cân nhắc mobile (sẽ ảnh hưởng lớn đến thiết kế control của minigame)?
