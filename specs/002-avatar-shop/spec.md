# Feature Specification: Nhân vật và cửa hàng

**Feature Branch**: `feat/avatar-shop`

**Created**: 2026-11-12

**Status**: Draft

**Input**: User description: "Tính năng 2 'Nhân vật và cửa hàng' cho QuizBattle: người chơi có tài khoản sở hữu một nhân vật (avatar) ghép nhiều lớp, dùng Xu kiếm từ trận đấu (Tính năng 1) để mua đồ cho nhân vật trong cửa hàng, và nhân vật hiện ở các nơi trong game (hồ sơ, cửa hàng, sảnh chờ, bảng điểm, bảng xếp hạng). Khách vẫn chơi như cũ với nhân vật tự sinh, không mua được đồ."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Mua đồ bằng Xu trong cửa hàng (Priority: P1)

Người chơi đã đăng nhập mở trang cửa hàng, xem danh sách đồ chia theo ô (tóc, áo, kính, râu, cử chỉ, hình in, màu nền) và mức giá (Thường 50 Xu, Hiếm 150 Xu, Đặc biệt 400 Xu), xem thử món đồ trên nhân vật của mình, rồi mua. Xu bị trừ ngay, món đồ được đánh dấu "đã sở hữu" vĩnh viễn.

**Why this priority**: Đây là chỗ Xu có giá trị sử dụng, là lý do cho vòng lặp "chơi trận → nhận Xu → mua đồ". Không có việc mua thì các phần còn lại của tính năng không có nội dung để chỉnh.

**Independent Test**: Tài khoản có đủ Xu mua một món Thường; số dư giảm đúng 50, món đó hiện "đã sở hữu" khi tải lại trang, và dữ liệu có thêm đúng một giao dịch Xu -50 với lý do mua đồ trong cửa hàng.

**Acceptance Scenarios**:

1. **Given** người chơi đã đăng nhập và có ít nhất 50 Xu, **When** họ mua một món Thường chưa sở hữu, **Then** số dư giảm đúng 50 Xu, món đó chuyển sang "đã sở hữu" và vẫn như vậy sau khi tải lại trang hay đăng nhập từ thiết bị khác.
2. **Given** người chơi có ít hơn giá món đồ, **When** họ mua món đó, **Then** yêu cầu bị từ chối với thông báo không đủ Xu, số dư và danh sách đồ sở hữu không đổi.
3. **Given** người chơi đã sở hữu một món, **When** họ mua lại món đó (kể cả bấm hai lần liên tiếp hoặc gửi hai yêu cầu đồng thời), **Then** chỉ lần đầu thành công; các lần còn lại bị từ chối vì xung đột và không trừ Xu lần hai.
4. **Given** người chơi chỉ đủ Xu cho một trong hai món, **When** hai yêu cầu mua hai món khác nhau đến cùng lúc, **Then** đúng một món được mua, món kia bị từ chối vì không đủ Xu và số dư không âm.
5. **Given** người chơi đang xem cửa hàng, **When** họ chọn xem thử một món (kể cả món chưa sở hữu), **Then** nhân vật của họ hiện món đó ngay mà không tốn Xu và không thay đổi nhân vật đã lưu.
6. **Given** người chưa đăng nhập (khách), **When** họ mở cửa hàng, **Then** họ xem được danh mục và giá, và xem thử được món bất kỳ trên một nhân vật mặc định chỉ gồm món miễn phí (không phải nhân vật của riêng họ); họ không mua được và nút mua dẫn tới đăng nhập.

---

### User Story 2 - Chỉnh và lưu nhân vật ở hồ sơ (Priority: P1)

Người chơi đã đăng nhập mở trang hồ sơ, thấy nhân vật lớn và trình chỉnh liệt kê các món miễn phí cùng các món đã sở hữu theo từng ô. Họ chọn món cho từng ô, xem kết quả ngay, rồi bấm lưu một lần để áp dụng toàn bộ thay đổi.

