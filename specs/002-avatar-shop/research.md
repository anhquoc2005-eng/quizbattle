# Research: Nhân vật và cửa hàng

Ngày: 2026-11-12. Mọi quyết định bám [spec.md](spec.md) và CLAUDE.md. Thử nghiệm DiceBear chạy trong thư mục tạm NGOÀI repo, không cài gì vào dự án.

## R1. DiceBear: gói npm, phiên bản ghim, chứng minh vẽ được

### Phát hiện quan trọng: thế hệ 10.x đổi cách đóng gói

Ở 9.x mỗi kiểu là một gói riêng (`@dicebear/notionists@9.4.2`, tính đến nay là bản cuối của gói này). Từ 10.x gói `@dicebear/notionists` **không còn bản 10.x**; định nghĩa kiểu nằm trong gói `@dicebear/styles` (một file JSON mỗi kiểu), còn `@dicebear/core` chỉ là bộ vẽ. Con số "DiceBear styles 10.6.0" trong yêu cầu chính là phiên bản của `@dicebear/styles`.

| Gói | Phiên bản ghim | Vai trò | Giấy phép |
|---|---|---|---|
| `@dicebear/core` | `10.6.0` | Bộ vẽ SVG (`Style`, `Avatar`) | MIT |
| `@dicebear/styles` | `10.6.0` | File định nghĩa kiểu; chỉ dùng `notionists.json` | Theo từng kiểu: **Notionists = CC0 1.0** (Zoish); mã và manifest = MIT |

Kiểm bằng `npm view`: `@dicebear/core` có bản `10.6.0` (dist-tag `latest` hiện là `10.7.0`, `v10-lts` là `10.5.0`, nên **ghim đúng `10.6.0`, không dùng dist-tag**). `@dicebear/styles` có `latest = 10.6.0`. `@dicebear/core@10.6.0` không có dependency nào. Cả hai gói đều chưa có peer dependency.

Cài bằng `npm install --save-exact @dicebear/core@10.6.0 @dicebear/styles@10.6.0` để `package.json` ghi `"10.6.0"` không có `^`/`~` (cộng `.npmrc` `save-exact=true` cho frontend để lần sau không lỡ tay). Ở **Đợt D**, không phải bây giờ.

Cách import (Vite xử lý JSON): `import notionists from '@dicebear/styles/notionists.json'`, nạp **lazy** (`import()` động) vì file là 372.629 byte thô, **112.878 byte gzip** (97.426 byte brotli); chỉ các màn có nhân vật mới tải.

### Cấu trúc kiểu Notionists 10.6.0 (đọc từ `notionists.min.json`)

| Thành phần (tên DiceBear) | Số variant | Ô trong dự án | Ghi chú |
|---|---|---|---|
| `eyes` | 5 | mắt (bắt buộc) | `variant01`..`variant05` |
| `eyebrows` | 13 | lông mày (bắt buộc) | `variant01`..`variant13` |
| `mouth` | 30 | miệng (bắt buộc) | `variant01`..`variant30` |
| `nose` | 20 | mũi (bắt buộc) | `variant01`..`variant20` |
| `hair` | 64 | tóc (bắt buộc) | `hat`, `variant01`..`variant63` (tên **không** đều: có `hat`) |
| `clothes` | 25 | áo (bắt buộc) | `variant01`..`variant25` |
| `glasses` | 11 | kính (tùy chọn) | `variant01`..`variant11` |
| `beard` | 12 | râu (tùy chọn) | `variant01`..`variant12` |
| `gesture` | 10 | cử chỉ (tùy chọn) | tên chữ: `hand, handPhone, ok, okLongArm, point, pointLongArm, waveLongArm, waveLongArms, waveOkLongArms, wavePointLongArms` |
| `clothesGraphic` | 3 | hình in (tùy chọn) | `electric`, `galaxy`, `saturn` |
| `head` | 1 | (cố định) | không cho chọn |

Khớp từng con số trong yêu cầu. Màu của kiểu chỉ có hai: `ink` `#000000` và `paper` `#ffffff` (không đổi màu đồ, đúng yêu cầu "đen trắng").

### Thử nghiệm (ngoài repo)

```text
mkdir <scratchpad>/dbx && cd <scratchpad>/dbx && npm init -y
npm i --save-exact @dicebear/core@10.6.0 @dicebear/styles@10.6.0
node chk.mjs
```

