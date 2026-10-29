# QuizBattle — Quy tắc dự án

Web quiz real-time: chủ phòng nhập chủ đề, AI sinh câu hỏi, người chơi vào phòng bằng mã và thi đấu trực tiếp.
Mục đích: dự án portfolio xin thực tập fullstack C#. Code phải sạch, nhất quán và giải thích được.

## Stack (không tự ý đổi)

- Backend: ASP.NET Core 8 (Web API + Controller), Entity Framework Core, SignalR
- Database: PostgreSQL (chạy bằng Docker)
- Frontend: React 18 + TypeScript + Vite, Tailwind CSS, React Router
- Gọi API: một API client dùng chung; state server dùng TanStack Query
- Real-time phía client: `@microsoft/signalr`
- Test: xUnit (backend), Vitest (frontend, chỉ cho logic)
- Muốn thêm thư viện mới: hỏi trước, nêu lý do.

## Cấu trúc thư mục

```
/backend
  QuizBattle.Api/            Controllers, Hubs, Program.cs, middleware
  QuizBattle.Application/    Services, DTOs, interfaces, validation
  QuizBattle.Domain/         Entities, enums, logic nghiệp vụ thuần (tính điểm)
  QuizBattle.Infrastructure/ DbContext, repositories, migrations, gọi AI
  QuizBattle.Tests/          Unit test
/frontend
  src/api/        API client + hàm gọi từng endpoint
  src/realtime/   Kết nối SignalR, sự kiện
  src/pages/      Mỗi màn hình một thư mục
  src/components/ Component dùng lại
  src/hooks/      Custom hooks
  src/types/      Kiểu TypeScript dùng chung
  src/validation/ Validate form thuần, không gọi API (test được bằng Vitest)
docker-compose.yml
```

## Quy tắc Backend

### Kiến trúc
- Phân lớp: Controller/Hub → Service → Repository → DbContext. Không bỏ qua lớp.
- Controller và Hub chỉ nhận request, gọi Service, trả kết quả. Không chứa logic nghiệp vụ.
- Logic tính điểm nằm trong Domain, là hàm thuần (không phụ thuộc DB, thời gian hệ thống truyền vào làm tham số) để test được.
- Dùng dependency injection qua interface (`IRoomService`, `IQuestionGenerator`...).
- Không trả Entity ra ngoài. Luôn map sang DTO. Request và Response là DTO riêng.

### API
- RESTful, danh từ số nhiều: `GET /api/rooms/{code}`, `POST /api/rooms`.
- Mọi response lỗi theo một format chung (ProblemDetails):
  `{ "type", "title", "status", "detail", "errors"? }`.
- Có middleware xử lý exception toàn cục. Không try/catch nuốt lỗi trong Controller.
- Mã trạng thái đúng: 200, 201, 400 (input sai), 401, 403, 404, 409 (xung đột), 500.

### Dữ liệu
- Validate mọi input (FluentValidation hoặc DataAnnotations). Không tin dữ liệu từ client.
- Dùng async/await cho mọi thao tác I/O. Không dùng `.Result` hay `.Wait()`.
- Mọi thay đổi schema đi qua EF Core migration. Không sửa DB bằng tay.
- Truy vấn chỉ đọc dùng `AsNoTracking()`. Tránh N+1: dùng `Include` hoặc projection.
- Thao tác nhiều bước phải nhất quán (ví dụ lưu kết quả trận) thì bọc trong transaction.

### Bảo mật
- Mật khẩu băm bằng thư viện chuẩn (ASP.NET Identity PasswordHasher hoặc BCrypt). Không tự viết.
- Xác thực chủ phòng bằng JWT. Người chơi khách dùng token phiên tạm, không cần tài khoản.
- Mọi secret (connection string, JWT key, API key AI) đọc từ biến môi trường / user-secrets. Không bao giờ commit, không bao giờ gửi xuống frontend.
- Có file `.env.example` liệt kê tên biến, không chứa giá trị thật.
- Rate limit cho endpoint gọi AI và endpoint đăng nhập.
- Bật CORS chỉ cho domain frontend, không dùng `AllowAnyOrigin` khi deploy.