**Why this priority**: Mua đồ mà không mặc được thì vô nghĩa; hai câu chuyện này cùng nhau tạo thành giá trị cốt lõi và đều cần để nhân vật xuất hiện ở các nơi khác.

**Independent Test**: Chọn một kiểu tóc miễn phí khác và một món kính đã sở hữu, lưu; tải lại trang thì nhân vật giữ nguyên lựa chọn đó.

**Acceptance Scenarios**:

1. **Given** người chơi ở trang hồ sơ, **When** họ đổi nhiều ô rồi bấm lưu, **Then** tất cả thay đổi được lưu cùng lúc và nhân vật mới hiện sau khi tải lại.
2. **Given** trình chỉnh đang mở, **When** người chơi xem danh sách của một ô, **Then** họ chỉ thấy các món miễn phí và các món đã sở hữu của ô đó, không thấy món chưa mua.
3. **Given** ô tùy chọn (kính, râu, cử chỉ, hình in, màu nền), **When** người chơi bỏ chọn món ở ô đó rồi lưu, **Then** nhân vật không còn món ở ô đó.
4. **Given** ô bắt buộc (mắt, lông mày, miệng, mũi, tóc, áo), **When** người chơi cố lưu mà để trống ô đó, **Then** yêu cầu bị từ chối và nhân vật đã lưu không đổi.
5. **Given** một yêu cầu lưu chứa món chưa sở hữu (và không miễn phí) hoặc món đặt sai ô, **When** server nhận yêu cầu, **Then** toàn bộ yêu cầu bị từ chối, không lưu một phần nào.
6. **Given** người chơi chưa từng chỉnh nhân vật (kể cả mọi tài khoản tạo trước tính năng này), **When** họ mở hồ sơ, **Then** họ thấy một nhân vật tự sinh cố định theo tài khoản, chỉ dùng món miễn phí, và nhân vật đó không đổi giữa các lần mở.
7. **Given** người chơi đã sở hữu một hình in, **When** họ chọn hình in đó trong khi áo đang chọn không in được, **Then** giao diện vô hiệu hóa hình in và nói rõ lý do; nếu yêu cầu lưu vẫn tới được server thì bị từ chối (400, `graphic_incompatible_clothes`) và nhân vật đã lưu không đổi.

---

### User Story 3 - Thấy nhân vật trong phòng và bảng xếp hạng (Priority: P2)

Trong sảnh chờ, bảng điểm giữa trận, bảng điểm cuối trận và trang bảng xếp hạng XP, mỗi người chơi hiện kèm nhân vật nhỏ cạnh tên. Người chơi có tài khoản hiện nhân vật hiện tại của họ; khách hiện nhân vật tự sinh theo tên.

**Why this priority**: Cho nhân vật ý nghĩa xã hội (được người khác nhìn thấy), nhưng game vẫn chơi được nếu thiếu phần này.

**Independent Test**: Hai người chơi (một có tài khoản đã chỉnh nhân vật, một khách) cùng vào một phòng; sảnh chờ hiện nhân vật đã chỉnh cho người đầu và nhân vật tự sinh nền xám cho khách.

**Acceptance Scenarios**:

1. **Given** phòng có người chơi tài khoản và khách, **When** mở sảnh chờ, bảng điểm giữa trận, bảng điểm cuối trận, **Then** mỗi người có nhân vật nhỏ cạnh tên; khách có nhân vật tự sinh theo tên, chỉ dùng món miễn phí, nền xám.
2. **Given** một người chơi tài khoản đang ở trong phòng, **When** họ đổi nhân vật ở nơi khác rồi người khác tải lại bảng điểm, **Then** bảng điểm hiện nhân vật mới (nhân vật hiện tại, không phải bản chụp lúc vào phòng).
3. **Given** trận đang diễn ra, **When** một người chơi đổi đồ, **Then** điểm số, thứ hạng và thưởng của trận không bị ảnh hưởng.
4. **Given** trang bảng xếp hạng XP, **When** mở trang, **Then** mỗi tài khoản trong danh sách hiện nhân vật nhỏ cạnh tên.

---

