# Research: Giới hạn AI theo ngày

**Spec**: [spec.md](./spec.md)

## R1. Nơi lưu bộ đếm

- **Chọn**: bảng PostgreSQL `AiUsageDaily` (qua EF Core migration).
- **Lý do**: Render ngủ/redeploy làm mất RAM; Postgres là nơi dữ liệu bền duy nhất và đã có sẵn. Không thêm Redis (CLAUDE.md cấm thêm hạ tầng/thư viện khi chưa hỏi).
- **Loại**: `PartitionedRateLimiter` trong RAM (lỗ hổng 1); Redis (thư viện/hạ tầng mới); bộ đếm trong cột của bảng `Users` (khóa dòng `Users` dùng cho XP/Xu, xem CLAUDE.md, sẽ kéo thêm tranh chấp; và không có chỗ cho dòng global).

## R2. Cột `Scope` kiểu chuỗi thay vì `UserId` nullable

- **Chọn**: `Scope varchar(50) NOT NULL` = `'global'` | `'user:{guid:D}'` (5 + 36 = 41 ký tự), khóa chính `(Day, Scope)`.
- **Lý do**: câu giữ chỗ dựa vào `INSERT ... ON CONFLICT (Day, Scope)`. Nếu dùng `UserId uuid NULL` cho dòng global, `(Day, NULL)` không bao giờ xung đột trong unique index của PostgreSQL (mọi NULL được coi là khác nhau) → mỗi lần gọi lại chèn thêm một dòng global mới và trần global không bao giờ chặn. Cách vá (`NULLS NOT DISTINCT`, PG15+; hoặc partial unique index; hoặc user "giả" Guid.Empty) đều phức tạp hơn hoặc phụ thuộc phiên bản Postgres (Supabase/Docker có thể khác nhau).
- **Đánh đổi**: không có khóa ngoại tới `Users` (chuỗi). Chấp nhận: bảng này là bộ đếm, không phải quan hệ; xóa user không cần cascade vì dữ liệu theo ngày và tự vô nghĩa.
- **Loại**: hai bảng riêng (`AiUsageGlobalDaily`, `AiUsageUserDaily`) — thêm hai migration/entity, và mất khả năng dùng cùng một câu SQL cho cả hai phạm vi.

## R3. Câu lệnh giữ chỗ nguyên tử

```sql
INSERT INTO "AiUsageDaily" ("Day", "Scope", "Count")
VALUES (@day, @scope, 1)
ON CONFLICT ("Day", "Scope")
DO UPDATE SET "Count" = "AiUsageDaily"."Count" + 1
WHERE "AiUsageDaily"."Count" < @limit
RETURNING "Count";
```

- Có dòng trả về → đã giữ chỗ; không có dòng → dòng đã chạm trần (điều kiện `WHERE` của `DO UPDATE` sai thì không cập nhật và không `RETURNING`).
- Đọc-rồi-ghi (SELECT count, if < limit then UPDATE) KHÔNG nguyên tử: N request cùng đọc `K-1` rồi cùng ghi → vượt trần. Câu trên gộp kiểm tra và tăng vào một câu lệnh; khi hai request tranh cùng dòng, request sau chờ khóa dòng của request trước rồi đánh giá lại `WHERE` với giá trị mới (READ COMMITTED, hành vi `ON CONFLICT DO UPDATE` của PostgreSQL).
- Lần đầu trong ngày: nhánh `INSERT` không qua `WHERE`, tạo `Count = 1`. Hợp lệ vì cấu hình bắt buộc `limit ≥ 1` (FR-011). Hai request cùng chèn dòng mới: một thắng, một chờ unique index rồi rơi vào nhánh `DO UPDATE` + `WHERE`.
- **Không dùng** `Database.SqlQuery<int>` của EF: EF bọc câu thô trong `SELECT ... FROM (<sql>)`, không hợp lệ với `INSERT ... RETURNING`. Dùng `DbCommand` từ connection của `DbContext`, bọc trong transaction của chính repository (cùng cách `InsertWithRoomLockAsync` / các repository khác đang làm — kiểm tra khi code).

## R4. Hai dòng trong một transaction, thứ tự global → user

- Transaction: (1) giữ chỗ dòng global; không có dòng → rollback, báo `Global`; (2) giữ chỗ dòng user; không có dòng → rollback (hoàn tác luôn lần tăng global ở bước 1), báo `User`; (3) COMMIT.
- Cùng thứ tự khóa ở mọi giao dịch giữ chỗ → hai giữ chỗ không thể khóa chéo (A giữ global đợi user-A, B giữ... B phải đợi global trước khi chạm bất kỳ dòng user nào).
- Ưu tiên báo `Global` khi cả hai cùng hết (global kiểm trước). Khi user đã chạm trần cá nhân, yêu cầu vẫn tạm khóa dòng global vài ms rồi rollback; chấp nhận vì tổng lượt ≤ 300/ngày.
- **Loại**: chỉ kiểm user trước rồi global (nếu global từ chối thì phải hoàn user — thêm một bước bù trừ dễ lệch); một câu CTE hai INSERT (nhìn gọn nhưng khó đọc/khó kiểm thứ tự khóa, và `ON CONFLICT` trong CTE không cho "tất cả hoặc không gì" mà không có transaction).

## R5. Hoàn lượt (chỉ dòng user)

```sql
UPDATE "AiUsageDaily"
SET "Count" = GREATEST("Count" - 1, 0)
WHERE "Day" = @day AND "Scope" = @scope;
```

