# Contract: REST API (nhân vật và cửa hàng)

Mọi lỗi theo ProblemDetails `{ type, title, status, detail?, errors? }` **cộng trường mở rộng `code`** (chuỗi máy đọc được, `snake_case`) cho các lỗi của tính năng này. `GlobalExceptionHandler` đã ghi `code` cho 401; mở rộng để ghi `code` cho mọi ngoại lệ có `Code` (`ConflictException`, `NotFoundException`, `RequestValidationException`). Mã `code` cũ của 401 (`token_expired`, `token_invalid`, `session_replaced`) giữ nguyên. DTO TypeScript ở `frontend/src/types` phải khớp.

Quy ước JSON: camelCase (mặc định của dự án).

## Kiểu dùng chung

`AvatarDto`: bản đồ ô → `variantKey`. Sáu ô bắt buộc **luôn** có giá trị chuỗi; năm ô tùy chọn là chuỗi hoặc `null` (trống).

```json
{
  "eyes": "variant03", "eyebrows": "variant05", "mouth": "variant12", "nose": "variant07",
  "hair": "variant12", "clothes": "variant08",
  "glasses": null, "beard": null, "gesture": "ok", "clothesGraphic": null,
  "background": "2a9d8f"
}
```

`AvatarItemDto`: `{ "key": "hair.variant12", "slot": "hair", "variantKey": "variant12", "name": "Tóc 12", "tier": "free|common|rare|special", "price": 0 }`.

`price` là số Xu server quy định (0 khi `free`). Giá trị `tier` viết thường.

## `GET /api/shop/items` (danh mục, công khai)

`[AllowAnonymous]`. `200`: `AvatarItemDto[]` (201 phần tử), sắp theo ô rồi `sortOrder`. Header `Cache-Control: public, max-age=3600` (danh mục chỉ đổi theo migration). Không có lỗi nghiệp vụ. Danh sách rỗng chỉ xảy ra khi DB chưa seed; frontend vẫn xử lý trạng thái rỗng và lỗi như mọi màn gọi dữ liệu.

## `POST /api/shop/purchases` (mua đồ)

`[Authorize]` (JWT). Request: `{ "itemKey": "hair.variant12" }` (client chỉ gửi mã món; **không** gửi giá).

`201 Created` (kèm `Location: /api/users/me/avatar`):

```json
{ "itemKey": "hair.variant12", "price": 50, "coinBalance": 75 }
```

| Tình huống | Status | `code` |
|---|---|---|
| `itemKey` thiếu/rỗng/không đúng dạng `slot.variantKey` hoặc dài quá 80 ký tự | `400` (kèm `errors.itemKey`) | `validation_failed` |
| Món là món **miễn phí** (không có gì để mua) | `400` | `item_not_purchasable` |
| Chưa đăng nhập / token hết hạn / sai | `401` | `token_expired` / `token_invalid` (có sẵn) |
| Không có món trong danh mục | `404` | `item_not_found` |
| Đã sở hữu (kể cả bấm lần thứ hai hoặc gửi đồng thời) | `409` | `already_owned` |
| Số dư nhỏ hơn giá | `409` | `insufficient_coins` |

Lý do chọn `409` cho "không đủ Xu": yêu cầu hợp lệ về cú pháp nhưng xung đột với trạng thái hiện tại của tài khoản (như `already_owned`), `400` dành cho input sai (CLAUDE.md liệt kê 400/401/403/404/409; `402` không nằm trong danh sách). Thứ tự kiểm: validate → 404 → 400 `item_not_purchasable` → 409 `already_owned` → 409 `insufficient_coins`. Số dư và sở hữu **không đổi** ở mọi lỗi.

Ví dụ `409`:

```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10", "title": "Bạn không đủ Xu để mua món này.", "status": 409, "code": "insufficient_coins" }
```

## `GET /api/users/me/avatar` (nhân vật và đồ đã sở hữu)

`[Authorize]`. `200`:

```json
{
  "avatar": { "...": "AvatarDto đầy đủ" },
  "isDefault": true,
  "ownedItemKeys": ["hair.variant12", "gesture.ok"]
}
```

- `isDefault = true` khi tài khoản chưa từng lưu; `avatar` khi đó là nhân vật mặc định tính ở server.
- `ownedItemKeys`: chỉ món **bán** đã mua; món miễn phí không liệt kê (luôn dùng được).
- Dùng cho cả `/profile` (trình chỉnh) và `/shop` (đánh dấu đã sở hữu, xem thử).

Tách khỏi `GET /api/users/me` (giữ `ProfileDto` nguyên, không đổi hợp đồng cũ); số dư Xu vẫn lấy từ `GET /api/users/me` (`coinBalance`), cửa hàng đọc cả hai bằng TanStack Query và làm mới cả hai sau khi mua.

## `PUT /api/users/me/avatar` (lưu nhân vật, thay toàn bộ)

`[Authorize]`. Request: `AvatarDto` đầy đủ 11 ô (ô tùy chọn trống gửi `null`). **Không** nhận ô lạ (`JsonUnmappedMemberHandling.Disallow` → `400`). Giá trị là `variantKey` của ô đó.

`200`: `{ "avatar": AvatarDto, "isDefault": false }`.

| Tình huống | Status | `code` |
|---|---|---|
| Thiếu hoặc `null` ở ô bắt buộc; ô lạ; `variantKey` sai định dạng | `400` (kèm `errors.<ô>`) | `validation_failed` |
| `variantKey` không có trong danh mục của ô đó | `400` (kèm `errors.<ô>`) | `unknown_variant` |
| Món bán mà tài khoản chưa sở hữu | `409` (kèm `errors.<ô>`) | `item_not_owned` |
| Chưa đăng nhập / hết hạn | `401` | có sẵn |

Cả yêu cầu bị từ chối nếu có **một** lỗi (không lưu một phần). Lỗi đầu tiên theo thứ tự ô cố định. Chọn `409` cho `item_not_owned` (trạng thái sở hữu xung đột với yêu cầu), nhất quán với `already_owned`/`insufficient_coins`; `403` dành cho phân quyền theo vai trò, không dùng ở đây.

## Thay đổi: `GET /api/leaderboards/xp`

Mỗi phần tử thêm `avatar: AvatarDto` (luôn đủ ô; tài khoản chưa lưu nhận nhân vật mặc định). Các trường cũ giữ nguyên, không thêm `userId`.

```json
{ "rank": 1, "displayName": "Alice", "level": 3, "totalXp": 640, "avatar": { "...": "AvatarDto" } }
```

## Không đổi

`GET /api/users/me`, `GET /api/users/me/rewards`, `POST /api/rooms/{code}/players` và các endpoint khác giữ nguyên. Nhân vật của người chơi trong phòng đi qua SignalR (xem [signalr-events.md](signalr-events.md)).

## Frontend (hợp đồng phía client)

- Các hàm gọi ở `src/api/shop.ts` và `src/api/avatar.ts` (mọi HTTP qua `request()`), TanStack Query với khóa `['shop','items']`, `['me','avatar']`, `['me','profile']`; sau khi mua thành công làm mới `['me','avatar']` và `['me','profile']`.
- Mã lỗi `code` ánh xạ ra thông điệp tiếng Việt ở `src/api/` (một bảng duy nhất), không rải trong component.
- Nút Mua/Lưu `disabled` trong lúc gửi; lỗi `already_owned` sau khi bấm hai lần được hiểu là thành công không đổi (làm mới dữ liệu, không hiện đỏ).
