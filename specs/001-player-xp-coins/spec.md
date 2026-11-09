# Feature Specification: Tài khoản người chơi, XP và Xu

**Feature Branch**: `feat/player-xp-coins`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: "Tính năng 'Tài khoản người chơi, XP và Xu' cho QuizBattle: gắn tài khoản vào người chơi, sau mỗi trận hợp lệ nhận XP (chỉ tăng, quyết định cấp độ và xếp hạng) và Xu (tiền tệ có thể tăng/giảm, dùng ở cửa hàng của tính năng sau). Khách vẫn chơi như cũ nhưng không nhận thưởng."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Nhận XP và Xu sau trận hợp lệ (Priority: P1)

Người chơi đã đăng nhập vào phòng bằng mã phòng, thi đấu cùng ít nhất một tài khoản khác. Khi trận kết thúc bình thường, họ nhận XP và Xu theo điểm và thứ hạng, và thấy số vừa nhận (kèm thông báo lên cấp nếu có) ngay ở màn kết thúc.

**Why this priority**: Đây là giá trị cốt lõi của tính năng; hồ sơ, xếp hạng và cửa hàng sau này đều dựa vào dữ liệu này.

**Independent Test**: Hai tài khoản khác nhau chơi hết một trận; kiểm tra mỗi người nhận đúng XP/Xu một lần, màn kết thúc hiển thị đúng số và trạng thái lên cấp.

**Acceptance Scenarios**:

1. **Given** hai tài khoản A, B (không phải chủ phòng) đang là người chơi trong một phòng, **When** trận kết thúc sau câu cuối, **Then** A và B mỗi người nhận XP và Xu tính từ điểm trận (Xu cộng thêm thưởng nếu đứng top 1/2/3), và thấy số nhận được ở màn kết thúc.
2. **Given** thưởng đã được cộng cho một người chơi, **When** sự kiện kết thúc bị gửi lặp hoặc người chơi mất mạng rồi kết nối lại, **Then** XP và Xu không bị cộng thêm lần nữa.
3. **Given** việc cộng thưởng làm tổng XP vượt ngưỡng cấp kế tiếp, **When** màn kết thúc hiển thị, **Then** người chơi thấy thông báo đã lên cấp mới.
4. **Given** trận bị dừng giữa chừng (không hết câu cuối), **When** phòng đóng, **Then** không ai nhận XP hay Xu.

---

### User Story 2 - Gắn tài khoản khi vào phòng (Priority: P1)

Người dùng đã đăng nhập vào phòng thì người chơi của họ được gắn với tài khoản; vào lại hoặc kết nối lại thì dùng lại đúng người chơi cũ, giữ điểm. Khách vẫn vào bằng tên hiển thị như hiện nay.

**Why this priority**: Không gắn tài khoản thì không thể trao thưởng; đồng thời mỗi tài khoản chỉ có một người chơi mỗi phòng để chống nhận thưởng nhiều lần.

**Independent Test**: Một tài khoản vào phòng, thoát, vào lại: vẫn chỉ có một người chơi, điểm giữ nguyên. Một khách vào phòng: chơi được, không có tài khoản gắn kèm.

**Acceptance Scenarios**:

1. **Given** người dùng đã đăng nhập, **When** vào phòng bằng mã phòng, **Then** người chơi được tạo và gắn với tài khoản đó.
2. **Given** tài khoản đã có người chơi trong phòng, **When** vào lại hoặc kết nối lại, **Then** hệ thống dùng lại người chơi cũ, không tạo người chơi mới.
3. **Given** người dùng chưa đăng nhập, **When** vào phòng bằng tên hiển thị, **Then** vào được như hiện nay và là khách, không nhận XP/Xu.

---

### User Story 3 - Quy tắc trận hợp lệ và chủ phòng (Priority: P1)

Thưởng chỉ được trao khi trận có ít nhất 2 tài khoản khác nhau là người chơi (không tính chủ phòng), và chủ phòng không nhận thưởng trong phòng của chính mình. Khi trận không hợp lệ, màn kết thúc nói rõ lý do.

