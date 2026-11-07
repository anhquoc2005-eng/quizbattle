# QuizBattle

Web quiz real-time: chủ phòng nhập chủ đề, AI sinh bộ câu hỏi, người chơi vào phòng bằng mã và thi đấu trực tiếp.

## Stack

| Phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8, Entity Framework Core |
| Real-time | SignalR |
| Database | PostgreSQL (dev: Docker, production: Supabase) |
| Frontend | React 18 + TypeScript + Vite, Tailwind CSS |
| AI | Gọi API từ server, trả về JSON |
| CI | GitHub Actions: build và test mỗi lần push |

## Cấu trúc

```
backend/
  QuizBattle.Api/             Controllers, middleware, Program.cs
  QuizBattle.Application/     Services, DTO, interface
  QuizBattle.Domain/          Entity và logic nghiệp vụ thuần
  QuizBattle.Infrastructure/  DbContext, EF Core, gọi AI
  QuizBattle.Tests/           Unit test (xUnit)
frontend/
  src/api/  src/hooks/  src/pages/  src/components/  src/realtime/  src/types/
docker-compose.yml            PostgreSQL cho dev
```

Phụ thuộc giữa các project: `Api → Application + Infrastructure`, `Infrastructure → Application → Domain`.

## Chạy trên máy

