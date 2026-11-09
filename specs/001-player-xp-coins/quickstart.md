# Quickstart: kiểm chứng tính năng XP và Xu

Hướng dẫn chạy để chứng minh tính năng đúng từ đầu đến cuối. Không chứa mã triển khai. Dữ liệu/hợp đồng xem [data-model.md](data-model.md), [contracts/](contracts/).

## Điều kiện

- `docker compose up -d` (PostgreSQL dev, cổng 5433), có `.env` với `POSTGRES_PASSWORD`.
- Backend: `dotnet ef database update` (chỉ ở dev) rồi chạy API. Frontend: `npm run dev` với `VITE_API_URL`.
- Ba tài khoản: `host@x.com` (chủ phòng), `a@x.com`, `b@x.com`; hai trình duyệt/hồ sơ ẩn danh cho A và B, một cho khách.

## 1. Tự động

```text
dotnet build backend/QuizBattle.sln
dotnet test  backend/QuizBattle.sln          # unit + (nếu có QUIZBATTLE_TEST_DB) tích hợp Postgres
npm --prefix frontend run build
npm --prefix frontend test
```

Test tích hợp: đặt `QUIZBATTLE_TEST_DB` là chuỗi kết nối tới DB docker (role có quyền `CREATEDB`). Mỗi lần chạy tự tạo một database tạm `qb_test_<guid>`, chạy toàn bộ migration, rồi `DROP DATABASE ... WITH (FORCE)` khi xong; không đụng dữ liệu của DB dev. Không đặt biến thì các test đó hiển thị là bỏ qua, không phải lỗi.

## 2. Kịch bản thủ công (khớp spec)

| # | Làm | Kỳ vọng |
|---|---|---|
| 1 | Host tạo phòng 3 câu, mở sảnh. A và B đăng nhập, vào `/join` | `/join` điền sẵn tên và có dòng báo tính thưởng; sảnh chờ hiện cấp cạnh tên A, B; khách không có cấp |
| 2 | Cả A, B trả lời ít nhất 1 câu, để trận chạy hết câu cuối | màn kết thúc A và B: XP = điểm/10, Xu = điểm/100 + thưởng hạng; người đứng nhất +30 |
| 3 | Mở `/profile` của A | cấp, tổng XP, thanh tiến trình, số dư Xu, trận vừa rồi trong lịch sử, khớp màn kết thúc |
| 4 | Mở `/ranking` | A, B có mặt theo tổng XP; tài khoản 0 XP không có mặt |
| 5 | Làm mới trang của A ngay sau khi trận kết thúc | vẫn thấy cùng khối thưởng; số dư Xu và XP không tăng thêm |
| 6 | Chạy lại trận với chỉ A là tài khoản (B là khách) | không ai nhận thưởng; màn kết thúc nói chưa đủ 2 tài khoản |
| 7 | Chạy trận A, B nhưng B không trả lời câu nào | không ai nhận; B thấy "chưa trả lời câu nào" |
| 8 | Host tự vào phòng làm người chơi cùng A, B | host không nhận, màn host nói "chủ phòng không nhận thưởng"; A, B nhận |
| 9 | Khách đứng nhất, A nhì | khách không nhận; A nhận Xu thưởng hạng 2 (+20) |
| 10 | Điểm toàn 0 (trả sai hết) | `awarded` nhưng XP/Xu 0, ghi chú "không có thưởng hạng" |
| 11 | A vào phòng ở tab 1, sau đó mở `/join` ở tab 2 (cùng phòng, đang `Lobby`/`InProgress`) | vẫn một Player (không thêm dòng ở bảng người chơi), tab 1 nhận thông báo đã vào từ thiết bị khác và không trả lời được nữa |
| 12a | Đăng nhập, để token hết hạn (rút ngắn `ExpiryMinutes` khi thử, hoặc sửa `expiresAt` trong localStorage `quizbattle.auth` về quá khứ), tải lại rồi mở `/join` | hộp thoại "Phiên đăng nhập đã hết hạn" hiện ngay, hai lựa chọn; không tự thành khách; nút vào phòng bị vô hiệu cho tới khi chọn |
| 12b | Mở `/join` khi còn hạn, sau đó làm token hết hạn (sửa `expiresAt`) rồi bấm "Vào phòng" | không gửi request; hộp thoại hiện; không có người chơi khách nào được tạo |
| 12c | Hai tab đang mở, tab 1 tải lại sau khi hết hạn (xóa token), chuyển sang tab 2 mở `/join` rồi bấm vào phòng | tab 2 cũng hiện hộp thoại (cờ chia sẻ qua localStorage) |
| 12d | Làm token vẫn còn hạn ở client nhưng sai ở server (đổi 1 ký tự chữ ký) rồi vào phòng | server trả 401 `token_invalid`; hộp thoại hiện; hộp còn nguyên sau khi `user` thành null |
| 13 | Chọn "Vào làm khách" từ hộp thoại | hộp đóng, cờ xóa, vào được như khách, không có thưởng; chọn "Đăng nhập lại" rồi đăng nhập xong thì cờ cũng xóa và quay lại `/join` |
| 14 | Sau trận có thưởng, chủ phòng xóa phòng `Finished` | `/profile` của A, B vẫn có trận đó (mã phòng, chủ đề, thời điểm); Xu và XP không đổi; hai truy vấn bất biến ở mục 3 vẫn trả 0 dòng |

