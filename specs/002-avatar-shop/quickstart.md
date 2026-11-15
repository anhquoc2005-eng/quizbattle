# Quickstart: kiểm chứng tính năng nhân vật và cửa hàng

Hướng dẫn chạy để chứng minh tính năng đúng từ đầu đến cuối. Không chứa mã triển khai. Dữ liệu: [data-model.md](data-model.md); hợp đồng: [contracts/](contracts/); quyết định: [research.md](research.md).

## Điều kiện

- `docker compose up -d` (PostgreSQL dev, cổng 5433), `.env` có `POSTGRES_PASSWORD`.
- Frontend: `npm ci` trong `frontend/` (cài hai gói DiceBear ghim `10.6.0`); `VITE_API_URL` đặt như hiện nay.
- Backend: `dotnet ef database update` (chỉ ở dev) rồi chạy API.
- Tài khoản: `host@x.com` (chủ phòng), `a@x.com`, `b@x.com`. `a@x.com` cần ≥ 400 Xu để thử đủ ba mức giá: cộng tạm bằng chơi vài trận, hoặc chèn dòng `CoinTransactions` `MatchReward` kèm tăng `CoinBalance` **trong cùng một transaction** (chỉ ở DB dev, để giữ bất biến).

## 1. Tự động

```text
dotnet build backend/QuizBattle.sln
dotnet test  backend/QuizBattle.sln          # unit + (có QUIZBATTLE_TEST_DB) tích hợp Postgres
npm --prefix frontend run build
npm --prefix frontend test
npm --prefix frontend run lint
```

Test tích hợp tạo DB tạm `qb_test_<guid>` như Tính năng 1 (xem [quickstart 001](../001-player-xp-coins/quickstart.md)). Test đồng bộ danh mục với file style cần `frontend/node_modules` (`npm ci`); thiếu thì **fail** với thông báo rõ (chỉ bỏ qua khi đặt `QB_SKIP_STYLE_SYNC=1`).

Nhóm test chính phải có và xanh:

| Nhóm | Ví dụ |
|---|---|
| Danh mục | 201 món, 85 miễn phí / 116 bán, 53/52/11 theo mức, giá 50/150/400; cử chỉ Hiếm, hình in Đặc biệt; `hair.variant61`..`63` miễn phí; tổng giá mua hết 14.850 Xu |
| Đồng bộ style | mọi `variantKey` đã seed == khóa `components.*.variants` của `notionists.min.json` đúng phiên bản `10.6.0` |
| Nhân vật mặc định | xác định, chỉ món miễn phí, đủ ô bắt buộc, khách không phân biệt hoa thường, giá trị cố định (golden) |
| Quy tắc lưu | thiếu ô bắt buộc, sai ô, món chưa sở hữu, món lạ, hình in trên áo ngoài `variant01`..`10` (`graphic_incompatible_clothes`): từ chối cả yêu cầu |
| Mua đồ | trừ đúng Xu, ghi sổ cái âm `ShopPurchase`, thứ tự kiểm (404 → 400 → 409 sở hữu → 409 số dư) |
| Đồng thời | xem bảng ở [plan.md](plan.md) điểm 6; gồm mua trùng, hai món chỉ đủ Xu một món, mua cùng kết thúc trận |
| Độ nhạy | `Purchase_WithoutUserRowLock_LosesUpdate_…` phá được bất biến khi tắt khóa |
| RLS | `pg_class.relrowsecurity = true` cho ba bảng mới |
| Truy vấn phòng | số lệnh SQL bằng nhau cho phòng 2 người và 50 người |
| Frontend | tập áo in được == tập áo cho SVG khác nhau khi thêm hình in (đúng phiên bản ghim); dựng tùy chọn DiceBear (ô trống → xác suất 0, nền trống → `d4d4d4`), áo in được, cấm chuỗi `api.dicebear.com`, `package.json` ghim chính xác (không `^`/`~`) hai gói DiceBear, các màn Cửa hàng/Hồ sơ có đủ tải/lỗi/rỗng |

## 2. Kịch bản thủ công (khớp spec)