Cần cài: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), [Node.js 22 LTS](https://nodejs.org), [Docker Desktop](https://www.docker.com/products/docker-desktop/).

**1. Database**

```bash
cp .env.example .env          # rồi điền POSTGRES_PASSWORD trong .env
docker compose up -d
```

**2. Backend** (chạy ở http://localhost:5000)

```bash
cd backend/QuizBattle.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5433;Database=quizbattle;Username=quizbattle;Password=<mật khẩu trong .env>"
dotnet run
```

Cần thêm `Jwt:Key` (ký JWT, dùng chung cho đăng ký/đăng nhập) — sinh ngẫu nhiên ≥ 32 byte rồi lưu bằng user-secrets, **không đặt trong appsettings hay commit**:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

```bash
dotnet user-secrets set "Jwt:Key" "<chuỗi vừa sinh>"
```

`Jwt:Issuer`/`Jwt:Audience` đã có sẵn trong `appsettings.Development.json` (không phải secret, không cần đặt thêm khi dev).

Kiểm tra: mở http://localhost:5000/api/health, kết quả mong đợi `{"status":"ok","database":"up"}`.

**AI sinh câu hỏi (tùy chọn)** — thiếu thì backend vẫn chạy bình thường, chỉ endpoint `POST /api/rooms/{code}/questions/generate` trả 503:

1. Lấy API key miễn phí tại [Google AI Studio](https://aistudio.google.com/apikey).
2. Chọn một model Gemini Flash còn hạn mức miễn phí ở trang [Rate Limit của AI Studio](https://aistudio.google.com/usage?tab=rate-limit) (ví dụ `gemini-3.5-flash-lite` — model cũ hơn như `gemini-2.5-flash-lite` có thể đã ngừng cấp cho user mới, kiểm tra lại trang Rate Limit nếu gặp lỗi 404 từ Gemini).
3. Đặt bằng user-secrets, **không đặt trong appsettings hay commit**:

```bash
dotnet user-secrets set "Ai:ApiKey" "<key của bạn>"
dotnet user-secrets set "Ai:Model" "<model đã chọn, ví dụ gemini-3.5-flash-lite>"
```

Lưu ý: gói miễn phí của Gemini có thể dùng dữ liệu gửi lên (chủ đề câu hỏi) để cải thiện model — không gửi dữ liệu cá nhân/nhạy cảm vào ô chủ đề.

**3. Frontend** (chạy ở http://localhost:5173)

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

Trang chủ hiện thẻ "Trạng thái hệ thống" với Backend và Database đều màu xanh là đã nối thông cả 3 phần.

**Chạy nhanh bằng VS Code**

Sau khi đã làm bước cài đặt lần đầu ở trên (tạo `.env` ở gốc repo, tạo `.env` trong `frontend/`, chạy `dotnet user-secrets set` cho backend), mở project trong VS Code và nhấn `Ctrl+Shift+B` (task `dev: all`) để khởi động Postgres, backend (`dotnet watch run`) và frontend (`npm run dev`) cùng lúc, mỗi phần một terminal riêng. Dùng task `db: stop` khi muốn tắt Postgres.

## Migration

Lần đầu clone repo (hoặc sau khi pull có migration mới), cài `dotnet-ef` đúng version theo tool manifest:

```bash
dotnet tool restore
```

Áp migration vào database dev (đọc connection string từ user-secrets, xem mục "Chạy trên máy"):

```bash
dotnet ef database update --project backend/QuizBattle.Infrastructure --startup-project backend/QuizBattle.Api
```

Tạo migration mới sau khi sửa entity/cấu hình:

```bash
dotnet ef migrations add <TênMigration> --project backend/QuizBattle.Infrastructure --startup-project backend/QuizBattle.Api -o Persistence/Migrations
```

App không tự chạy migration khi khởi động — luôn chạy `dotnet ef database update` thủ công.

## Luồng chơi (SignalR)

1. Chủ phòng tạo phòng, thêm câu hỏi (AI hoặc thủ công), duyệt câu → `POST /api/rooms/{code}/open` (Draft → Lobby, cần ít nhất 1 câu đã duyệt, không thì 409).
2. Người chơi vào phòng (không cần tài khoản) → `POST /api/rooms/{code}/players` với `{ displayName }`, nhận lại `{ playerId, sessionToken, roomCode }`. **Lưu `sessionToken` ở client** (ví dụ sessionStorage) để vào lại đúng phòng, giữ nguyên điểm nếu rớt mạng.
3. Cả hai kết nối SignalR tới `/hubs/game`:
   - Chủ phòng: xác thực bằng JWT (gửi qua query `access_token` vì WebSocket không gửi được header `Authorization`), gọi `JoinRoom(code, null)`.
   - Người chơi: kết nối ẩn danh, gọi `JoinRoom(code, sessionToken)`.
   - Cả hai nhận lại `RoomState` — dùng chung cho lần vào đầu tiên lẫn vào lại sau khi rớt mạng.
4. Chủ phòng gọi `StartGame()` → mọi người nhận `QuestionStarted` (không có đáp án đúng).
5. Người chơi gọi `SubmitAnswer(questionId, selectedIndex)` trước `deadline` trong `QuestionStarted` — mỗi câu chỉ trả lời được đúng 1 lần.
6. Câu tự kết thúc khi hết hạn hoặc khi mọi người đã trả lời; chủ phòng cũng có thể gọi `EndQuestion()` để kết thúc sớm bất cứ lúc nào. Mọi người nhận `QuestionEnded` (có đáp án đúng và `NextQuestionAt`) rồi `LeaderboardUpdated`.
7. Sau 6 giây hiện kết quả, server tự chuyển câu (hoặc chủ phòng gọi `NextQuestion()` để chuyển ngay): còn câu thì lặp lại bước 4-6; hết câu thì phòng chuyển `Finished`, mọi người nhận `GameEnded` kèm bảng xếp hạng cuối.

Server là nguồn sự thật duy nhất cho thời gian và điểm số — client không bao giờ gửi điểm hay thời gian lên, trận tự chạy nhờ `GameTimerService` (BackgroundService quét DB mỗi giây, không giữ timer trong RAM nên khởi động lại backend không làm mất lượt).

## Thử chơi nhiều người trên máy

Sau khi backend (`http://localhost:5000`) và frontend (`http://localhost:5173`) đều đang chạy:

1. Mở 1 tab trình duyệt, đăng nhập (`/login`), tạo phòng, thêm câu hỏi và duyệt, rồi vào `/rooms/:code/live` (nút "Mở phòng chờ" → "Vào màn điều khiển" trên `RoomPage`). Đây là màn chủ phòng.
2. Mở thêm 1-2 tab/trình duyệt khác (hoặc cửa sổ ẩn danh để không trùng session với tab chủ phòng), vào `/join`, nhập mã phòng 6 ký tự và tên hiển thị. Mỗi tab là một người chơi.
3. Để test trên điện thoại thật trong cùng mạng LAN: chạy `npm run dev -- --host` ở frontend, sửa `VITE_API_URL` trỏ sang IP LAN của máy chạy backend (backend cũng cần bind `0.0.0.0` và mở CORS cho origin đó), rồi mở `http://<IP-LAN>:5173/join` trên điện thoại.
4. Ở màn điều khiển, bấm "Bắt đầu" rồi theo dõi "Đã trả lời x/y"; không cần bấm gì thêm: câu tự kết thúc khi hết giờ hoặc mọi người đã trả lời, rồi tự chuyển sau 6 giây ("Kết thúc sớm" / "Chuyển ngay" chỉ để bấm sớm hơn).
5. Test rớt mạng: tắt Wi-Fi/mạng của một tab người chơi giữa câu, bật lại sau vài giây — tab đó hiện trạng thái "Đang kết nối lại", sau khi nối lại sẽ tự `JoinRoom` lại và đồng bộ đúng câu hỏi/điểm hiện tại (không bị reset).

## Test

```bash
cd backend && dotnet test
cd frontend && npm test
```

## Vibe code với Claude Code

Repo khai báo sẵn plugin trong `.claude/settings.json`. Mở repo bằng Claude Code và chọn tin tưởng thư mục;
nếu không thấy hỏi cài plugin, chạy trong Claude Code:

```
/plugin marketplace add anthropics/claude-plugins-official
/plugin marketplace add wshobson/agents
/plugin install csharp-lsp@claude-plugins-official
/plugin install typescript-lsp@claude-plugins-official
/plugin install security-guidance@claude-plugins-official
/plugin install commit-commands@claude-plugins-official
/plugin install code-review@claude-plugins-official
/plugin install dotnet-contribution@claude-code-workflows
```

Hai plugin LSP cần language server trên máy:

```bash
dotnet tool install --global csharp-ls
npm install -g typescript-language-server typescript
```

## Deploy

Kiến trúc: **Supabase** (PostgreSQL) ← **Render** (backend, Docker) ← **Vercel** (frontend tĩnh). Secret chỉ đặt trong biến môi trường của từng dịch vụ, không commit.

**Thứ tự**

1. **Supabase**: tạo project (region Singapore). Lấy chuỗi **Session pooler** (cổng 5432, không dùng Transaction pooler cổng 6543 và không dùng kết nối trực tiếp). Chỉ dùng Supabase như PostgreSQL, không dùng Auth/REST/Realtime.
   **Tắt Data API** (Integrations → Data API → tắt Enable Data API) trước khi chạy migration: backend không dùng REST tự sinh của Supabase, và các bảng trong schema `public` chưa bật RLS nên nếu Data API bật thì ai có anon key cũng đọc/ghi được (kể cả `Users`).
2. **Chạy migration** (làm tay, backend không tự chạy migration): `./scripts/migrate-prod.ps1` sinh `scripts/out/migrate-prod.sql` (idempotent, chạy lại không lỗi). Dán file vào Supabase SQL Editor, hoặc chạy trực tiếp: `./scripts/migrate-prod.ps1 -ConnectionString "postgresql://postgres.<ref>:<mật khẩu>@<host>:5432/postgres?sslmode=require"` (cần `psql`).
3. **Render**: New Web Service từ repo này, runtime **Docker**, region Singapore, Dockerfile Path `./backend/Dockerfile`, Docker Context `./backend`, Health Check Path `/api/health`. Đặt các biến ở bảng dưới. Render tự cấp `PORT`, backend đọc biến này.
4. **Vercel**: import repo, Root Directory `frontend`. Đặt `VITE_API_URL` (URL của backend trên Render) **trước khi build** (Vite nhúng giá trị lúc build; đổi sau phải build lại). `frontend/vercel.json` rewrite mọi đường dẫn về `index.html` để vào thẳng `/play/ABC123` không bị 404.
5. Quay lại Render, đặt `Cors__AllowedOrigins` bằng domain Vercel (ví dụ `https://quizbattle.vercel.app`, nhiều domain cách nhau bằng dấu phẩy) rồi deploy lại backend.

**Biến môi trường**

| Dịch vụ | Biến | Bắt buộc | Ghi chú |
|---|---|---|---|
| Render | `ASPNETCORE_ENVIRONMENT` | Có | `Production` (bật ForwardedHeaders và kiểm tra cấu hình lúc khởi động) |
| Render | `ConnectionStrings__Default` | Có | `Host=<host pooler>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<mật khẩu>;SSL Mode=Require;Maximum Pool Size=10` |
| Render | `Jwt__Key` | Có | Chuỗi ngẫu nhiên >= 32 byte |
| Render | `Jwt__Issuer`, `Jwt__Audience` | Có | Ví dụ `quizbattle` |
| Render | `Ai__ApiKey`, `Ai__Model` | Có (Production) | Khóa và model Gemini |
| Render | `Cors__AllowedOrigins` | Có (Production) | Domain Vercel, không dùng `*` |
| Render | `ForwardedHeaders__ClientIpHeader` | Có | `CF-Connecting-IP` (Render đứng sau Cloudflare, `X-Forwarded-For` có 2 IP nên không dùng được cho rate limit) |
| Vercel | `VITE_API_URL` | Có | URL backend, đặt lúc build |

Thiếu `Cors__AllowedOrigins`, `Ai__*`, `Jwt__*` hoặc connection string thì backend không khởi động và log nêu rõ tên biến thiếu.

**Lưu ý**

- `SSL Mode=Require` mã hóa kết nối nhưng không xác minh chuỗi chứng chỉ của server (đã kiểm với Npgsql 8). Tăng cường sau này: `SSL Mode=VerifyFull` kèm root CA của Supabase.
- `Maximum Pool Size=10` trong chuỗi kết nối: Supabase gói Nano giới hạn khoảng 60 kết nối, trong khi Npgsql mặc định tối đa 100 kết nối mỗi pool nên có thể vượt hạn mức. Chuỗi mẫu chỉ có chỗ giữ `<mật khẩu>`, không chứa mật khẩu thật.
- Session pooler không cần tắt prepared statement hay thêm `No Reset On Close` (các tùy chọn đó chỉ dành cho Transaction pooler).
- Render free tự ngủ sau 15 phút không có request, lần đầu gọi lại mất khoảng 1 phút để khởi động; trận đang chơi sẽ rớt kết nối nếu server ngủ giữa chừng.
- Supabase free tạm dừng project nếu một tuần không hoạt động; vào dashboard để khôi phục.
- Workflow `.github/workflows/keepalive.yml` gọi `GET /api/health` mỗi 2 ngày (và chạy tay được ở tab Actions) để Render thức dậy và Supabase có truy vấn DB, nên không bị tạm dừng. Job fail nếu không nhận 200. Đổi URL bằng biến repo `HEALTH_URL` (Settings → Variables), mặc định là backend trên Render.
- GitHub tự tắt workflow theo lịch nếu repo không có commit trong 60 ngày. Khi đó vào tab Actions, chọn workflow Keepalive và bật lại.
- Backend chỉ chạy đúng với 1 instance (xem "Hạn chế đã biết").

## Hạn chế đã biết

- **Login không chống timing side-channel**: thời gian xử lý "email không tồn tại" (không hash) khác "sai mật khẩu" (có hash) dù trả về cùng message, nên về lý thuyết kẻ tấn công có thể đo thời gian để dò email tồn tại qua endpoint login. Trên thực tế không tăng rủi ro: `POST /api/auth/register` đã trả 409 ngay khi email tồn tại, tức là cách lộ email tồn tại dễ và rẻ hơn nhiều đã có sẵn. Chặn timing ở riêng login không giải quyết được gốc vấn đề; muốn chặn triệt để cần thêm bước xác minh email (gửi mail, click link) — ngoài phạm vi MVP này.
- **GameHub chỉ chạy đúng với 1 instance backend**: nhóm SignalR (Groups) và trạng thái kết nối (Context.Items) sống trong bộ nhớ của từng tiến trình. Nếu scale ngang ra nhiều instance, 2 người trong cùng phòng có thể rơi vào 2 instance khác nhau và không nhận được broadcast của nhau. Muốn chạy nhiều instance cần thêm **Redis backplane** cho SignalR (gói `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, gọi `AddStackExchangeRedis(...)` sau `AddSignalR()`) để các instance chia sẻ group/connection qua Redis pub/sub — chưa làm ở bước này.
- **JWT của chủ phòng hết hạn giữa trận (60 phút) làm mất kết nối màn điều khiển**: khác với người chơi (dùng `sessionToken` không hết hạn giữa trận), kết nối SignalR của chủ phòng xác thực bằng JWT đăng nhập. Nếu một trận kéo dài quá 60 phút, JWT hết hạn thì `withAutomaticReconnect` sẽ thử nối lại nhưng bị 401 vì token cũ trong `accessTokenFactory` đã hết hạn — màn điều khiển rơi vào trạng thái lỗi, chủ phòng phải đăng nhập lại. Điểm và trạng thái trận ở server không mất (server không phụ thuộc kết nối của chủ phòng), người chơi không bị ảnh hưởng. Chưa xử lý refresh token tự động trong phạm vi MVP này.

## Quy tắc dự án

Xem [CLAUDE.md](CLAUDE.md).
