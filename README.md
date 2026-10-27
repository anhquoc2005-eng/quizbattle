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

Kiểm tra: mở http://localhost:5000/api/health, kết quả mong đợi `{"status":"ok","database":"up"}`.

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

## Quy tắc dự án

Xem [CLAUDE.md](CLAUDE.md).