### User Story 4 - Đồng thời giữa mua đồ và kết thúc trận (Priority: P2)

Khi người chơi đang mua đồ đúng lúc trận của họ kết thúc và được cộng Xu, cả hai thao tác phải cùng đúng: không mất Xu vừa kiếm, không trừ nhầm, số dư luôn khớp lịch sử giao dịch.

**Why this priority**: Đây là rủi ro sai số tiền tệ nhưng chỉ xảy ra ở điều kiện hiếm; cần đúng nhưng không phải luồng chính của người dùng.

**Independent Test**: Chạy đồng thời một lần mua đồ và một lần kết thúc trận cho cùng tài khoản; số dư cuối bằng số dư đầu + Xu thưởng − giá món đồ, và tổng lịch sử giao dịch bằng số dư.

**Acceptance Scenarios**:

1. **Given** người chơi có số dư X, **When** việc mua món giá P và việc cộng Xu thưởng R diễn ra đồng thời, **Then** số dư cuối là X + R − P (nếu đủ Xu để mua) và bằng tổng lịch sử giao dịch của họ.

---

### Edge Cases

- Mất kết nối ngay sau khi bấm mua: khi tải lại, trạng thái "đã sở hữu" và số dư phản ánh đúng kết quả cuối cùng (đã mua hoặc chưa), không ở trạng thái nửa chừng.
- Phiên đăng nhập hết hạn khi đang ở cửa hàng hoặc hồ sơ: thao tác mua/lưu bị từ chối và người chơi được đưa về đăng nhập; lựa chọn đang xem thử không cần giữ lại.
- Món đồ không tồn tại hoặc mã món đồ sai: bị từ chối là không tìm thấy, không ảnh hưởng số dư.
- Khách đổi tên giữa các phòng: nhân vật tự sinh đổi theo tên mới; hai khách trùng tên có thể trùng nhân vật (chấp nhận).
- Ô tùy chọn đang rỗng nhưng người chơi chưa lưu và xem thử một món: xem thử không thay đổi dữ liệu đã lưu.
- Danh mục có món miễn phí: bấm "mua" món miễn phí không áp dụng (không có nút mua), và yêu cầu mua món miễn phí gửi trực tiếp bị từ chối.
- Người chơi rời phòng giữa trận: nhân vật vẫn hiện đúng ở bảng điểm cuối trận như người chơi khác.

## Requirements *(mandatory)*

### Functional Requirements

**Danh mục**

- **FR-001**: Hệ thống MUST có một danh mục đồ gồm các ô: mắt, lông mày, miệng, mũi, tóc, áo, kính, râu, cử chỉ tay, hình in trên áo, màu nền. Mỗi món thuộc đúng một ô, có tên hiển thị, mức giá (hoặc miễn phí) và mã món đồ.
- **FR-002**: Số kiểu mỗi ô MUST khớp bộ hình đã chọn: mắt 5, lông mày 13, miệng 30, mũi 20, tóc 64, áo 25, kính 11, râu 12, cử chỉ 10, hình in 3, cộng khoảng 8 màu nền do dự án định nghĩa.
- **FR-003**: Món miễn phí cho mọi tài khoản MUST gồm toàn bộ mắt, lông mày, miệng, mũi; 8 kiểu tóc và 3 kiểu khăn trùm đầu (khăn quấn đầu và hai kiểu khăn hijab, thuộc ô tóc, vì là trang phục tôn giáo nên không được bán); 5 kiểu áo; và nền mặc định. Mọi món còn lại là món bán.
- **FR-004**: Món bán MUST thuộc một trong ba mức giá: Thường 50 Xu, Hiếm 150 Xu, Đặc biệt 400 Xu. Cử chỉ MUST là Hiếm, hình in MUST là Đặc biệt; mức của các món còn lại được chốt ở bước lập kế hoạch.
- **FR-005**: Danh mục (kể cả giá) MUST xem được công khai, không cần đăng nhập.

**Mua đồ**

