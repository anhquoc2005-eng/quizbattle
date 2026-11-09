# Implementation Plan: Tài khoản người chơi, XP và Xu

**Branch**: `feat/player-xp-coins` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md) (đã clarify, mục Clarifications). Thiết kế chi tiết: [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md).

## Summary

Gắn `Player.UserId` khi người dùng đã đăng nhập vào phòng, và trao XP/Xu **ngay trong transaction chuyển phòng sang `Finished`** (đang giữ `SELECT ... FOR UPDATE` trên dòng Room). Toàn bộ luật thưởng là hàm thuần ở Domain (`RewardCalculator`, `LevelCalculator`); `GameService` chỉ gom dữ liệu, gọi hàm thuần, ghi `MatchReward` + `CoinTransaction` + cập nhật `Users`. Idempotency có hai lớp: (1) khóa phòng + chuyển trạng thái `InProgress -> Finished` chỉ xảy ra một lần, (2) unique index `MatchRewards(PlayerId)`. Frontend thêm trang hồ sơ, bảng xếp hạng XP, khối thưởng ở màn kết thúc, và hiển thị cấp độ trong phòng.

## Technical Context

**Language/Version**: C# 12 / .NET 8; TypeScript 5.8 (strict)

**Primary Dependencies**: ASP.NET Core 8, EF Core + Npgsql, SignalR; React 18, TanStack Query, `@microsoft/signalr`. **Không thêm thư viện runtime nào.** Chỉ có đề xuất devDependencies cho test component ở frontend (xem R9, cần bạn duyệt).

**Storage**: PostgreSQL (dev: docker compose, cổng 5433; prod: Supabase Session pooler). 1 migration EF mới.

**Testing**: xUnit (backend), Vitest (frontend). Thêm test tích hợp Postgres (không thêm package, tự bỏ qua khi thiếu biến môi trường).

**Target Platform**: Render (backend, một instance), Vercel (frontend)

**Project Type**: web application (backend + frontend)

**Performance Goals**: kết thúc trận 50 người xong trong một transaction ngắn (< 1 giây, SC-004: thấy thưởng trong 3 giây); hồ sơ/bảng xếp hạng < 2 giây (SC-005).

**Constraints**: Server là nguồn sự thật; không nhận điểm/thưởng từ client; số dư Xu không âm (CHECK); migration prod phải idempotent và an toàn với dữ liệu cũ.

**Scale/Scope**: tối đa 50 người chơi/phòng (`Room.MAX_PLAYERS`), bảng xếp hạng top 50.

## Constitution Check

Gate theo [constitution](../../.specify/memory/constitution.md) (CLAUDE.md thắng khi mâu thuẫn).

| Nguyên tắc | Kết quả |
|---|---|
| 1. Server là nguồn sự thật | Đạt. XP/Xu/hạng tính ở server từ `Player.Score` và `Answers` đã lưu; client chỉ nhận DTO. |
| 2. Phân lớp, DI qua interface, logic thuần ở Domain có test | Đạt. `RewardCalculator`/`LevelCalculator` ở Domain; thêm `IUserRepository` method và `IProfileService`; Controller/Hub mỏng. |
| 3. Bảo mật | Đạt. Không secret mới; endpoint hồ sơ `[Authorize]`; bảng xếp hạng chỉ lộ tên hiển thị + XP (không email). |
| 4. Quy trình | Đạt. Nhánh `feat/player-xp-coins`, commit nhỏ, build + test trước khi merge. |
| 5. Không thêm thư viện khi chưa hỏi | Đạt với backend. Frontend: **chờ duyệt** (R9). |

**Điểm cần bạn quyết (không tự ý làm theo CLAUDE.md)**:

1. Thêm mã lỗi SignalR `session_replaced` vào `GameErrorDto.Code` (CLAUDE.md đang liệt kê cố định 4 mã). Khuyến nghị: thêm, và sửa CLAUDE.md cùng lúc ([R5](research.md#r5-đổi-thiết-bị-fr-020)).
2. Test component frontend (CLAUDE.md ghi "Vitest, chỉ cho logic"). Khuyến nghị: duyệt `jsdom` + `@testing-library/react` cho 3 màn, và sửa CLAUDE.md ([R9](research.md#r9-test-frontend)).
3. Thứ hạng thưởng tính theo **điểm** (hòa điểm cùng hạng), còn bảng điểm hiện tại phân hạng hòa bằng tổng thời gian trả lời đúng. Hai hạng này có thể khác nhau; khuyến nghị giữ nguyên bảng điểm và hiển thị riêng "hạng thưởng" ở khối thưởng ([R3](research.md#r3-thứ-hạng-thưởng-và-hạng-trên-bảng-điểm)).

## Project Structure

### Documentation (this feature)

```text
specs/001-player-xp-coins/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── rest-api.md
│   └── signalr-events.md
└── tasks.md          # bước sau (/speckit-tasks), chưa tạo
```

### Source Code (thay đổi theo từng lớp)

```text
backend/
  QuizBattle.Domain/
    Entities/        User (+TotalXp, CoinBalance), Player (bỏ ghi chú "chưa dùng"), MatchReward, CoinTransaction (mới)
    Enums/           RewardStatus, RewardDenialReason, CoinTransactionReason (mới)
    RewardCalculator.cs, LevelCalculator.cs (mới, hàm thuần)
  QuizBattle.Application/
    Game/            GameService (sửa CreatePlayer/JoinAsPlayer/SubmitAnswer/AdvanceFrom), GameSnapshot (+Users, +Rewards, +NewMatchRewards, +NewCoinTransactions),
                     PlayerRewardDto, GameEndedDto/RoomStateDto/PlayerSummaryDto/LeaderboardEntryDto/PlayerJoinedDto (+Level, +Rewards)
    Profile/         IProfileService, ProfileService, ProfileDto, RewardHistoryItemDto, XpLeaderboardEntryDto (mới)
    Common/          SessionReplacedException (mới); UnauthorizedException (+Code tùy chọn)
  QuizBattle.Infrastructure/
    Persistence/     GameRepository (sửa), UserRepository (+truy vấn hồ sơ/xếp hạng), Configurations (User, Player, MatchReward, CoinTransaction), Migrations/<ts>_AddXpCoinsAndPlayerAccounts
  QuizBattle.Api/
    Controllers/     GameController (CreatePlayer đọc user tùy chọn), UsersController (me, rewards), LeaderboardsController
    Hubs/            GameHub (lưu hash phiên vào Context.Items), PlayerConnectionRegistry (mới), GameHubExceptionFilter (+session_replaced)
    Extensions/      HttpContextExtensions.GetOptionalUserId (mới)
    Middleware/      GlobalExceptionHandler (+extension "code")
  QuizBattle.Tests/  Domain/, Game/, Profile/, Api/, Integration/ (mới)
frontend/src/
  api/        profile.ts, leaderboards.ts, game.ts (createPlayer)
  types/      profile.ts (mới), game.ts (+level, +rewards), problemDetails.ts (+code)
  hooks/      useProfile.ts, useXpLeaderboard.ts
  pages/      ProfilePage/, XpRankingPage/ (mới); JoinPage, PlayPage/PlayFinished, RoomLivePage/LiveFinished, PlayLobby, LiveLobby (sửa)
  components/ RewardSummary.tsx, LevelBadge.tsx (mới); PlayerList, Leaderboard (hiện LevelBadge)
  realtime/   events.ts (không đổi tên sự kiện), sessionErrors.ts (+session_replaced), gameReducer.ts (lưu rewards)
scripts/migrate-prod.ps1  (không sửa; chỉ kiểm chứng theo quickstart)
.github/workflows/ci.yml  (thêm service postgres cho test tích hợp)
CLAUDE.md                 (cập nhật mã lỗi + phạm vi test nếu bạn duyệt)
```

**Structure Decision**: giữ nguyên cấu trúc dự án; không thêm project mới.

## Cách plan xử lý 7 điểm bắt buộc

| # | Yêu cầu | Quyết định tóm tắt | Chi tiết |
|---|---|---|---|
| 1 | Thưởng trong cùng transaction với `Status = Finished` | Thưởng được tạo trong nhánh cuối của `AdvanceFrom` (trong `WithLockedRoomAsync`); repo `SaveChanges` một lần. Unique `MatchRewards(PlayerId)` là lớp chặn thứ hai. Khóa hàng `Users` theo thứ tự `Id` chỉ khi sắp kết thúc trận. | [R1](research.md#r1-thưởng-trong-cùng-transaction-với-finished), [R2](research.md#r2-chống-mất-cập-nhật-trên-users) |
| 2 | Unique `(RoomId, UserId)` có điều kiện; tương tác với `IX_Players_RoomId_lower_DisplayName` | Index lọc `WHERE "UserId" IS NOT NULL`. Vào lại: dùng Player cũ, **bỏ qua tên gửi lên**, nên không đụng index tên. Vào lần đầu mới kiểm tra tên. | [R4](research.md#r4-một-tài khoản-một-player-mỗi-phòng-và-index-tên) |
| 3 | Số dư Xu, tổng XP, index xếp hạng | `Users.CoinBalance`, `Users.TotalXp` + CHECK `>= 0`; cấp tính từ XP; index lọc `TotalXp > 0` | [R6](research.md#r6-số-dư-xp-và-index-xếp-hạng), [data-model.md](data-model.md) |
| 4 | Ngắt kết nối cũ khi đổi thiết bị | Hash phiên trong `Context.Items` được so với DB ở mỗi `SubmitAnswer` (xác thực chính, đúng cả khi nhiều instance) + `PlayerConnectionRegistry` đẩy lỗi `session_replaced` và gỡ khỏi group (cố gắng tối đa, một instance) | [R5](research.md#r5-đổi-thiết-bị-fr-020) |
| 5 | Phân biệt "không gửi token" và "token hết hạn" | `OnAuthenticationFailed` ghi lý do vào `HttpContext.Items`; `GetOptionalUserId()` trả `null` khi không có header, ném 401 kèm `code` khi có header mà xác thực hỏng | [R7](research.md#r7-jwt-hết-hạn-khi-vào-phòng-fr-021), [contracts/rest-api.md](contracts/rest-api.md) |
| 6 | Migration chạy được bằng `migrate-prod.ps1` | Một migration, cột mới có DEFAULT 0, không backfill, không SQL không idempotent; kiểm chứng bằng chạy script hai lần trên DB dữ liệu cũ | [R8](research.md#r8-migration-và-triển-khai-production), [quickstart.md](quickstart.md) |
| 7 | Test | Danh sách theo từng FR + test đồng thời + hạ tầng test component | [R9](research.md#r9-test-frontend), [R10](research.md#r10-ma-trận-test-backend) |

## Complexity Tracking

Không có vi phạm hiến pháp cần biện minh. Hai chỗ phức tạp hơn mức tối thiểu, và lý do:

| Chỗ | Vì sao cần | Phương án đơn giản hơn bị loại vì |
|---|---|---|
| Khóa hàng `Users` khi kết thúc trận | Hai phòng kết thúc cùng lúc cho cùng một tài khoản sẽ ghi đè số dư của nhau | `UPDATE ... SET x = x + n` thuần cũng được, nhưng `GameService` cần số tổng sau cộng để báo lên cấp và vẫn phải chạy qua snapshot; khóa theo thứ tự `Id` rõ ràng hơn |
| `PlayerConnectionRegistry` | Đẩy thông báo "đã vào từ thiết bị khác" cho thiết bị cũ ngay | Chỉ kiểm hash khi `SubmitAnswer` đã đủ về an toàn, nhưng thiết bị cũ không biết mình bị đá cho tới khi bấm trả lời |