Mỗi ô được ép bằng cặp tùy chọn `<ô>Variant` + `<ô>Probability: 100`; ô để trống bằng `<ô>Probability: 0`; nền bằng `backgroundColor`:

```js
import { Avatar, Style } from '@dicebear/core';
import notionists from '@dicebear/styles/notionists.json' with { type: 'json' };
const style = new Style(notionists);            // bọc Style: truyền thẳng định nghĩa vào Avatar bị cảnh báo "deprecated, bỏ ở v11"
const opts = {
  seed: 'hair.variant12|ok',                    // ghi chú R1-c: seed chỉ ảnh hưởng hậu tố id
  eyesVariant: 'variant03', eyesProbability: 100,
  eyebrowsVariant: 'variant05', eyebrowsProbability: 100,
  mouthVariant: 'variant12', mouthProbability: 100,
  noseVariant: 'variant07', noseProbability: 100,
  hairVariant: 'variant12', hairProbability: 100,
  clothesVariant: 'variant08', clothesProbability: 100,
  glassesProbability: 0, beardProbability: 0, clothesGraphicProbability: 0,
  gestureVariant: 'ok', gestureProbability: 100,
  backgroundColor: ['2a9d8f'],                  // hex KHÔNG cần '#'
};
new Avatar(style, opts).toString();             // chuỗi SVG; .toDataUri() cho <img src>
```

Kết quả đo được:

| Kiểm tra | Kết quả |
|---|---|
| Vẽ `hair=variant12`, kính rỗng, `gesture=ok`, nền `2a9d8f` | Có SVG (≈12 KB); chứa `<rect>` màu nền `2a9d8f`; khác SVG khi tắt cử chỉ |
| Bật `glasses=variant04` + `clothesGraphic=galaxy` | SVG khác SVG không kính/không hình in |
| Nền hex không có `#` | Hoạt động (`<rect ... fill>` đúng màu) |
| Không đặt nền | Không có `<rect>` nền: **trong suốt** (xem R1-b) |
| Ép từng variant của mọi ô (193 trường hợp, đã loại `head`) | Không ném lỗi |
| Cùng tùy chọn, hai lần | Giống nhau từng byte |

#### R1-a. Hình in chỉ hiện trên áo `variant01`..`variant10`

Thử `clothesGraphic=saturn` trên từng kiểu áo: **`clothes` `variant11`..`variant25` (15 kiểu) vẽ ra SVG y hệt khi không có hình in** (áo đó không có chỗ in). Hệ quả: không được để người mua hình in (400 Xu) thấy "không hiện gì". **Quyết định đã duyệt (Q-A)**: tập áo in được `variant01`..`variant10` là hằng `AvatarCatalog.PrintableClothesVariantKeys`; `AvatarRules` (hàm thuần) từ chối lưu nhân vật có hình in mà áo ngoài tập này (`400 graphic_incompatible_clothes`); giao diện vô hiệu hóa danh sách hình in kèm lý do. Hằng số này có thể lệch khi nâng cấp DiceBear, nên frontend có test Vitest dựng lại tập áo in được từ chính phiên bản đã ghim (vẽ từng áo có và không có hình in, so SVG sau khi bỏ hậu tố id) và khẳng định bằng đúng hằng số; test backend `AvatarCatalogStyleSyncTests` cũng kiểm hằng số chỉ chứa khóa áo có thật. Chi tiết ở [plan.md](plan.md).

#### R1-b. Ô nền trống phải có nền mặc định khi vẽ

Không có nền thì SVG trong suốt, mà viền và nét vẽ là `#000000`, đặt trên nền `#000000` của giao diện thì **mất hình**. Vì vậy nền phải luôn hiện: ô `background` trống nghĩa là vẽ **nền mặc định** `d4d4d4` (đúng giả định đã duyệt ở spec). Giá trị này là một hằng số duy nhất ở Domain (`AvatarCatalog.DefaultBackgroundHex`) và là một món miễn phí trong danh mục; frontend nhận nền qua dữ liệu, chỉ riêng trường hợp ô trống mới dùng hằng số cùng giá trị ở `src/avatar/` (có test so khớp với danh mục).

#### R1-c. Tính xác định và hậu tố id