### Real-time (SignalR)
- Một Hub: `GameHub`. Mỗi phòng là một SignalR group theo mã phòng.
- Danh sách sự kiện cố định, đặt tên thống nhất, ghi trong `src/realtime/events.ts` và hằng số tương ứng ở backend:
  - Client → Server: `JoinRoom`, `StartGame`, `SubmitAnswer`, `LeaveRoom`
  - Server → Client: `PlayerJoined`, `PlayerLeft`, `QuestionStarted`, `QuestionEnded`, `LeaderboardUpdated`, `GameEnded`, `Error`
- Server là nguồn sự thật duy nhất: thời gian bắt đầu câu, thời gian trả lời, điểm số đều tính ở server. Client chỉ hiển thị.
- Không nhận điểm hay thời gian do client gửi lên.
- Mỗi người chỉ được trả lời một lần cho mỗi câu; chặn ở server.
- Xử lý rớt mạng: người chơi kết nối lại bằng token phiên thì vào lại đúng phòng, giữ điểm.

### Tích hợp AI
- Chỉ gọi AI từ backend, qua interface `IQuestionGenerator` (để đổi nhà cung cấp hoặc mock khi test).
- Prompt yêu cầu trả về JSON đúng schema: `[{ "text", "options": [4 chuỗi], "correctIndex": 0-3 }]`.
- Luôn parse và validate kết quả: đủ 4 đáp án, `correctIndex` hợp lệ, không trùng câu. Sai thì thử lại tối đa 1 lần rồi báo lỗi rõ ràng.
- Đặt timeout cho lời gọi AI. Lỗi AI không được làm sập request khác.
- Câu hỏi AI sinh ra luôn ở trạng thái chờ duyệt, chủ phòng duyệt xong mới dùng.

### Test
- Bắt buộc unit test cho: tính điểm, validate kết quả AI, chặn trả lời trùng.
- Tên test mô tả hành vi: `CalculateScore_AnswerAfterDeadline_ReturnsZero`.

## Quy tắc Frontend

### Tổ chức
- Component hàm + hooks. Không dùng class component.
- TypeScript strict. Không dùng `any`; kiểu chưa rõ thì dùng `unknown` và kiểm tra.
- Mỗi file một component chính, tên PascalCase: `RoomLobby.tsx`. Hook bắt đầu bằng `use`: `useGameHub.ts`.
- Component dài quá ~150 dòng thì tách nhỏ.
- Kiểu DTO ở `src/types` khớp với DTO backend.

### Gọi API
- Mọi lời gọi HTTP đi qua `src/api`. Không gọi `fetch`/`axios` trực tiếp trong component.
- Dùng TanStack Query cho dữ liệu từ server. Không tự lưu bản sao dữ liệu server vào `useState`.
- URL backend lấy từ biến môi trường `VITE_API_URL`. Không chứa secret nào trong frontend.

### Real-time
- Kết nối SignalR quản lý trong một hook/context duy nhất (`src/realtime`). Không tạo kết nối trong từng component.
- Đăng ký sự kiện trong `useEffect` và luôn hủy đăng ký khi unmount.
- Bật tự kết nối lại (`withAutomaticReconnect`) và hiện trạng thái "đang kết nối lại" cho người dùng.
- Đồng hồ đếm ngược hiển thị dựa trên thời gian server gửi xuống, không tự đặt mốc.

### Giao diện
- Mọi màn hình gọi dữ liệu phải xử lý đủ 3 trạng thái: đang tải, lỗi, rỗng.
- Form có validate phía client (để báo nhanh) nhưng vẫn coi validate server là chính.
- Responsive: dùng được trên điện thoại (người chơi thường vào bằng điện thoại).
- Dùng Tailwind, không viết CSS inline rời rạc. Màu và khoảng cách theo một bộ cố định.
- Nút bấm có trạng thái disabled khi đang gửi để tránh gửi hai lần.