- **FR-006**: Chỉ tài khoản đã đăng nhập MUST mua được đồ; yêu cầu mua của khách MUST bị từ chối.
- **FR-007**: Giá MUST do server quyết định; yêu cầu mua chỉ mang mã món đồ, không mang giá.
- **FR-008**: Khi mua thành công, hệ thống MUST trừ Xu, ghi một giao dịch Xu âm với lý do mua đồ trong cửa hàng và cấp quyền sở hữu vĩnh viễn, tất cả như một thao tác không thể xảy ra từng phần.
- **FR-009**: Hệ thống MUST từ chối mua món đã sở hữu với lỗi xung đột và MUST NOT trừ Xu lần hai, kể cả khi bấm hai lần liên tiếp hoặc gửi hai yêu cầu đồng thời.
- **FR-010**: Hệ thống MUST từ chối mua khi số dư nhỏ hơn giá, giữ nguyên số dư; số dư MUST NOT bao giờ âm, kể cả khi nhiều yêu cầu mua khác nhau đến đồng thời.
- **FR-011**: Hệ thống MUST từ chối mua món miễn phí và món không tồn tại.
- **FR-012**: Hệ thống MUST giữ bất biến: số dư Xu của mỗi tài khoản luôn bằng tổng các giao dịch Xu của tài khoản đó, kể cả khi mua đồ diễn ra đồng thời với việc cộng Xu cuối trận.
- **FR-013**: Món đã mua MUST NOT bán lại, hoàn Xu hoặc tặng cho tài khoản khác.

**Mặc đồ**

- **FR-014**: Mỗi ô MUST có tối đa một món được mặc. Các ô mắt, lông mày, miệng, mũi, tóc, áo MUST luôn có món; các ô kính, râu, cử chỉ, hình in, màu nền được để trống.
- **FR-015**: Server MUST từ chối việc lưu nhân vật nếu có món chưa sở hữu và không miễn phí, món đặt sai ô, hoặc ô bắt buộc để trống; khi từ chối, nhân vật đã lưu MUST giữ nguyên toàn bộ (không lưu một phần).
- **FR-016**: Việc lưu nhân vật MUST áp dụng tất cả thay đổi của các ô trong một lần lưu.
- **FR-017**: Chỉ tài khoản đã đăng nhập MUST lưu được nhân vật, và chỉ cho chính tài khoản đó.
- **FR-018**: Tài khoản chưa từng chỉnh (gồm mọi tài khoản tạo trước tính năng này) MUST hiện một nhân vật tự sinh cố định theo tài khoản, chỉ dùng món miễn phí; không cần cập nhật dữ liệu cũ. Nhân vật này MUST không đổi giữa các lần xem.
- **FR-019**: Khách MUST hiện nhân vật tự sinh cố định theo tên người chơi, chỉ dùng món miễn phí, nền xám. Hai khách trùng tên có thể có nhân vật giống nhau.
- **FR-028**: Hình in chỉ hiển thị được trên một số kiểu áo (áo `variant01` đến `variant10` của bộ hình, gọi là "áo in được"). Server MUST từ chối việc lưu nhân vật có hình in mà áo không thuộc tập áo in được (lỗi 400, mã lỗi `graphic_incompatible_clothes`) và giữ nguyên nhân vật đã lưu. Giao diện MUST vô hiệu hóa danh sách hình in và nêu lý do khi áo hiện chọn không in được. Tập áo in được là một hằng số duy nhất của danh mục, có test đối chiếu với bộ hình đúng phiên bản đã ghim.

**Hiển thị**