Cùng tùy chọn thì cùng SVG. Khác `seed` mà variant đã ép hết thì SVG **chỉ khác hậu tố 8 ký tự hex của các id** (`head-variant01-d7612fdb`); xóa hậu tố thì giống từng byte. Hậu tố này đáng giữ: hai nhân vật trên cùng một trang sẽ không trùng `id` SVG nếu `seed` khác nhau. Quy ước: `seed` = chuỗi nối các cặp `ô.variantKey` của nhân vật (`"hair.variant12|eyes.variant03|..."`), để cùng nhân vật thì cùng id, khác nhân vật thì khác id. Dùng `<img src={dataUri}>`, không chèn SVG thẳng vào DOM, nên id không đụng nhau trong tài liệu và script trong SVG (nếu có) không chạy.

#### R1-d. Không dùng API công khai

Vẽ hoàn toàn ở trình duyệt từ file JSON đi kèm gói npm; không có lời gọi nào tới `api.dicebear.com`. Cấm cứng bằng một test Vitest quét `src/` không chứa chuỗi `api.dicebear.com`.

### Giấy phép và THIRD_PARTY_NOTICES.md (làm ở Đợt D)

Nội dung dự kiến: bảng hai gói, phiên bản, giấy phép; ghi rõ "Notionists (Zoish), bản phối lại trong `@dicebear/styles`, CC0 1.0" với liên kết `https://heyzoish.gumroad.com/l/notionists` (bản gốc), `https://bio.link/heyzoish`, `https://github.com/dicebear/styles`, `https://github.com/dicebear/dicebear`; MIT cho mã DiceBear (Copyright (c) 2026 Florian Körner, theo `LICENSE.md` của gói). Chỉ `notionists.json` được import nên các kiểu CC BY khác trong cùng gói **không** vào bản build (kiểm bằng danh sách module ở Đợt D; nếu bundler đóng cả gói thì dừng và báo).

### Phương án đã loại

| Phương án | Lý do loại |
|---|---|
| API HTTP `api.dicebear.com` | Chỉ miễn phí phi thương mại, không cam kết hoạt động; phụ thuộc mạng ngoài (đã chốt: cấm) |
| `@dicebear/notionists@9.4.2` (gói riêng 9.x) | Không phải thế hệ 10.x; số variant 9.x khác (không bảo đảm khớp bảng yêu cầu) |
| `@dicebear/collection` | Kéo cả chục kiểu CC BY 4.0 không dùng, phải ghi công nhiều hơn, nặng hơn |
| Tự lưu SVG trên backend | Trái ràng buộc "backend chỉ lưu cặp ô → variantKey" |

## R2. Tính nhân vật mặc định ở Domain bằng hàm thuần

**Quyết định**: `DefaultAvatarGenerator` (Domain, static, thuần) với hai hàm `ForAccount(Guid userId)` và `ForGuest(string displayName)`. Hash ổn định, không dùng `string.GetHashCode()` (đổi giữa các lần chạy, theo từng process). Thuật toán:

1. Khóa đầu vào: tài khoản `"u:" + userId.ToString("N")`; khách `"g:" + displayName.Trim().ToLowerInvariant()` (tên trong phòng đã không phân biệt hoa thường, nên "An" và "an" ra cùng một nhân vật).
2. Băm FNV-1a 64 bit trên UTF-8 của khóa.
3. Với mỗi ô bắt buộc theo thứ tự cố định (`eyes, eyebrows, mouth, nose, hair, clothes`, chỉ gồm món **miễn phí dùng cho mặc định** của ô, tức `AvatarCatalog.DefaultFreeItems(slot)`; ở ô tóc là **8 kiểu**, không có khăn trùm đầu `variant61`..`63`): `x = SplitMix64(hash + (i+1) * 0x9E3779B97F4A7C15)`; chọn `free[slot][x % count]`. SplitMix64 tách các ô khỏi nhau để cùng một hash không làm các ô chọn tương quan.
4. Ô tùy chọn để trống; `background` = nền mặc định (xám `d4d4d4`) cho cả tài khoản lẫn khách (khách "nền xám" khớp nền mặc định).

Danh sách món miễn phí lấy từ `AvatarCatalog` (R3), là cùng nguồn với bản seed, nên không thể lệch giữa "nhân vật mặc định" và "món miễn phí".

Danh sách món miễn phí do cố vấn chọn (data-model mục 5.1): tóc `variant01, 02, 05, 08, 20, 23, 41, 45` + khăn trùm đầu `61, 62, 63`; áo `variant01, 02, 07, 14, 23`.

