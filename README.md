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
6. Chủ phòng gọi `EndQuestion()` khi đã hết hạn hoặc mọi người đã trả lời → mọi người nhận `QuestionEnded` (có đáp án đúng) rồi `LeaderboardUpdated`.
7. Chủ phòng gọi `NextQuestion()`: còn câu thì lặp lại bước 4-6 cho câu tiếp theo; hết câu thì phòng chuyển `Finished`, mọi người nhận `GameEnded` kèm bảng xếp hạng cuối.

Server là nguồn sự thật duy nhất cho thời gian và điểm số — client không bao giờ gửi điểm hay thời gian lên, server cũng không chạy bộ hẹn giờ nền (chỉ ghi thời điểm và từ chối câu trả lời quá hạn khi có request tới).

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

## Hạn chế đã biết

- **Login không chống timing side-channel**: thời gian xử lý "email không tồn tại" (không hash) khác "sai mật khẩu" (có hash) dù trả về cùng message, nên về lý thuyết kẻ tấn công có thể đo thời gian để dò email tồn tại qua endpoint login. Trên thực tế không tăng rủi ro: `POST /api/auth/register` đã trả 409 ngay khi email tồn tại, tức là cách lộ email tồn tại dễ và rẻ hơn nhiều đã có sẵn. Chặn timing ở riêng login không giải quyết được gốc vấn đề; muốn chặn triệt để cần thêm bước xác minh email (gửi mail, click link) — ngoài phạm vi MVP này.
- **GameHub chỉ chạy đúng với 1 instance backend**: nhóm SignalR (Groups) và trạng thái kết nối (Context.Items) sống trong bộ nhớ của từng tiến trình. Nếu scale ngang ra nhiều instance, 2 người trong cùng phòng có thể rơi vào 2 instance khác nhau và không nhận được broadcast của nhau. Muốn chạy nhiều instance cần thêm **Redis backplane** cho SignalR (gói `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, gọi `AddStackExchangeRedis(...)` sau `AddSignalR()`) để các instance chia sẻ group/connection qua Redis pub/sub — chưa làm ở bước này.
- **Tên hiển thị người chơi trùng đang so phân biệt hoa thường**: "Alice" và "alice" được coi là 2 tên khác nhau khi vào cùng phòng (so sánh `==` thường, khớp đúng unique index `(RoomId, DisplayName)` trong DB). Có thể gây nhầm lẫn nhẹ trên bảng xếp hạng nhưng không phải lỗi bảo mật.

## Quy tắc dự án

Xem [CLAUDE.md](CLAUDE.md).