- **FR-020**: Trang hồ sơ MUST hiện nhân vật lớn và trình chỉnh theo từng ô, chỉ liệt kê món miễn phí và món đã sở hữu, kèm nút lưu.
- **FR-021**: Trang cửa hàng MUST hiện danh sách đồ theo ô và mức giá, đánh dấu món đã sở hữu, hiện số dư Xu của người đang đăng nhập, cho xem thử món bất kỳ mà không đổi dữ liệu đã lưu (người đã đăng nhập xem thử trên nhân vật của mình; khách xem thử trên một nhân vật mặc định chỉ gồm món miễn phí), và mua món chưa sở hữu (chỉ người đã đăng nhập).
- **FR-022**: Sảnh chờ, bảng điểm giữa trận, bảng điểm cuối trận và trang bảng xếp hạng XP MUST hiện nhân vật nhỏ cạnh tên mỗi người chơi.
- **FR-023**: Nhân vật hiện trong phòng MUST là nhân vật hiện tại của tài khoản tại thời điểm xem, không phải bản chụp lúc vào phòng.
- **FR-024**: Đổi đồ trong lúc trận diễn ra MUST NOT ảnh hưởng điểm số, thứ hạng hay thưởng của trận.
- **FR-025**: Các màn có dữ liệu từ server (cửa hàng, hồ sơ, danh sách) MUST xử lý đủ trạng thái đang tải, lỗi và rỗng; nút mua và nút lưu MUST bị vô hiệu hóa trong lúc đang gửi.
- **FR-026**: Giao diện MUST dùng được trên điện thoại.

**Ngoài phạm vi**

- **FR-027**: Tính năng này MUST NOT bao gồm màn chơi kiểu lớp học, bục vinh danh (thuộc Tính năng 3) và nút Đăng xuất trên header của hồ sơ, bảng xếp hạng (việc treo riêng).

### Key Entities *(include if feature involves data)*

- **Món đồ (Item)**: một lựa chọn cho một ô của nhân vật. Thuộc tính: ô, mã định danh nhìn thấy được bởi client, tên hiển thị, mức giá (miễn phí / Thường / Hiếm / Đặc biệt) và giá Xu tương ứng. Danh mục cố định, quản lý cùng cơ sở dữ liệu.
- **Quyền sở hữu (Ownership)**: quan hệ giữa một tài khoản và một món bán đã mua. Mỗi cặp (tài khoản, món) tối đa một lần; không có trạng thái gỡ bỏ.
- **Nhân vật (Avatar)**: tập hợp tối đa một món cho mỗi ô của một tài khoản. Tài khoản chưa từng lưu thì không có bản ghi và nhân vật được tự sinh khi cần.
- **Tài khoản và giao dịch Xu (có sẵn từ Tính năng 1)**: số dư Xu và lịch sử giao dịch; việc mua đồ thêm giao dịch âm lý do "mua đồ trong cửa hàng".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Người chơi có đủ Xu mua và mặc một món mới trong dưới 1 phút kể từ lúc mở cửa hàng (mua, sang hồ sơ, chọn, lưu).
- **SC-002**: Trong 100% các lần thử mua đồng thời (cùng món hoặc hai món khi chỉ đủ Xu cho một), số dư cuối không âm và Xu bị trừ đúng bằng giá các món mua thành công, không món nào bị tính tiền hai lần.
- **SC-003**: Với mọi tài khoản tại mọi thời điểm kiểm tra, số dư Xu bằng tổng lịch sử giao dịch Xu của tài khoản đó, kể cả sau thử nghiệm mua đồng thời với kết thúc trận.
- **SC-004**: 100% yêu cầu lưu nhân vật có món chưa sở hữu, sai ô, thiếu ô bắt buộc hoặc hình in trên áo không in được bị từ chối và nhân vật đã lưu không đổi.
- **SC-005**: Mọi tài khoản hiện có trước tính năng này hiện được một nhân vật hợp lệ (không lỗi, không ảnh trống) ngay lần mở đầu tiên mà không cần di chuyển dữ liệu.
- **SC-006**: Nhân vật hiện đúng ở cả bốn nơi (sảnh chờ, bảng điểm giữa trận, bảng điểm cuối trận, bảng xếp hạng) và ở hồ sơ/cửa hàng với cùng một hình cho cùng một tài khoản.
- **SC-007**: Với phòng 50 người chơi, bảng điểm và sảnh chờ hiện đủ nhân vật mà người chơi không nhận ra độ trễ thêm so với trước tính năng.
- **SC-008**: Mọi mã món đồ trong danh mục đều ứng với một kiểu có thật của bộ hình ở đúng phiên bản đã chọn; kiểm tra tự động xác nhận điều này.