**Khăn trùm đầu không bao giờ nằm trong nhân vật mặc định** (sửa sau khi cố vấn duyệt Đợt A): nếu chọn đều trong 11 món miễn phí của ô tóc thì 3/11 ≈ 27% tài khoản và khách sẽ tự động mang khăn tôn giáo dù không chọn. Khăn vẫn miễn phí và chọn được ở trình chỉnh; chỉ riêng bộ sinh mặc định bỏ chúng qua `AvatarCatalog.DefaultFreeItems(slot)`, dựa trên tập `AvatarCatalog.HeadscarfVariantKeys` (nguồn duy nhất).

**Lý do**: backend là nơi duy nhất sinh mặc định (các nơi đều nhận đủ ô qua DTO, frontend không tự sinh); hàm thuần nên test được, đúng quy tắc Domain của CLAUDE.md.

**Đã loại**: dùng `seed` của DiceBear để frontend tự sinh (sẽ khác giữa các nơi, không kiểm chứng "chỉ món miễn phí" ở server được); lưu nhân vật mặc định vào DB cho mọi tài khoản cũ (trái yêu cầu "không cần sửa dữ liệu cũ").

**Test (bắt buộc)**: cùng đầu vào cho cùng kết quả (nhiều lần, và hai thể hiện `Guid` bằng nhau); mọi món trả về thuộc danh sách miễn phí dùng cho mặc định (tóc ∈ 8 kiểu, **không bao giờ** `variant61`..`63`, kiểm trên 10.000 tài khoản và 10.000 tên khách; áo ∈ 5 kiểu); đủ sáu ô bắt buộc, bốn ô tùy chọn rỗng, nền mặc định; khách không phân biệt hoa thường/khoảng trắng đầu cuối; kết quả cố định cho một vài đầu vào cụ thể (**golden**: nếu ai đó đổi hash hay danh sách miễn phí thì test fail, vì như vậy nhân vật mặc định của mọi người đổi hình); 10.000 khóa ngẫu nhiên đều hợp lệ và phủ đủ nhiều kiểu (không dồn vào một kiểu).

## R3. Danh mục: một nguồn duy nhất trong mã, seed vào DB, server kiểm bằng DB

**Quyết định**: định nghĩa danh mục **một lần** trong `QuizBattle.Domain` (`AvatarCatalog`: danh sách 201 bản ghi `AvatarItemDefinition`), dùng cho: (1) seed bằng `HasData` trong migration, (2) sinh nhân vật mặc định, (3) test so với file style. Server khi mua/lưu nhân vật luôn tra **bảng `AvatarItems` trong DB** (không tra danh sách trong mã), đúng yêu cầu "nằm trong DB để server kiểm tra mọi variantKey".

**Lý do**: nếu seed và mặc định cùng đọc `AvatarCatalog`, hai nơi không thể lệch nhau. Seed qua `HasData` làm EF sinh `InsertData` trong migration và snapshot; migration production idempotent do `scripts/migrate-prod.ps1` (`--idempotent`) chỉ chạy migration chưa áp.

**Hệ quả cần biết**: sửa `AvatarCatalog` sau khi đã áp migration lên prod → EF sẽ sinh migration mới với `UpdateData`/`InsertData`; **không** sửa migration đã áp. Chỉnh bảng giá hoặc danh sách miễn phí sau khi lên prod đồng nghĩa với migration mới và (với món miễn phí đổi thành bán) có thể làm nhân vật mặc định đổi hình; vì vậy mọi lựa chọn món miễn phí/giá phải được duyệt **trước** khi Đợt A chạy migration lên prod (xem [data-model.md](data-model.md) mục 5).

**Test đồng bộ với file style (bắt buộc)**: `AvatarCatalogStyleSyncTests` mở `frontend/node_modules/@dicebear/styles/dist/notionists.min.json` và `package.json` của gói đó:

1. phiên bản trong `package.json` == hằng `AvatarCatalog.StyleVersion` (`"10.6.0"`) → bắt trường hợp ai nâng cấp mà quên cập nhật danh mục;
2. với mỗi ô DiceBear, tập `variantKey` trong danh mục == tập khóa `components.<ô>.variants` của file (so hai chiều: không thiếu, không thừa);
3. tên ô trong danh mục (kể cả `clothesGraphic`) có trong `components`;
4. ô `background` không được kiểm (không thuộc DiceBear); mỗi `variantKey` nền khớp `^[0-9a-f]{6}$`.