**Why this priority**: Chặn việc chơi một mình hoặc tự tạo phòng để cày XP/Xu, bảo vệ tính công bằng của bảng xếp hạng và nền kinh tế Xu.

**Independent Test**: Chạy các trận chỉ có một tài khoản, trận chủ phòng tự vào chơi, trận có khách; kiểm tra không ai nhận thưởng sai và lý do hiển thị đúng.

**Acceptance Scenarios**:

1. **Given** chỉ có một tài khoản (ngoài chủ phòng) là người chơi, **When** trận kết thúc, **Then** không ai nhận thưởng và màn kết thúc nói trận chưa đủ 2 tài khoản.
2. **Given** chủ phòng tự vào phòng của mình làm người chơi cùng 2 tài khoản khác, **When** trận kết thúc, **Then** chủ phòng không nhận thưởng, hai tài khoản kia vẫn nhận, và màn kết thúc của chủ phòng nói rõ lý do.
3. **Given** trận có 1 tài khoản đã đăng nhập và nhiều khách, **When** trận kết thúc, **Then** không ai nhận thưởng (chưa đủ 2 tài khoản khác nhau).
4. **Given** chủ phòng và đúng 1 tài khoản khác là người chơi, **When** trận kết thúc, **Then** trận không hợp lệ vì chủ phòng không được tính vào số tài khoản.

---

### User Story 4 - Hồ sơ của tôi (Priority: P2)

Người dùng đã đăng nhập mở trang hồ sơ để xem cấp độ, tổng XP, thanh tiến trình tới cấp sau, số dư Xu và lịch sử thưởng các trận gần đây.

**Why this priority**: Giúp người chơi thấy tiến triển, tạo động lực quay lại; không chặn việc trao thưởng nên xếp sau.

**Independent Test**: Sau khi nhận thưởng từ một trận, mở hồ sơ và đối chiếu các số với màn kết thúc và lịch sử.

**Acceptance Scenarios**:

1. **Given** người dùng đã có thưởng từ vài trận, **When** mở hồ sơ, **Then** thấy cấp, tổng XP, tiến trình tới cấp sau, số dư Xu và danh sách trận gần đây kèm XP/Xu nhận được.
2. **Given** người dùng mới chưa chơi trận nào, **When** mở hồ sơ, **Then** thấy cấp khởi đầu, 0 XP, 0 Xu và trạng thái rỗng cho lịch sử.
3. **Given** không tải được dữ liệu, **When** mở hồ sơ, **Then** thấy thông báo lỗi và nút thử lại.

---

### User Story 5 - Cấp độ trong phòng và bảng xếp hạng (Priority: P2)

Cấp độ hiển thị cạnh tên người chơi có tài khoản ở sảnh chờ và bảng điểm. Có bảng xếp hạng top N theo tổng XP.

**Why this priority**: Tạo yếu tố xã hội và cạnh tranh, nhưng phụ thuộc dữ liệu từ các story trước.

**Independent Test**: Hai tài khoản khác cấp vào cùng phòng, kiểm tra cấp hiện đúng cạnh tên; mở bảng xếp hạng và kiểm tra thứ tự theo tổng XP.

**Acceptance Scenarios**:

1. **Given** người chơi có tài khoản trong sảnh chờ hoặc bảng điểm, **When** danh sách hiển thị, **Then** cấp độ xuất hiện cạnh tên; khách không có cấp.
2. **Given** nhiều tài khoản có tổng XP khác nhau, **When** mở bảng xếp hạng, **Then** thấy top N sắp theo tổng XP giảm dần.
3. **Given** chưa tài khoản nào có XP, **When** mở bảng xếp hạng, **Then** thấy trạng thái rỗng.

---

### User Story 6 - Trang /join báo trước về thưởng (Priority: P3)

Ở trang /join, người dùng đã đăng nhập thấy tên hiển thị điền sẵn và được cho biết trận này có thể được tính thưởng.

**Why this priority**: Tiện lợi và khuyến khích đăng nhập, nhưng không ảnh hưởng logic thưởng.

