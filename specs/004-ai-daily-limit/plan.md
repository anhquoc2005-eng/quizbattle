# Implementation Plan: Giới hạn AI theo ngày

**Branch**: `feat/ai-daily-limit` | **Date**: 2026-11-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-ai-daily-limit/spec.md`

## Summary

Thêm bảng `AiUsageDaily` (khóa `(Day, Scope)`) làm bộ đếm bền trong PostgreSQL. `QuestionService.GenerateAsync`
giữ chỗ lượt (global rồi user, một transaction, commit trước khi gọi Gemini) sau mọi kiểm tra rẻ; thất bại
hoàn toàn thì hoàn lượt user (đúng `Day` đã giữ). Vượt trần → 429 `AI_DAILY_LIMIT_USER|GLOBAL` + `resetAt`;
429 của policy 10/giờ thêm `AI_RATE_LIMIT`. `GET /api/ai-quota` cho frontend hiện "Còn X/20 lượt AI hôm nay".
Có 1 migration; không thêm thư viện.

## Technical Context

**Language/Version**: C# / .NET 8 (backend), TypeScript 5 + React 18 (frontend)

**Primary Dependencies**: ASP.NET Core 8, EF Core + Npgsql (đã có); TanStack Query (đã có). KHÔNG thêm gói mới.

**Storage**: PostgreSQL — THÊM bảng `AiUsageDaily` (1 migration `AddAiUsageDaily`)

**Testing**: xUnit (unit + tích hợp Postgres thật qua `DatabaseFixture`/`RequiresDatabaseFact`), Vitest + jsdom/@testing-library/react

**Project Type**: Web app (backend + frontend)

**Constraints**: Giữ nguyên policy 10/giờ; không giữ khóa trong lúc chờ Gemini; không hoàn global; không hoàn khi hủy; ngày VN cố định +7h không dùng `TimeZoneInfo`; theo hệ thiết kế true-black-flat (accent chỉ cho dữ liệu).

**Scale/Scope**: ≤ 300 giữ chỗ/ngày toàn hệ thống; 1 bảng, 1 endpoint GET mới, 1 exception mới, 1 panel frontend sửa.

## Constitution Check (CLAUDE.md)

| Quy tắc | Đạt? | Ghi chú |
|---|---|---|
| Controller → Service → Repository → DbContext | Đạt | `AiQuotaController` + `QuestionsController` chỉ gọi Service; SQL ở `AiQuotaRepository` |
| Logic thuần trong Domain, thời gian là tham số | Đạt | `VietnamDay` (Domain) nhận `DateTimeOffset`; service lấy giờ từ `TimeProvider` |
| Không trả Entity ra ngoài | Đạt | `AiQuotaResponse` DTO |
| ProblemDetails + middleware exception toàn cục | Đạt | `AiDailyLimitException` map 429 trong `GlobalExceptionHandler`, không try/catch ở Controller |
| Validate input, không tin client | Đạt | Không có input mới; cấu hình được validate lúc khởi động |
| Async, không `.Result`/`.Wait()` | Đạt | |
| Thay đổi schema qua migration | Đạt | `AddAiUsageDaily`; production chạy thủ công bằng SQL idempotent |
| Thao tác nhiều bước nhất quán → transaction | Đạt | Giữ chỗ global+user trong 1 transaction |
| Khóa `Users` theo thứ tự `Id` | Không áp dụng | Tính năng không ghi vào `Users`; bất biến `CoinBalance`/`TotalXp` không đụng |
| Secret từ env, `.env.example` | Đạt | Hai biến mới không bí mật, vẫn liệt kê trong `.env.example` |
| Rate limit endpoint AI | Đạt (giữ nguyên) | Policy 10/giờ + trần ngày |
| Không thêm thư viện | Đạt | |
| DI qua interface, test bằng lớp giả tự viết | Đạt | `IAiQuotaRepository`, `IAiQuotaService`; fake tự viết (không Moq) |
| Frontend: TanStack Query, mọi lời gọi qua `src/api`, 3 trạng thái, nút disabled khi gửi | Đạt | |
| Hệ thiết kế (cấm gradient/bóng/emoji, accent chỉ dữ liệu) | Đạt | Số lượt có thể dùng `text-accent` (là dữ liệu); nút không đổi màu |

## Project Structure

### Documentation (this feature)

```text
specs/004-ai-daily-limit/
├── spec.md · research.md · data-model.md · plan.md · tasks.md · quickstart.md
├── contracts/ai-quota.md
├── checklists/requirements.md
```

### Source Code (dự kiến)

```text
backend/
  QuizBattle.Domain/
    AiQuota/VietnamDay.cs                      # DayOf(utcNow), NextResetAt(day) - hàm thuần
    Entities/AiUsage.cs
  QuizBattle.Application/
    AiQuota/ IAiQuotaRepository, IAiQuotaService, AiQuotaService, AiQuotaOptions,
             AiQuotaScopes, AiReservation, AiReserveResult, AiQuotaResponse
    Common/AiDailyLimitException.cs
    Questions/QuestionService.cs               # GenerateAsync: reserve sau kiểm tra rẻ, refund khi lỗi AI
  QuizBattle.Infrastructure/
    Persistence/Configurations/AiUsageConfiguration.cs
    Persistence/AiQuotaRepository.cs           # SQL giữ chỗ/hoàn/đọc
    Migrations/*_AddAiUsageDaily.cs
  QuizBattle.Api/
    Controllers/AiQuotaController.cs           # GET /api/ai-quota
    Middleware/GlobalExceptionHandler.cs       # nhánh 429 cho AiDailyLimitException
    Program.cs                                 # OnRejected thêm code AI_RATE_LIMIT; bind options
    Extensions/ProductionConfigValidator.cs    # kiểm 2 biến Ai:DailyLimit*
  QuizBattle.Tests/
    Domain/VietnamDayTests.cs
    Questions/QuestionServiceQuotaTests.cs     # fake repo/generator
    Integration/AiQuotaReservationTests.cs     # Postgres thật: đồng thời, atomic, hoàn, xen kẽ
    Integration/AiQuotaGenerateFlowTests.cs    # QuestionService + repo thật + đồng hồ giả
frontend/src/
  types/aiQuota.ts · api/aiQuota.ts · hooks/useAiQuota.ts
  validation/aiQuota.ts                        # logic thuần: lý do chặn, thông báo
  pages/RoomPage/AiQuotaBadge.tsx              # hiển thị số lượt (tách để Panel ≤ ~150 dòng)
  pages/RoomPage/GenerateQuestionsPanel.tsx    # tắt nút, làm mới quota, map 429
.env.example                                   # thêm Ai__DailyLimitPerUser / Ai__DailyLimitGlobal
```

## Các đợt triển khai

Mỗi đợt kết thúc bằng checkpoint build + test xanh và push độc lập.

| Đợt | Nội dung | Chặn bởi |
|---|---|---|
| 1 | Backend nền + migration: Domain (`VietnamDay`), entity, migration, `AiQuotaRepository` + test tích hợp Postgres (đồng thời, atomic, hoàn, xen kẽ, ranh giới ngày, bằng chứng mutation) | — |
| 2 | Backend nối dây: exception + 429, options + validator, `AiQuotaService`, `QuestionService` (thứ tự kiểm tra, hoàn lượt), `GET /api/ai-quota`, `AI_RATE_LIMIT`, test luồng đầy đủ | Đợt 1 |
| 3 | Frontend: type/api/hook/logic thuần, `AiQuotaBadge`, panel (tắt nút, thông báo, làm mới), test component | Đợt 2 (contract) |
| 4 | Chốt: SQL idempotent, `.env.example`, chạy quickstart, build + test hai phía, push | Đợt 1–3 |

Contract (`contracts/ai-quota.md`) đã chốt trước nên Đợt 3 có thể bắt đầu song song với Đợt 2 nếu cần.

## Migration và triển khai (BẮT BUỘC làm trước khi merge)

CLAUDE.md: migration lên production chạy thủ công, có chủ đích, không tự chạy khi app khởi động.

1. Dev: `dotnet ef migrations add AddAiUsageDaily` (đọc kỹ file sinh ra: chỉ `CreateTable`, PK `(Day, Scope)`, CHECK `Count >= 0`), thử trên Postgres Docker (`docker compose up -d`).
2. Xuất SQL idempotent CHỈ từ migration đang có trên production (`20261112104137_AddAvatarShop`) tới `20261119094951_AddAiUsageDaily`: `dotnet ef migrations script 20261112104137_AddAvatarShop 20261119094951_AddAiUsageDaily --idempotent -p QuizBattle.Infrastructure -s QuizBattle.Api`. File lưu NGOÀI repo (`D:/IT_Code/Quiz_Battle/sql/004-AddAiUsageDaily.sql`, UTF-8 không BOM) để không lẫn vào mã nguồn. Kiểm tra chỉ có `CREATE TABLE "AiUsageDaily"` (PK, CHECK) và `INSERT INTO "__EFMigrationsHistory"`, mỗi khối bọc trong điều kiện chưa có `MigrationId`; không có `DROP`/`ALTER`.
3. Chạy script HAI LẦN liên tiếp trên Postgres dev để chứng minh idempotent (lần hai không lỗi, không đổi dữ liệu).
4. Chạy script lên Supabase qua **Session pooler (cổng 5432)** TRƯỚC khi merge `feat/ai-daily-limit` vào `main` (backend mới truy vấn `AiUsageDaily`; bảng chưa có thì mọi generate 500). Người chạy: chủ dự án; Claude không tự chạy lên production.
5. Đặt biến môi trường trên nơi deploy nếu muốn khác mặc định: `Ai__DailyLimitPerUser`, `Ai__DailyLimitGlobal` (global nên theo RPD của key Gemini — kiểm sau).
6. Thứ tự rollback: bảng mới không bị bảng cũ tham chiếu; gỡ code cũ vẫn chạy được khi bảng còn tồn tại.

## Rủi ro kỹ thuật cần để mắt

- EF `SqlQuery` không dùng được cho `INSERT ... RETURNING` (research R3) → dùng `DbCommand`.
- Test đồng thời cần mỗi tác vụ một `DbContext`/connection riêng (không chia sẻ một context giữa các task).
- Test "đỏ khi bỏ `WHERE`" phải được chạy thật rồi khôi phục câu SQL (T013), không chỉ khẳng định.
- `ApiError` có `code` nhưng chưa chắc có `resetAt` → xác nhận khi làm Đợt 3.