Nếu thiếu `frontend/node_modules` thì test **fail với thông báo "chạy `npm ci` ở frontend"**, trừ khi đặt `QB_SKIP_STYLE_SYNC=1` (ngoại lệ có chủ đích, in cảnh báo). Không bỏ qua im lặng.

## R4. Khóa món đồ: chuỗi `ô.variantKey`, không dùng Guid

**Quyết định**: khóa chính `AvatarItems.Key` kiểu `text`, dạng `"<ô>.<variantKey>"`, ví dụ `hair.variant12`, `clothesGraphic.galaxy`, `background.2a9d8f`. Hai cột tách riêng `Slot` và `VariantKey` luôn khớp (`Key = Slot || '.' || VariantKey`, có CHECK).

**Lý do**:
- Seed cố định trong migration và giống hệt giữa dev, test, prod mà không cần sinh/ghi nhớ 201 Guid.
- Đọc được trong log, trong bảng sở hữu và trong ràng buộc CHECK (kiểm "ô nào" bằng `LIKE 'hair.%'`).
- Client gửi đúng mã người đọc được (`itemKey`), server không cần dịch.
- Khóa này bất biến (cùng với việc ghim phiên bản DiceBear): đổi tên một variant là một thay đổi có chủ đích qua migration.

**Đã loại**: Guid (cần bảng đối chiếu để seed ổn định, khó đọc log, không thêm bảo đảm gì); khóa số tự tăng (seed không ổn định giữa môi trường).

## R5. Nơi lưu nhân vật: bảng riêng `UserAvatars`, không thêm cột vào `Users`

**Quyết định**: bảng `UserAvatars` quan hệ 1-1 với `Users`, `UserId` vừa là khóa chính vừa là khóa ngoại; mỗi ô một cột lưu `ItemKey` (bắt buộc: `NOT NULL`; tùy chọn: nullable).

**Lý do**:
- `Users` bị khóa `FOR UPDATE` ở cuối trận và khi mua đồ; thêm 11 cột vào đó làm dòng lớn hơn và mọi lần lưu nhân vật chạm vào bảng nóng nhất. Lưu nhân vật **không** cần khóa `Users` (không đụng Xu/XP).
- Tài khoản chưa chỉnh (mọi tài khoản cũ) **không có dòng** ở đây ⇒ "không cần sửa dữ liệu cũ" đạt tự nhiên; có dòng nghĩa là "đã từng lưu".
- Một câu `LEFT JOIN` lấy nhân vật của mọi người chơi trong phòng (R6).

**Đảm bảo mọi variant hợp lệ**:
1. Khóa ngoại từng cột → `AvatarItems.Key`: không thể lưu món không có trong danh mục.
2. `CHECK` theo ô: ví dụ `"HairItemKey" LIKE 'hair.%'` → không thể đặt món sai ô.
3. `NOT NULL` ở sáu ô bắt buộc.
4. Điều "món này là của người dùng này, hoặc miễn phí" không biểu diễn được bằng ràng buộc một dòng, nên **service kiểm trong cùng transaction** (miễn phí hoặc có dòng trong `UserAvatarItems`). Vì quyền sở hữu không bao giờ bị xóa (chỉ thêm), kiểm-rồi-ghi này không có race cần khóa.

**Lưu cả `PUT` trùng nhau**: `PUT` thay toàn bộ, dòng được `upsert`; hai `PUT` đồng thời của cùng một tài khoản thì bên ghi sau thắng (chấp nhận, ngữ nghĩa "lưu lần cuối"). Hai `PUT` **đầu tiên** đồng thời cùng chèn dòng mới → một bên vi phạm khóa chính: bắt `23505` và thử lại một lần dưới dạng cập nhật.

## R6. Hiển thị trong phòng: một truy vấn bổ sung, không N+1

Đối chiếu code thật: `GameRepository.LoadSnapshotAsync` đã nạp `Users` của **mọi người chơi trong phòng bằng một truy vấn** (`WHERE Id = ANY(userIds)`; có khóa `FOR UPDATE` theo `ORDER BY Id` chỉ ở lượt kết thúc trận). Service dựng `PlayerSummaryDto`, `LeaderboardEntryDto`, `PlayerJoinedDto` từ `snapshot.Users` qua `LevelOf`.