**Independent Test**: Mở /join khi đã đăng nhập và khi chưa đăng nhập, so sánh hiển thị.

**Acceptance Scenarios**:

1. **Given** đã đăng nhập, **When** mở /join, **Then** tên hiển thị được điền sẵn (sửa được) và có thông tin trận này sẽ được tính thưởng.
2. **Given** chưa đăng nhập, **When** mở /join, **Then** form như hiện nay, kèm gợi ý rằng khách không nhận thưởng.

---

### Edge Cases

- Khách chơi xong rồi mới đăng nhập: không được cộng hồi tố cho trận đó.
- Trận đã kết thúc trước khi tính năng ra mắt: không được cộng.
- Cùng một tài khoản mở nhiều tab/thiết bị vào cùng phòng: vẫn chỉ một người chơi, thưởng một lần.
- Hai yêu cầu cộng thưởng đồng thời cho cùng người chơi: chỉ một lần có hiệu lực.
- Mất mạng đúng lúc trận kết thúc: khi kết nối lại người chơi thấy thưởng đã nhận, đúng một lần.
- Người chơi rời phòng giữa chừng rồi trận kết thúc: xem Assumptions.
- Điểm trận bằng 0: XP và Xu theo công thức (có thể bằng 0).
- Hòa điểm ở top 1/2/3: xem Assumptions.
- Một lần trừ Xu (tính năng sau) vượt quá số dư: bị từ chối, số dư không âm.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Hệ thống MUST gắn tài khoản vào người chơi khi người dùng đã đăng nhập vào phòng; khách vẫn vào được và không có tài khoản gắn kèm.
- **FR-002**: Hệ thống MUST đảm bảo mỗi tài khoản có tối đa một người chơi trong một phòng; vào lại hoặc kết nối lại MUST dùng lại người chơi cũ và giữ điểm.
- **FR-003**: Hệ thống MUST chỉ trao XP và Xu khi trận kết thúc bình thường (đã qua câu cuối), và đúng một lần cho mỗi người chơi, kể cả khi gọi lặp, lỗi mạng hoặc xử lý đồng thời.
- **FR-004**: Hệ thống MUST coi trận là hợp lệ để trao thưởng chỉ khi có ít nhất 2 tài khoản khác nhau là người chơi, không tính tài khoản chủ phòng.
- **FR-005**: Hệ thống MUST không trao thưởng cho tài khoản chủ phòng trong phòng do chính họ tạo, kể cả khi họ tự vào làm người chơi.
- **FR-006**: Hệ thống MUST không trao thưởng cho khách.
- **FR-007**: Hệ thống MUST tính XP từ điểm trận theo công thức cố định (mặc định XP = điểm trận / 10, làm tròn xuống).
- **FR-008**: Hệ thống MUST tính Xu từ điểm trận cộng thưởng thứ hạng cho top 1/2/3 của trận.
- **FR-009**: Hệ thống MUST suy ra cấp độ từ tổng XP theo đường cong ngưỡng tăng dần; tổng XP MUST không bao giờ giảm.
- **FR-010**: Hệ thống MUST ghi mỗi thay đổi Xu thành một bản ghi lịch sử gồm lý do, số lượng (dương hoặc âm), trận/phòng liên quan và thời điểm; số dư Xu MUST luôn bằng tổng lịch sử và MUST không bao giờ âm.
- **FR-011**: Loại bản ghi lịch sử Xu MUST mở rộng được để tính năng cửa hàng ghi khoản trừ Xu mà không đổi cách tính số dư.
- **FR-012**: Hệ thống MUST không cộng hồi tố cho trận đã kết thúc trước khi tính năng ra mắt, hoặc cho khách đăng nhập sau khi chơi xong.
- **FR-013**: Màn kết thúc MUST hiển thị XP và Xu vừa nhận và việc có lên cấp hay không; khi không có thưởng MUST nói rõ lý do (chưa đủ 2 tài khoản khác nhau; chủ phòng không nhận thưởng; là khách).
- **FR-014**: Người dùng đã đăng nhập MUST xem được trang hồ sơ gồm cấp, tổng XP, tiến trình tới cấp sau, số dư Xu và lịch sử thưởng các trận gần đây, với đủ trạng thái đang tải, lỗi, rỗng.
- **FR-015**: Sảnh chờ và bảng điểm MUST hiển thị cấp độ cạnh tên người chơi có tài khoản.
- **FR-016**: Hệ thống MUST cung cấp bảng xếp hạng top N theo tổng XP, với trạng thái đang tải, lỗi, rỗng.
- **FR-017**: Trang /join MUST điền sẵn tên hiển thị và báo trận sẽ được tính thưởng khi đã đăng nhập; khi chưa đăng nhập MUST báo khách không nhận thưởng.
- **FR-018**: Hệ thống MUST tính XP và Xu từ điểm đã được xác thực phía máy chủ, không nhận giá trị thưởng hay điểm do máy khách gửi.