- Truyền `Day` lấy từ kết quả giữ chỗ, KHÔNG gọi lại `TimeProvider`: giữ chỗ 23:59:59 và hoàn sau 00:00 phải trừ đúng dòng ngày cũ.
- **Không hoàn global** (quyết định A): trần global là ngân sách gọi Gemini; lần gọi thất bại vẫn tiêu quota phía Gemini (lỗi định dạng đã tốn token; lỗi 429/5xx cũng có thể tính vào RPM/RPD). Hoàn sẽ làm trần global đo sai.
- **Không hoàn khi bị hủy**: nếu hoàn, kẻ xấu gửi request rồi đóng kết nối ngay sau khi server đã gọi Gemini sẽ có lượt "miễn phí" vô hạn trong khi quota vẫn bị tiêu.
- **Hoàn dùng `CancellationToken.None`**: request thất bại có thể đã bị hủy; nếu hoàn dùng token của request thì chính nó bị hủy và lượt không bao giờ được trả. Hoàn lỗi (DB sập) thì ghi log và để lỗi gốc đi tiếp, không che lỗi gốc.
- **Không deadlock**: hoàn là một câu UPDATE đơn lẻ chạm đúng một dòng user và không giữ khóa nào khác. Giữ chỗ giữ khóa global và chờ khóa dòng user; hoàn chỉ giữ dòng user và không chờ ai → đồ thị chờ không có chu trình. Nếu sau này một thao tác cần đụng cả hai dòng, bắt buộc theo thứ tự global → user.
- `GREATEST(..., 0)` cùng `CHECK ("Count" >= 0)` là lưới an toàn; mỗi request chỉ hoàn tối đa một lần theo cấu trúc code.

## R6. Thời điểm giữ chỗ và hoàn lượt trong `GenerateAsync`

- Giữ chỗ **sau** các kiểm tra rẻ (phòng, chủ phòng, Draft, `MAX_QUESTIONS_PER_ROOM`, danh sách câu hiện có) và **trước** `GenerateValidCandidatesAsync` (quyết định D).
- Commit giữ chỗ trước khi gọi Gemini (quyết định C): Gemini chậm đến 30 giây; giữ khóa dòng global suốt thời gian đó sẽ làm mọi request generate toàn hệ thống xếp hàng.
- Hoàn chỉ bắt `BadGatewayException` và `AiUnavailableException`. Không bắt `OperationCanceledException` (không hoàn), không bắt `ConflictException` sau Gemini (Gemini đã trả kết quả hợp lệ — xem Điểm cần chốt 1).

## R7. Tính ngày giờ Việt Nam

- **Chọn**: `DateOnly.FromDateTime(utcNow.UtcDateTime.AddHours(7))`; `resetAt` = `new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7))`. Hàm thuần trong Domain, nhận `DateTimeOffset` làm tham số.
- **Lý do**: Việt Nam không có giờ mùa hè, độ lệch +7 cố định. `TimeZoneInfo.FindSystemTimeZoneById` có ID khác nhau giữa Windows (`SE Asia Standard Time`) và Linux (`Asia/Ho_Chi_Minh`), và container Render có thể thiếu tzdata → rủi ro chạy khác môi trường (CLAUDE.md: "Code không phân biệt môi trường").
- Test ranh giới: `16:59:59Z` → ngày D (23:59:59 VN), `17:00:00Z` → ngày D+1 (00:00:00 VN).

## R8. Phản hồi 429 và cách client phân biệt

- `AiDailyLimitException` (Application/Common) mang `Code` và `ResetAt`; `GlobalExceptionHandler` map sang 429 và đặt `Extensions["code"]`, `Extensions["resetAt"]` (cùng cách `code` hiện có). Controller không có logic.
- 429 của policy 10/giờ thêm `code = AI_RATE_LIMIT` trong `OnRejected`, chỉ khi endpoint dùng policy `generate-questions` (các policy auth/join giữ nguyên, không gắn `code` mới ngoài phạm vi).
- Frontend phân biệt 3 loại bằng `code` thay vì đoán theo chuỗi thông báo.

## R9. Phía frontend

- Số lượt là dữ liệu server → TanStack Query (`['ai-quota']`), không sao chép vào `useState` (CLAUDE.md).
- Làm mới (invalidate) sau mỗi lần generate, kể cả khi lỗi, vì server có thể đã trừ/hoàn lượt.
- Nút tắt chỉ để báo nhanh; server vẫn là nguồn sự thật (nhiều tab/thiết bị có thể lệch).
- Lấy số lượt lỗi → KHÔNG chặn nút Sinh (người dùng vẫn thử được, server quyết định).
- Tách thành phần hiển thị số lượt ra `AiQuotaBadge.tsx` để `GenerateQuestionsPanel.tsx` không vượt ~150 dòng.

## R10. Cấu hình và triển khai

- `Ai:DailyLimitPerUser` (mặc định 20), `Ai:DailyLimitGlobal` (mặc định 300); biến môi trường `Ai__DailyLimitPerUser`, `Ai__DailyLimitGlobal`. Không bí mật nên không cần user-secrets, nhưng vẫn đọc từ cấu hình như mọi biến khác và liệt kê trong `.env.example`.
- `ProductionConfigValidator`: không bắt buộc có mặt (đã có mặc định); chỉ kiểm tra nếu có đặt thì parse được thành số nguyên > 0. Sai giá trị trên mọi môi trường (không riêng production) làm backend không khởi động để tránh `limit = 0` (nhánh INSERT lần đầu sẽ lách trần).
- Migration lên production chạy thủ công, SQL idempotent, TRƯỚC khi merge (xem plan.md).