## Assumptions

- Người dùng chính là người chơi có tài khoản (đã có từ Tính năng 1); khách vẫn chơi như cũ nhưng không mua hay chỉnh nhân vật.
- Xu và lịch sử giao dịch Xu đã có từ Tính năng 1, kể cả lý do giao dịch "mua đồ trong cửa hàng". Tính năng này chỉ sử dụng chúng.
- Ô để trống cho các ô tùy chọn nghĩa là không có món đó trên nhân vật; riêng "màu nền" để trống hiện nền mặc định.
- Tính năng này KHÔNG làm giao diện lịch sử giao dịch Xu; hồ sơ vẫn chỉ hiện lịch sử thưởng trận như Tính năng 1.
- Chưa có đồ tặng khi đăng ký ngoài phần miễn phí; không có sự kiện, giảm giá hay đồ giới hạn thời gian.
- Danh mục không đổi sau khi phát hành trong phạm vi tính năng này; thêm món mới là một thay đổi riêng.
- Trang phục tôn giáo (khăn quấn đầu, khăn hijab) luôn miễn phí để không ai phải trả Xu cho trang phục tôn giáo của mình; tên hiển thị của chúng trung tính ("Khăn quấn đầu", "Khăn trùm đầu 1", "Khăn trùm đầu 2").
- Phân bổ mức giá cho các món bán ngoài cử chỉ (Hiếm) và hình in (Đặc biệt) do bước lập kế hoạch đề xuất, tuân thủ ba mức giá ở FR-004.
- Các màn hình mới tuân theo hệ thiết kế "true-black-flat" của dự án; nhân vật là hình đen trắng nên không vi phạm quy tắc màu nhấn.

### Ràng buộc kỹ thuật đã chốt (dùng ở bước lập kế hoạch)

- Thư viện mới đã được duyệt: DiceBear core + kiểu Notionists (DiceBear styles 10.6.0), cài qua npm, vẽ SVG trên frontend. Phiên bản MUST được ghim cứng chính xác (không dùng `^`/`~`) vì tên variant là dữ liệu lưu trong cơ sở dữ liệu và nâng cấp có thể đổi hình. KHÔNG dùng API HTTP công khai `api.dicebear.com` (chỉ miễn phí phi thương mại, không cam kết hoạt động).
- Thêm `THIRD_PARTY_NOTICES.md` ở gốc repo: tên gói, phiên bản, giấy phép (CC0 1.0 cho Notionists; MIT cho code DiceBear), liên kết nguồn (DiceBear và Notionists gốc của Zoish).
- Backend chỉ lưu và trả cặp ô → `variantKey` (chuỗi). Không gửi ảnh qua API hay SignalR.
- Danh mục đồ (cả món miễn phí) nằm trong cơ sở dữ liệu, seed bằng migration, để server kiểm tra được mọi `variantKey`. Cần một test so mọi `variantKey` đã seed với file định nghĩa style của đúng phiên bản đã ghim.
- Mua đồ chạy trong một transaction: khóa dòng `Users` bằng `SELECT ... FOR UPDATE` (theo thứ tự `Id` như quy tắc trong CLAUDE.md), kiểm tra sở hữu (ràng buộc duy nhất theo người dùng + món đồ), kiểm tra số dư, trừ `CoinBalance`, ghi `CoinTransactions` số âm với `Reason = ShopPurchase` (đã có). Giữ bất biến `CoinBalance` = tổng `CoinTransactions`.
- Test đồng thời bắt buộc: hai request mua cùng món; hai request mua hai món khi chỉ đủ Xu cho một món; có test kiểm độ nhạy (tạm bỏ `FOR UPDATE` thì test phải fail).
- Mua đồ đồng thời với kết thúc trận (cộng Xu) phải không mất cập nhật.
- Bảng mới trên Supabase phải bật RLS; migration production sinh bằng `scripts/migrate-prod.ps1`, chạy TRƯỚC khi merge.