| # | Làm | Kỳ vọng |
|---|---|---|
| 1 | Chưa đăng nhập, mở `/shop` | thấy danh mục và giá, xem thử món trên nhân vật mặc định chỉ có món miễn phí; nút mua dẫn tới đăng nhập (US1-6) |
| 2 | Đăng nhập `a@x.com` (chưa từng chỉnh), mở `/profile` | thấy nhân vật mặc định, cùng hình mỗi lần tải lại và sau khi đăng nhập thiết bị khác; chỉ liệt kê món miễn phí (US2-6) |
| 3 | `/shop`: chọn món Thường, xem thử, mua | Xu giảm đúng 50; món hiện "đã sở hữu"; tải lại vẫn vậy; dữ liệu có thêm đúng một giao dịch `-50` `ShopPurchase` (US1-1) |
| 4 | Bấm mua món vừa mua lần nữa (hoặc bấm đúp nhanh) | `409 already_owned`, Xu không trừ lần hai (US1-3) |
| 5 | Mua món giá lớn hơn số dư | bị từ chối `insufficient_coins`, số dư không đổi (US1-2) |
| 6 | `/profile`: đổi tóc sang món vừa mua, thêm một cử chỉ đã mua, bỏ chọn kính, bấm Lưu | tải lại thấy đúng nhân vật; ô bắt buộc không thể để trống trên giao diện (US2-1,3,4) |
| 7 | Gửi tay `PUT /api/users/me/avatar` với một món chưa sở hữu | `409 item_not_owned`, nhân vật cũ nguyên vẹn; thiếu `hair` → `400` (US2-4,5) |
| 8 | Chọn hình in rồi mặc áo `variant11` trở lên; hoặc gửi tay `PUT` có hình in kèm áo `variant14` | giao diện vô hiệu hóa hình in và nêu lý do; `PUT` trả `400 graphic_incompatible_clothes`, nhân vật cũ nguyên vẹn (FR-028) |
| 9 | Phòng có `a`, `b` (tài khoản) và một khách: mở sảnh chờ | mỗi người có nhân vật nhỏ cạnh tên; khách có nhân vật tự sinh nền xám (US3-1) |
| 10 | Trong khi trận chạy, `a` đổi đồ ở tab khác | điểm và hạng không đổi; bảng điểm câu kế tiếp hiện nhân vật mới (US3-2,3) |
| 11 | Kết thúc trận, xem bảng điểm cuối trận và `/ranking` | có nhân vật nhỏ ở mọi dòng (US3-1,4) |
| 12 | Mở DevTools → Network khi dùng cả app | không có yêu cầu nào tới `api.dicebear.com`; chunk `notionists` (≈ 111 KB gzip) chỉ tải khi vào màn có nhân vật, không nằm trong bundle chính (đối chiếu số trước/sau ở báo cáo Đợt D) |

## 3. Truy vấn bất biến (chạy trên DB sau khi thử)

```sql
-- Số dư phải bằng tổng sổ cái (không dòng nào trả về):
SELECT u."Id" FROM "Users" u
LEFT JOIN (SELECT "UserId", SUM("Amount") s FROM "CoinTransactions" GROUP BY "UserId") c ON c."UserId" = u."Id"
WHERE u."CoinBalance" <> COALESCE(c.s, 0);

-- Mỗi lần mua có đúng một quyền sở hữu và ngược lại (không dòng nào trả về):
SELECT ct."Id" FROM "CoinTransactions" ct
LEFT JOIN "UserAvatarItems" ui ON ui."Id" = ct."UserAvatarItemId"
WHERE ct."Reason" = 'ShopPurchase' AND (ui."Id" IS NULL OR ui."UserId" <> ct."UserId");

-- Danh mục: 201 món, 85 miễn phí:
SELECT count(*), count(*) FILTER (WHERE "Tier" = 'Free') FROM "AvatarItems";

-- RLS bật cho ba bảng mới (cả ba dòng đều true):
SELECT relname, relrowsecurity FROM pg_class
WHERE relname IN ('AvatarItems','UserAvatars','UserAvatarItems');
```

## 4. Đồng thời (kiểm tay thêm cho test tự động)

1. Hai cửa sổ ẩn danh đăng nhập cùng `a@x.com`, mỗi cửa sổ mở `/shop` ở cùng một món; bấm Mua gần như cùng lúc → đúng một thành công, cửa sổ kia nhận "đã sở hữu".
2. Số dư 60 Xu: mở hai món 50 Xu ở hai cửa sổ, bấm cùng lúc → một thành công, một "không đủ Xu"; số dư 10.
3. **Kiểm độ nhạy bằng tay**: tạm chú thích câu `FOR UPDATE` của lệnh khóa `Users` trong `AvatarRepository`, chạy `dotnet test --filter Purchase_`: test `Purchase_TwoItemsConcurrently_…`/`Purchase_ConcurrentWithMatchEnd_…` phải **đỏ** (hoặc kiểm độ nhạy tự động báo "bất biến bị phá"); hoàn nguyên, chạy lại phải xanh. Không commit bản đã chú thích.

## 5. Migration production

Làm theo mục 6 của [data-model.md](data-model.md): kiểm trên DB riêng (không phải DB dev), áp SQL do `scripts/migrate-prod.ps1` sinh bằng `psql` **hai lần**, rồi mới chạy lên Supabase (Session pooler, cổng 5432) **trước khi merge**. Sau khi áp, kiểm: 201 dòng `AvatarItems`, RLS bật cho ba bảng mới, `GET /api/shop/items` trả 201 phần tử.