### Key Entities *(include if feature involves data)*

- **Tài khoản (User)**: người dùng đã đăng ký; có tổng XP, cấp độ suy ra từ tổng XP, số dư Xu.
- **Người chơi (Player)**: một lượt tham gia trong một phòng; có thể gắn một tài khoản hoặc là khách; mỗi tài khoản tối đa một Player mỗi phòng.
- **Thưởng trận (Match Reward)**: kết quả thưởng của một Player trong một trận: XP, Xu, thứ hạng, hợp lệ hay lý do không thưởng; duy nhất cho mỗi Player.
- **Lịch sử Xu (Coin Transaction)**: bản ghi bất biến mỗi lần Xu đổi: lý do, số lượng (±), phòng/trận liên quan, thời điểm.
- **Cấp độ (Level)**: ngưỡng XP tăng dần để lên từng cấp.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% người chơi có tài khoản trong trận hợp lệ nhận thưởng đúng một lần; 0 trường hợp cộng đôi trong các kịch bản gọi lặp, mất mạng và đồng thời.
- **SC-002**: 0 phần thưởng được trao cho khách, chủ phòng trong phòng của mình, hoặc trận có dưới 2 tài khoản khác nhau.
- **SC-003**: Số dư Xu của mọi tài khoản luôn bằng tổng lịch sử Xu và không bao giờ âm.
- **SC-004**: Người chơi thấy XP/Xu vừa nhận và trạng thái lên cấp trong vòng 3 giây sau khi trận kết thúc.
- **SC-005**: Người dùng mở hồ sơ hoặc bảng xếp hạng và thấy dữ liệu trong vòng 2 giây trong điều kiện bình thường.
- **SC-006**: Khi không có thưởng, 100% màn kết thúc nêu rõ lý do tương ứng.
- **SC-007**: Khách vẫn vào phòng và chơi hết trận với trải nghiệm không đổi so với trước tính năng.

## Assumptions

- Con số cụ thể (XP = điểm / 10 làm tròn xuống; Xu từ điểm và thưởng top 1/2/3; ngưỡng từng cấp; N của bảng xếp hạng) sẽ chốt ở bước clarify; spec dùng các mặc định này làm đề xuất.
- Hệ thống đăng nhập hiện có của chủ phòng được mở rộng cho người chơi; không thêm phương thức đăng nhập mới.
- Người chơi rời phòng giữa chừng nhưng vẫn là người chơi khi trận kết thúc thì vẫn được thưởng theo điểm đã có (giả định, chốt ở clarify).
- Hòa điểm: các người chơi hòa cùng nhận thưởng thứ hạng cao hơn (giả định, chốt ở clarify).
- Khách không bao giờ nhận thưởng, kể cả khi đăng nhập sau đó; không có cơ chế nhận lại.
- Ngoài phạm vi: cửa hàng, nhân vật/avatar, thời trang, màn chơi kiểu lớp học, bục vinh danh, giới hạn dùng AI mỗi ngày, chế độ ôn tập, role Admin. Tính năng cửa hàng sau này chỉ cần ghi thêm bản ghi trừ Xu.
- Trận tính là "kết thúc bình thường" khi đã qua câu cuối theo luồng hiện có của trận.