**Quyết định**: thêm vào `LoadSnapshotAsync` **đúng một** truy vấn `AsNoTracking` đọc `UserAvatars` theo cùng mảng `userIds`, đưa vào `GameSnapshot.Avatars` (`Dictionary<Guid, UserAvatar>`). Service có hàm `AvatarOf(snapshot, player)`: tài khoản có dòng ⇒ nhân vật đã lưu; tài khoản không dòng ⇒ `DefaultAvatarGenerator.ForAccount(userId)`; khách ⇒ `ForGuest(displayName)`. Mọi sự kiện/DTO dùng cùng hàm này nên **mọi nơi luôn nhận đủ ô**.

- Tổng truy vấn cho một snapshot: +1, không phụ thuộc số người chơi (kiểm bằng test đếm lệnh SQL, ví dụ qua `DbCommandInterceptor` trong test).
- Không dùng khóa `FOR UPDATE` cho bảng này (không liên quan Xu/XP), nên **không thay đổi thứ tự khóa** `Room → Users` của CLAUDE.md.
- "Nhân vật hiện tại, không chụp lại lúc vào phòng": snapshot luôn đọc `UserAvatars` ở thời điểm dựng sự kiện; ảnh chụp không được lưu ở `Players`. Hệ quả: người khác thấy thay đổi ở sự kiện kế tiếp (bảng điểm mỗi câu, `RoomState` khi tải lại); **không** thêm sự kiện đẩy riêng khi ai đó đổi đồ (ngoài phạm vi).

**Bảng xếp hạng XP**: `UserRepository.GetTopByXpAsync` đổi sang projection có `LEFT JOIN` `UserAvatars` (một câu SQL cho 50 dòng), nhân vật mặc định tính ở service từ `UserId` (`UserId` **không** đưa ra DTO).

**Kích thước payload** (xem chi tiết ở [contracts/signalr-events.md](contracts/signalr-events.md)): một `AvatarDto` ≈ 210 byte JSON; phòng 50 người: `RoomState` +21 KB (Players + Leaderboard), `LeaderboardUpdated` +10,5 KB mỗi câu, `GameEnded` +10,5 KB một lần. Chấp nhận được; nếu về sau phình, tách bản đồ `avatars` theo `playerId` dùng chung để giảm một nửa.

## R7. Mua đồ và đồng thời

**Trình tự** (một transaction, một `AppDbContext`):

1. `BEGIN`.
2. `SELECT * FROM "Users" WHERE "Id" = @userId FOR UPDATE` (một dòng; nếu không có → 401/404 tuỳ ngữ cảnh, token hợp lệ nhưng tài khoản đã xóa).
3. Đọc món từ `AvatarItems` (bất biến, không khóa): không có → `404 item_not_found`; miễn phí → `400 item_not_purchasable`.
4. Đã sở hữu (`UserAvatarItems` có `(UserId, ItemKey)`) → `409 already_owned`. **Kiểm sở hữu trước kiểm số dư** để người đã sở hữu luôn nhận `already_owned`, không phải `insufficient_coins`.
5. `CoinBalance < Price` → `409 insufficient_coins`.
6. `CoinBalance -= Price`; thêm `UserAvatarItems`; thêm `CoinTransactions` (`Amount = -Price`, `Reason = ShopPurchase`, `UserAvatarItemId` = id vừa thêm).
7. `SaveChanges` + `COMMIT`. Vi phạm duy nhất `(UserId, ItemKey)` (lưới an toàn thứ hai, không xảy ra khi bước 2 đúng) → `409 already_owned`.

**Chứng minh không mất cập nhật**: mọi ghi `Users.CoinBalance` (mua, kết thúc trận) đều phải giữ khóa dòng `Users` của chính nó; hai giao dịch cùng tài khoản bị tuần tự hoá ở bước 2. Bất biến `CoinBalance = SUM(CoinTransactions.Amount)` giữ vì cả ba thay đổi (số dư, quyền sở hữu, dòng sổ cái) cùng commit hoặc cùng rollback.