## 3. Kiểm tra bất biến trong DB

```sql
-- Số dư Xu khớp sổ cái (phải trả 0 dòng)
SELECT u."Id" FROM "Users" u
LEFT JOIN (SELECT "UserId", SUM("Amount") s FROM "CoinTransactions" GROUP BY "UserId") t ON t."UserId" = u."Id"
WHERE u."CoinBalance" <> COALESCE(t.s, 0);

-- Tổng XP khớp thưởng (phải trả 0 dòng)
SELECT u."Id" FROM "Users" u
LEFT JOIN (SELECT "UserId", SUM("Xp") s FROM "MatchRewards" GROUP BY "UserId") r ON r."UserId" = u."Id"
WHERE u."TotalXp" <> COALESCE(r.s, 0);

-- Không Player nào có hai thưởng (phải trả 0 dòng)
SELECT "PlayerId" FROM "MatchRewards" WHERE "PlayerId" IS NOT NULL GROUP BY "PlayerId" HAVING COUNT(*) > 1;
```

## 4. Đồng thời (đã kết thúc chỉ một lần)

Với phòng ở câu cuối đã `EndedAt`, bắn đồng thời `NextQuestion` của host và chờ timer tự chuyển (hoặc chạy test `NextQuestion_TwoConcurrentCallsAtFinalQuestion_RewardedExactlyOnce`). Kỳ vọng: một lần `GameEnded`, bất biến ở mục 3 vẫn đúng.

## 5. Migration production (bắt buộc trước khi merge)

1. Trên một DB dev trống, áp migration **trước** tính năng này (dừng ở `UseCaseInsensitivePlayerName`), rồi thêm dữ liệu cũ: 1 `User`, 1 `Room` `Finished`, vài `Player` khách (`UserId` NULL).
2. Chạy `scripts/migrate-prod.ps1` (không truyền `-ConnectionString` để chỉ sinh `scripts/out/migrate-prod.sql`), xem SQL: mọi migration bọc kiểm tra `__EFMigrationsHistory`.
3. Áp file SQL bằng `psql` **hai lần** liên tiếp: lần hai không lỗi, không thay đổi gì.
4. Kiểm tra: dữ liệu cũ nguyên vẹn; `Users.TotalXp = 0`, `CoinBalance = 0`; `MatchRewards`/`CoinTransactions` rỗng; có 2 CHECK trên `Users`, index lọc `IX_Users_TotalXp` và `IX_Players_RoomId_UserId`; không có `MatchRewards` cho phòng `Finished` cũ.
5. Thử vi phạm: `UPDATE "Users" SET "CoinBalance" = -1` phải lỗi CHECK.
6. Thứ tự lên production: áp SQL lên Supabase trước, rồi mới deploy backend mới.