**Hệ thiết kế**: "true-black-flat" (taste-skill), nguồn gốc ở `docs/design/` (`app.css`, `login.html`, `dashboard.html`, ảnh chụp, `taste-direction.json`). Token định nghĩa ở `frontend/src/index.css` (`@theme`), dùng qua class Tailwind, không viết hex trực tiếp:

| Token | Giá trị | Dùng cho |
|---|---|---|
| `bg-ground` | `#000000` | Nền |
| `bg-surface` | `#111111` | Thẻ/card (`.card`) |
| `bg-raised` | `#2A2A2A` | Panel lồng bên trong card (bước, mã phòng) |
| `border-rule` | `#1F1F1F` | Đường kẻ phân cách (ví dụ đầu bảng lịch sử) |
| `border-control` | `#444444` | Viền ô nhập, nút ghost, khối rỗng nét đứt |
| `text-ink` | `#FFFFFF` | Chữ chính |
| `text-muted` | `#B4B4B4` | Chữ phụ |
| `text-faint` | `#949494` | Chữ chú thích nhỏ nhất (đi cùng `text-xs`) |
| `text-accent` / `bg-accent` | `#F5C518` | **Chỉ dùng cho dữ liệu** (mã phòng, thanh tiến độ) |
| `text-positive` | `#3FC07A` | Trạng thái tốt ("Đã xong") |
| `text-negative` | `#E5566F` | Lỗi |