**Chứng minh không deadlock với kết thúc trận**: kết thúc trận lấy khóa theo thứ tự `Room` rồi các `Users` theo `ORDER BY Id` (CLAUDE.md). Mua đồ **chỉ** lấy **một** khóa `Users` và không bao giờ chờ khóa `Room` hay `Users` khác trong khi đang giữ nó. Một chu trình chờ cần một giao dịch giữ `Users[A]` mà chờ thứ gì đó do giao dịch khác giữ và ngược lại; mua đồ giữ `Users[A]` nhưng không chờ ai (các bước 3–7 chỉ đọc bảng bất biến và chèn dòng mới có khóa ngoại tới `Users[A]` mà chính nó đã khóa, và `AvatarItems`, không bị ai khóa độc quyền). Vậy mua đồ không thể nằm trong chu trình ⇒ không deadlock. Khóa ngoại: `FOR KEY SHARE` lên `Users[A]` do chính giao dịch giữ `FOR UPDATE` thì không xung đột với chính nó.

**Test đồng thời** và **kiểm độ nhạy**: xem mục "Đồng thời" của [plan.md](plan.md) (điểm 6).

## R8. RLS trên Supabase

Hiện trạng (cố vấn đã kiểm trên Supabase production, 07/11 và 10/11): **cả 8 bảng hiện có đã bật RLS**, truy vấn kiểm cho 0 bảng tắt RLS. Dòng trong README nói "chưa bật RLS" đã lỗi thời và sẽ được sửa ở Đợt A. Yêu cầu: **bảng mới cũng phải bật RLS** để nhất quán.

**Quyết định**: trong migration `AddAvatarShop` thêm `migrationBuilder.Sql("ALTER TABLE \"AvatarItems\" ENABLE ROW LEVEL SECURITY;")` cho ba bảng mới (`AvatarItems`, `UserAvatars`, `UserAvatarItems`). **Không tạo policy**: không policy nghĩa là vai trò `anon`/`authenticated` (Data API) không đọc/ghi được gì; backend kết nối bằng vai trò chủ bảng (`postgres`, bỏ qua RLS) nên không bị ảnh hưởng. Không `FORCE`. Câu lệnh vô hại trên PostgreSQL dev (docker) và chạy lại được (idempotent).

Test tích hợp: truy vấn `pg_class.relrowsecurity` của ba bảng phải `true` sau khi áp toàn bộ migration trên DB test.

Không cần việc nào cho bảng cũ (đã bật). Việc duy nhất liên quan: sửa dòng README (Đợt A).

## R9. Phát hiện ở mã hiện có cần xử lý trong tính năng

| Phát hiện | Xử lý |
|---|---|
| `ConflictException`/`NotFoundException`/`RequestValidationException` không có `code`; `GlobalExceptionHandler` chỉ ghi `code` cho 401 | Thêm thuộc tính `Code` (tùy chọn) vào ba lớp ngoại lệ + handler ghi `code` chung; mã lỗi cũ giữ nguyên |
| `CoinTransaction` chưa truy được món đã mua; chú thích cũ ghi "mua đồ ở tính năng sau" | Thêm `UserAvatarItemId` (FK nullable) + CHECK; cập nhật chú thích |
| `CoinTransactionReason.ShopPurchase` đã có, chưa dùng | Dùng nguyên, không thêm giá trị enum |
| `ProfileDto` không có nhân vật; Hồ sơ dùng `/api/users/me` | Giữ nguyên `ProfileDto`; nhân vật qua endpoint riêng `/api/users/me/avatar` (xem contracts) |
| README nói schema `public` chưa RLS (lỗi thời: production đã bật RLS cho cả 8 bảng) | Sửa dòng README ở Đợt A; xem R8 |
| `GameBroadcaster` đã gom việc gửi sự kiện | Không đổi; chỉ đổi DTO |

## R10. Quyết định về kiểu (Hồ sơ) và UI nhân vật nhỏ

- Hai cỡ nhân vật: nhỏ (cạnh tên, 28 px) và lớn (hồ sơ/cửa hàng, 160 px); một component `Avatar` nhận `avatar` + `size`, vẽ bằng `<img src=dataUri>` có bộ nhớ đệm theo chữ ký nhân vật (50 người thường trùng nhiều ô nên số lần vẽ thật thấp); có khung chờ (ô xám) trong lúc nạp `notionists.json` lần đầu và khi `avatar` thiếu.
- Hệ thiết kế true-black-flat: khung tròn/bo góc bằng Tailwind (`rounded-full`/`rounded-2xl`), không bóng, không viền gradient; màu nền nhân vật là **dữ liệu nghệ thuật** (hex trong danh mục), không phải token UI, nên không vi phạm quy tắc "không viết hex trong component"; `accent` `#F5C518` không dùng làm màu nền nhân vật.
- 8 màu nền (hex) và bảng giá: [data-model.md](data-model.md).