**Quy tắc màu nhấn**: `accent` (#f5c518) chỉ xuất hiện trên dữ liệu (ký tự mã phòng, thanh tiến độ). Không dùng cho nút, tab, hay link.

**Font**: Inter (400/500/600) qua `<link>` Google Fonts ở `index.html` (có `preconnect`), không cài package. Một font cho cả trang. Số liệu dùng `tabular-nums`.

**Tiêu đề màn hình**: 20px/600 (`h1` mặc định trong `index.css`). Không dùng cỡ chữ hero lớn cho màn hình ứng dụng (login/dashboard); trang chủ (landing) là ngoại lệ, vẫn dùng tiêu đề lớn.

**Class dùng chung** (`index.css`, `@layer components`): `.card` (thẻ, bo góc 20px, không viền không bóng), `.btn`/`.btn-primary` (pill trắng chữ đen, cao 36px desktop / 44px điện thoại)/`.btn-ghost` (viền, nền trong suốt), `.field`/`.field input` (nhãn phía trên, cao 44px, giữ viền, focus đổi viền sang trắng, `[data-invalid]` đổi viền sang đỏ), `.code`/`.code span` (ô ký tự mã phòng, `.code.slot` cho ô trống "?"), `.col`/`.bar`/`.mark` (cột căn giữa, thanh trên cùng, logo+tên).

**Cấm**: gradient, bóng đổ, hiệu ứng kính mờ (backdrop-blur), emoji, chữ in hoa giãn khoảng cách (eyebrow label phía trên tiêu đề), đánh số kiểu 01/02, zebra table, icon trong vòng tròn màu trên mỗi thẻ KPI.

**Kiểm tra**: nếu máy đã cài skill `design-taste-frontend`, chạy `node <đường dẫn skill>/scripts/lint.mjs --app <file>` và `node <đường dẫn skill>/scripts/inspect.mjs --app <url bản build>` sau khi đổi giao diện, sửa hết FAIL trước khi coi là xong.

## Quy trình làm việc với AI

- Làm từng bước nhỏ, mỗi lần một tính năng theo lộ trình. Không sinh cả dự án một lần.
- Trước khi code một tính năng: nêu ngắn gọn kế hoạch (file nào, thay đổi gì) và chờ xác nhận.
- Sau khi code xong một tính năng:
  1. Chạy build và test, đảm bảo không lỗi.
  2. Giải thích ngắn gọn cách hoạt động và lý do thiết kế, đủ để người làm trả lời phỏng vấn.
  3. Đề xuất commit message.
- Không xóa hoặc viết lại code ngoài phạm vi yêu cầu.
- Gặp yêu cầu mâu thuẫn với quy tắc này: dừng lại và hỏi, không tự quyết.

## Môi trường và Deploy

- Dev (máy cá nhân): PostgreSQL chạy bằng `docker compose up -d`. Thử migration ở đây trước.
- Production: database là Supabase, CHỈ dùng như PostgreSQL được host sẵn.
  - Không dùng Supabase Auth, Supabase REST API, Supabase Realtime. Xác thực là JWT của backend, real-time là SignalR.
  - Kết nối bằng chuỗi **Session pooler (cổng 5432)**. Không dùng kết nối trực tiếp (thường chỉ IPv6) và không dùng Transaction pooler (cổng 6543, vướng prepared statement của EF Core).
  - Gói Free bị tạm dừng sau 7 ngày ít hoạt động: backend có job nhẹ truy vấn DB định kỳ (ví dụ mỗi 12 giờ).
- Code không phân biệt môi trường: chuỗi kết nối luôn đọc từ biến `ConnectionStrings__Default`.
- Backend deploy lên nơi hỗ trợ WebSocket và chạy liên tục (yêu cầu của SignalR); frontend deploy lên Vercel/Netlify. Ưu tiên cùng vùng Singapore với database.
- Migration lên production chạy thủ công có chủ đích, không tự chạy khi app khởi động.
- Khi deploy sau reverse proxy/load balancer: bật `UseForwardedHeaders`, chỉ tin proxy của nhà cung cấp hosting (khai báo `KnownProxies`/`KnownNetworks`, không dùng mặc định tin mọi proxy). Thiếu bước này, `RemoteIpAddress` mà server thấy là IP của proxy, rate limit theo IP sẽ gộp toàn bộ người dùng thành một IP.

## Plugin và skill

Plugin của dự án khai báo trong `.claude/settings.json`: `csharp-lsp`, `typescript-lsp`, `security-guidance`,
`commit-commands`, `code-review` (Anthropic) và `dotnet-contribution` (wshobson/agents).
Giao diện dùng skill `taste-skill` (cài ở tài khoản người dùng).

- Khi skill/plugin gợi ý khác file này, **file này thắng**.
- Skill `dotnet-backend-patterns` có ví dụ Redis, Dapper, Moq, Minimal API và Result pattern:
  dự án KHÔNG dùng các thứ đó (trừ khi đã hỏi và được đồng ý). Giữ Controller + EF Core + test bằng lớp giả tự viết.
- Chỉ dùng một skill thiết kế giao diện cho toàn dự án để phong cách nhất quán.

## Git

- Commit nhỏ theo từng tính năng, message dạng Conventional Commits:
  `feat: tạo phòng và sinh mã phòng`, `fix: chặn trả lời trùng`, `test: thêm test tính điểm`.
- Không commit file `.env`, `bin/`, `obj/`, `node_modules/`.
- Nhánh: `main` luôn chạy được; làm tính năng trên nhánh `feat/<tên>` rồi merge.

## Quy ước đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Class, method C# | PascalCase | `RoomService.CreateRoomAsync` |
| Biến, tham số C# | camelCase | `roomCode` |
| Method async C# | hậu tố `Async` | `GenerateQuestionsAsync` |
| Bảng DB | số nhiều, PascalCase | `Rooms`, `Questions` |
| Biến, hàm TS | camelCase | `submitAnswer` |
| Component React | PascalCase | `Leaderboard` |
| Hằng số | UPPER_SNAKE_CASE | `MAX_PLAYERS` |
