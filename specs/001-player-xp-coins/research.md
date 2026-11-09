# Research: Tài khoản người chơi, XP và Xu

Mỗi mục: **Quyết định**, **Lý do**, **Phương án đã loại**. Đã đối chiếu với code hiện có (`GameService`, `GameRepository`, `GameHub`, `PlayerConfiguration`, `Program.cs`, `migrate-prod.ps1`). Không còn `NEEDS CLARIFICATION`.

## R1. Thưởng trong cùng transaction với `Finished`

**Hiện trạng**: duy nhất một chỗ đặt `Room.Status = Finished`: nhánh cuối của `GameService.AdvanceFrom`, gọi từ `NextQuestionAsync` (chủ phòng) và `TryAutoAdvanceAsync` (timer), cả hai qua `IGameRepository.WithLockedRoomAsync/ById` (`SELECT ... FOR UPDATE` trên Room, rồi `SaveChanges` + commit). Không có đường nào khác kết thúc phòng, nên "trận dừng giữa chừng" (FR-003) không bao giờ có thưởng theo cấu trúc.

**Quyết định**:

- Trong nhánh cuối của `AdvanceFrom`, sau khi đặt `Finished`, gọi `RewardCalculator.Calculate(...)` (Domain, hàm thuần) rồi áp kết quả lên snapshot: cộng `User.TotalXp`/`CoinBalance` (entity đang được EF theo dõi), thêm `MatchReward` và `CoinTransaction` vào `snapshot.NewMatchRewards`/`NewCoinTransactions`.
- `GameRepository.LockRoomAsync` đã gọi `SaveChanges` một lần rồi commit. Thêm một dòng trước `SaveChanges`: `db.MatchRewards.AddRange(snapshot.NewMatchRewards)` và `db.CoinTransactions.AddRange(...)`. Thay đổi `Room`, `Users`, `MatchRewards`, `CoinTransactions` nằm trong **một** `SaveChanges` trong **một** transaction: hoặc có đủ, hoặc không có gì.
- Lớp chặn 1 (logic): người đến sau thấy `Status != InProgress` (`TryAutoAdvance` trả `null`, `NextQuestion` ném `ConflictException`), không chạy lại nhánh cuối.
- Lớp chặn 2 (DB): unique index `IX_MatchRewards_PlayerId`. Nếu vi phạm (chỉ xảy ra khi có bug), transaction rollback, phòng ở lại `InProgress`, timer thử lại mỗi giây và log lỗi. Chấp nhận: dễ phát hiện hơn là cộng đôi âm thầm.
- `GameEndedDto` có thêm `Rewards` (xem contracts). `GameBroadcaster.NextQuestionAsync` đã gửi `outcome.GameEnded` nên cả đường chủ phòng lẫn timer cùng nhận payload mới mà không sửa thêm.
- `RoomStateDto` có thêm `Rewards` khi phòng `Finished`, để người rớt mạng đúng lúc kết thúc vẫn thấy thưởng khi `JoinRoom` lại (FR-003, edge case mất mạng). Dữ liệu đọc từ bảng `MatchRewards`, không tính lại.

**Phương án đã loại**: tính thưởng ở service riêng sau khi commit (có khe hở: phòng `Finished` nhưng chưa thưởng nếu sập giữa chừng); job nền quét phòng `Finished` chưa thưởng (thêm trạng thái, thêm timer, khó chứng minh "đúng một lần").

## R2. Chống mất cập nhật trên `Users`

Cùng một tài khoản có thể ở hai phòng khác nhau và hai phòng kết thúc cùng lúc (khóa Room khác nhau, nên không chặn nhau). Đọc-sửa-ghi `TotalXp`/`CoinBalance` qua entity theo dõi sẽ mất cập nhật.

**Quyết định**: khi phòng sắp kết thúc, repo nạp các `User` của người chơi có tài khoản bằng `SELECT * FROM "Users" WHERE "Id" = ANY(...) ORDER BY "Id" FOR UPDATE`. Thứ tự `Id` cố định giữ các transaction không bị deadlock (mọi transaction khóa Room trước rồi Users theo `Id` tăng dần). Tính năng cửa hàng sau này phải khóa dòng `Users` của người mua theo cùng quy tắc (ghi vào tài liệu tính năng 2).

"Sắp kết thúc" = `Room.Status == InProgress` và câu đã duyệt cuối cùng đã `StartedAt` và đã `EndedAt` (đúng điều kiện `AdvanceFrom` sẽ rơi vào nhánh cuối). Repo tính điều kiện này từ dữ liệu vừa nạp và đặt vào `GameSnapshot.IsFinalAdvance`. Các lần gọi khác chỉ nạp `User` không khóa (`AsNoTracking`) để hiển thị cấp độ.

**Loại**: `ExecuteUpdate` `TotalXp = TotalXp + n` không cần khóa, nhưng service không biết tổng mới để báo "lên cấp" trừ khi đọc lại; thêm `RETURNING` làm lệch kiểu viết của repo hiện tại (entity + `SaveChanges`). Dùng cột `xmin` làm concurrency token: phải viết vòng thử lại.

## R3. Thứ hạng thưởng và hạng trên bảng điểm

**Hiện trạng**: `BuildLeaderboard` sắp theo `Score` giảm dần rồi `TotalCorrectMs` tăng dần, gán `Rank = i + 1` (không bao giờ đồng hạng). Spec/Clarifications: hòa **điểm** là cùng hạng (1, 1, 3), thưởng theo hạng đó, xếp hạng tính trên **mọi** người chơi (kể cả khách, chủ phòng tự chơi).

**Quyết định**: `RewardCalculator` tự tính "hạng thưởng" (competition ranking theo `Score` duy nhất, bỏ qua thời gian) trên toàn bộ `snapshot.Players`. **Không** đổi `BuildLeaderboard`/`Rank` hiện có (tránh phá màn đang chạy). `PlayerRewardDto` mang `RewardRank` riêng; màn kết thúc hiển thị "Hạng thưởng" cạnh khoản thưởng.

**Hệ quả cần chấp nhận**: hai người cùng điểm hiện là hạng 1 và 2 trên bảng điểm nhưng cùng nhận thưởng hạng 1. Nếu bạn muốn hai hạng này trùng nhau, phải đổi `BuildLeaderboard` thành đồng hạng và đổi cách hiển thị (ngoài phạm vi bước này).

Thưởng hạng chỉ khi `Score > 0` và hạng ≤ 3. `RankBonus`: 30/20/10.

## R4. Một tài khoản một Player mỗi phòng, và index tên

**Index mới**: `IX_Players_RoomId_UserId` unique trên `(RoomId, UserId)` với `HasFilter("\"UserId\" IS NOT NULL")`. EF Core khai báo được bằng fluent API (khác index `lower(DisplayName)` phải dùng SQL thô). Khách (`UserId` NULL) không bị ảnh hưởng.

**Luồng `CreatePlayerAsync` mới** (REST `POST /api/rooms/{code}/players`), trong `AddPlayerAsync` đã khóa Room `FOR UPDATE`:

1. Có `userId` và đã có Player `(RoomId, userId)`: **dùng lại Player đó**, xoay `SessionTokenHash` sang token mới, **không đọc `request.DisplayName`**. Cho phép khi phòng `Lobby` hoặc `InProgress` (cần để kết nối lại giữa trận; hiện code chặn mọi trạng thái khác `Lobby`). `Finished`/`Draft`: giữ 409 như cũ.
2. Chưa có Player: chạy đúng các kiểm tra cũ (phải `Lobby`, chưa đủ 50 người, tên chưa trùng không phân biệt hoa thường), tạo Player có `UserId`.

**Tương tác với `IX_Players_RoomId_lower_DisplayName`**: vào lại không bao giờ INSERT nên không thể đụng index tên, kể cả khi người dùng gửi tên khác (tên mới bị bỏ qua, giữ tên cũ; điểm và lịch sử không đổi). Vào lần đầu mà tên đã bị người khác (khách hoặc tài khoản) giữ: 409 "Tên hiển thị đã có người dùng trong phòng", người dùng sửa tên ở /join rồi gửi lại. Hai tab cùng gửi lần đầu cùng lúc: khóa Room tuần tự hóa, tab thứ hai rơi vào nhánh 1 (xoay token, tab đầu mất phiên, đúng ý FR-020).

Bắt `PostgresException 23505` theo `ConstraintName` trong `AddPlayerAsync`: `IX_Players_RoomId_lower_DisplayName` giữ nguyên ánh xạ cũ; `IX_Players_RoomId_UserId` thêm ánh xạ sang `ConflictException("Bạn đã ở trong phòng này.")` (lớp chặn thứ hai, hiếm khi tới vì đã khóa phòng).

`IGameRepository.AddPlayerAsync` đổi tên thành `JoinPlayerAsync`: callback có thể trả Player đã theo dõi (rejoin) hoặc mới; repo chỉ `Add` khi `db.Entry(player).State == Detached`.

Host tự vào phòng mình làm người chơi: tạo Player có `UserId = hostUserId` như mọi tài khoản; bị loại khỏi thưởng ở `RewardCalculator` (so với `Room.HostUserId`).

## R5. Đổi thiết bị (FR-020)

**Vấn đề**: `GameHub.JoinRoom` lưu `playerId` vào `Context.Items` của kết nối; `SubmitAnswer` chỉ đọc `playerId` từ đó, không kiểm tra token nữa. Đổi token trong DB không ảnh hưởng kết nối cũ.

**Quyết định (hai lớp)**:

1. **Lớp xác thực chính, không giữ trạng thái**: `JoinAsPlayerAsync` trả thêm `SessionTokenHash`; Hub lưu vào `Context.Items["sessionTokenHash"]`. `SubmitAnswerAsync(playerId, sessionTokenHash, ...)` so với `snapshot.Player.SessionTokenHash` (Player đã được nạp sẵn trong transaction, không thêm truy vấn). Không khớp: ném `SessionReplacedException` (kế thừa ý nghĩa 401). Chạy đúng trên mọi instance vì chỉ dựa vào DB. `LeaveRoom` của kết nối cũ cũng bị bỏ qua nếu hash không khớp (tránh phát `PlayerLeft` sai).
2. **Lớp ngắt chủ động, một instance**: `PlayerConnectionRegistry` (singleton, `ConcurrentDictionary<Guid playerId, (string ConnectionId, string TokenHash)>`) cập nhật ở `JoinRoom`/`LeaveRoom`/`OnDisconnectedAsync` (chỉ xóa nếu `ConnectionId` còn khớp). Khi `JoinRoom` mới của cùng Player đến mà registry đang giữ kết nối khác **với hash khác**: `Groups.RemoveFromGroupAsync(oldConnectionId, code)` rồi gửi `Error { method: "JoinRoom", code: "session_replaced", message }` cho kết nối cũ. Cùng hash (làm mới trang trên cùng thiết bị) thì chỉ thay entry, không đá.

**Giới hạn phải ghi rõ**:

- Hub không thể `Abort` kết nối khác từ bên ngoài (`Context.Abort()` chỉ gọi được trong chính kết nối đó). Ngắt = gỡ khỏi group + từ chối mọi hành động ghi + yêu cầu client tự đóng.
- Nhiều instance: lớp 1 vẫn đúng; lớp 2 chỉ hoạt động khi hai kết nối cùng instance, và thiết bị cũ có thể còn nhận broadcast (chỉ đọc, dữ liệu nó vốn được xem) cho tới khi nó hành động hoặc rớt mạng. Ứng dụng hiện đã giả định một instance (không có backplane cho group, `GameTimerService` quét toàn DB, không phân vùng), nên không phải hạn chế mới.
- Client cũ rớt mạng rồi tự kết nối lại (`withAutomaticReconnect`) sẽ `JoinRoom` bằng token cũ và nhận `unauthorized` (token không còn trong DB), xóa phiên đã lưu theo quy tắc hiện có.

**Mã lỗi**: thêm `session_replaced` vào `GameErrorDto.Code` và `GameError['code']` ở frontend; `GameHubExceptionFilter` map `SessionReplacedException` sang mã này; client hiển thị thông báo và dừng kết nối lại. Cần sửa danh sách mã trong CLAUDE.md (đã nêu ở plan, chờ duyệt).

**Loại**: lưu `ConnectionId` vào cột trên Player (ghi DB mỗi lần vào/ra, vẫn không ngắt được kết nối); chỉ dùng registry (sai khi nhiều instance và sau khi restart process).

## R6. Số dư, XP và index xếp hạng

- `Users.TotalXp int NOT NULL DEFAULT 0`, `CHECK ("TotalXp" >= 0)`. Chỉ tăng: mọi đường ghi đều qua `RewardCalculator` (XP không âm).
- `Users.CoinBalance int NOT NULL DEFAULT 0`, `CHECK ("CoinBalance" >= 0)`. Cập nhật trong cùng `SaveChanges` với dòng `CoinTransaction` tương ứng. Bất biến: `CoinBalance == SUM(CoinTransactions.Amount)` theo `UserId` (kiểm tra trong test tích hợp và `quickstart.md`).
- Cấp độ **không lưu cột**: `LevelCalculator.FromXp(totalXp)` (Domain). Ngưỡng cấp n = `100 * n * (n - 1)` XP (cấp 2 ở 200, cấp 3 ở 600, cấp 4 ở 1200), đúng quy tắc "n lên n+1 cần 200 × n".
- `int` đủ dùng: mỗi trận tối đa ~30 câu × 1000 điểm / 10 = 3000 XP; vài trăm nghìn trận mới tràn.
- Index xếp hạng: `IX_Users_TotalXp` trên `(TotalXp DESC, CreatedAt, Id)` với `HasFilter("\"TotalXp\" > 0")`. Truy vấn `WHERE TotalXp > 0 ORDER BY TotalXp DESC, CreatedAt, Id LIMIT 50` đi thẳng vào index, và tài khoản 0 XP không nằm trong index (FR-016).
- Bản ghi `CoinTransaction` chỉ tạo khi `Amount != 0` (`CHECK ("Amount" <> 0)`); lý do là enum `CoinTransactionReason { MatchReward, ShopPurchase }` (`ShopPurchase` để dành cho tính năng 2, chưa dùng; cột lưu dạng chuỗi để thêm giá trị không cần migration kiểu).

## R7. JWT hết hạn khi vào phòng (FR-021)

**Vấn đề thực tế**: `POST /api/rooms/{code}/players` là `[AllowAnonymous]`. Với `AddJwtBearer`, token hết hạn/sai chữ ký **không** làm request thất bại ở endpoint ẩn danh: `User` chỉ là ẩn danh, lỗi bị nuốt. Nếu chỉ dùng `User.Identity.IsAuthenticated`, người dùng đã đăng nhập nhưng hết hạn sẽ âm thầm trở thành khách (đúng điều FR-021 cấm).

**Quyết định**:

- `JwtBearerEvents.OnAuthenticationFailed` (đã có `Events` trong `Program.cs`): ghi `context.HttpContext.Items["auth.failure"] = context.Exception is SecurityTokenExpiredException ? "token_expired" : "token_invalid"`. Không log nội dung token.
- `HttpContextExtensions.GetOptionalUserId(this HttpContext)`: không có header `Authorization` → `null` (khách); có `User` đã xác thực → `Guid`; có header nhưng không xác thực → ném `UnauthorizedException(message, code)` với `code` từ `Items`.
- `GameController.CreatePlayer` gọi `HttpContext.GetOptionalUserId()` rồi truyền `Guid?` vào `IGameService.CreatePlayerAsync`. Service không biết HTTP.
- `UnauthorizedException` thêm tham số `Code` tùy chọn; `GlobalExceptionHandler` ghi vào `ProblemDetails.Extensions["code"]` (đúng format chung của CLAUDE.md: thêm trường mở rộng, không đổi cấu trúc). Hai mã: `token_expired`, `token_invalid`.
- Token hợp lệ nhưng `User` không còn tồn tại: `token_invalid`.

**Frontend**: `request()` hiện tự `clearAuth()` + `onUnauthorized` khi 401 mà có token. Giữ nguyên (token hết hạn thì xóa là đúng). `JoinPage` đọc `error.problem.code === 'token_expired'`: hiện khối "Phiên đăng nhập đã hết hạn" với hai nút "Đăng nhập lại" (về `/login`, quay lại `/join?code=...`) và "Vào làm khách" (gửi lại; lúc này không còn token nên là khách). `token_invalid` xử lý giống `token_expired`. Có thể kiểm tra trước bằng `isTokenExpired` (đã có ở `src/api`) để báo ngay khi mở trang.

Hub `JoinRoom` cho người chơi dùng `sessionToken`, không dùng JWT, nên không đổi. JWT hết hạn khi chủ phòng `JoinRoom` đã có đường xử lý riêng (`UnauthorizedException`), ngoài phạm vi.

## R8. Migration và triển khai production

Một migration `AddXpCoinsAndPlayerAccounts` do `dotnet ef migrations add` sinh, gồm:

- `Users`: thêm `TotalXp`, `CoinBalance` (`int NOT NULL DEFAULT 0`) và hai CHECK. Postgres 11+ thêm cột có DEFAULT hằng không viết lại bảng; mọi dòng cũ có 0/0 (đúng giả thiết "Users cũ có XP/Xu = 0").
- Bảng mới `MatchRewards`, `CoinTransactions` (rỗng) kèm index/ràng buộc.
- `Players`: thêm unique index lọc `(RoomId, UserId) WHERE "UserId" IS NOT NULL`. Mọi Player cũ có `UserId` NULL nên không dòng nào vào index; không thể vi phạm khi tạo. Cột `UserId` và khóa ngoại đã tồn tại từ trước, không đổi.
- Index `IX_Users_TotalXp` lọc `TotalXp > 0`: bảng rỗng về mặt lọc, tạo tức thì.
- Không có bước chuyển dữ liệu, không SQL thô. Vì vậy FR-012 (không cộng hồi tố) đúng theo cấu trúc: phòng `Finished` cũ không có `MatchRewards`, và không có mã nào quét lại.

**Chạy bằng `scripts/migrate-prod.ps1`**: script gọi `dotnet ef migrations script --idempotent` nên SQL sinh ra bọc từng migration trong kiểm tra `__EFMigrationsHistory`; chạy lại không lỗi. Không cần sửa script. Cần kiểm chứng (xem quickstart): (a) dựng DB dev ở trạng thái trước migration có dữ liệu mẫu (User, Room Finished, Player khách), (b) chạy script hai lần, (c) kiểm tra dữ liệu cũ nguyên vẹn và CHECK/index tồn tại.

**Thứ tự triển khai**: áp migration **trước**, rồi mới deploy backend. Backend cũ chạy được trên schema mới (cột mới có DEFAULT, bảng mới không được dùng); backend mới không chạy được trên schema cũ. `CREATE INDEX` thường khóa ghi `Players` trong giây lát; bảng nhỏ nên chấp nhận, chạy ngoài giờ có trận.

`dotnet ef database update` không dùng trên production (đúng CLAUDE.md); chỉ dùng ở dev để thử.

## R9. Test frontend

Hiện Vitest chạy `environment: 'node'`, `include: ['src/**/*.test.ts']`, đúng CLAUDE.md "chỉ cho logic".

**Quyết định mặc định (không cần thư viện mới)**: đẩy mọi quyết định hiển thị vào hàm thuần và test bằng Vitest node như đang làm: `describeReward(reward)` (mã lý do sang câu tiếng Việt, có/không thưởng hạng, có lên cấp), `levelProgress(...)` chỉ là định dạng dữ liệu server gửi (server tính, client không lặp công thức), `describeJoinAuthError(problem)` (hết hạn/không hợp lệ/khác), mở rộng `sessionErrors.ts` cho `session_replaced`, mở rộng `gameReducer.test.ts` cho `GameEnded.rewards`.

**Đề xuất cần duyệt**: thêm hạ tầng test component cho ba màn có nhiều trạng thái: `PlayFinished`/`LiveFinished` (khối thưởng, 5 lý do không thưởng), `ProfilePage` (đang tải/lỗi/rỗng/có dữ liệu), `JoinPage` (hộp thoại hết hạn). Gói: `jsdom` và `@testing-library/react` (devDependencies, không vào bản build). Cấu hình: giữ `environment: 'node'` mặc định, mở rộng `include` thêm `src/**/*.test.tsx`, mỗi file component khai báo `// @vitest-environment jsdom`. Lý do nên làm: CLAUDE.md bắt buộc ba trạng thái tải/lỗi/rỗng mà hiện không có cách nào kiểm tra; rủi ro thấp vì chỉ là devDependency. Nếu duyệt, sửa dòng "Vitest (frontend, chỉ cho logic)" trong CLAUDE.md. Nếu không duyệt: giữ phần hàm thuần, ba màn kiểm bằng `quickstart.md` thủ công.

## R10. Ma trận test backend

Tên theo mẫu `Method_Điều kiện_KếtQuả`.

**Domain (`Tests/Domain/`, hàm thuần, không DB)** — `RewardCalculatorTests`, `LevelCalculatorTests`:

| FR | Test |
|---|---|
| 004 | `Calculate_OnlyOneAnsweredAccount_NoOneRewarded`; `Calculate_TwoAccountsOneNeverAnswered_NoOneRewarded`; `Calculate_HostAndOneAccount_NotEnoughAccounts`; `Calculate_OneAccountManyGuests_NoOneRewarded` |
| 005 | `Calculate_HostPlaysOwnRoom_HostNotRewardedButOthersAre` |
| 006 | `Calculate_GuestPlayer_NeverRewarded` |
| 007 | `CalculateXp_Score999_Returns99`; `CalculateXp_ZeroScore_ReturnsZero` |
| 008 | `CalculateCoins_Score250Rank4_Returns2`; `...Rank1_Adds30`; `...Rank2_Adds20`; `...Rank3_Adds10`; `...ZeroScoreRank1_NoRankBonus` |
| 008 (hạng) | `Rank_TiedAtTop_BothRank1AndNextIsRank3` (1,1,3); `Rank_GuestTakesFirst_AccountSecondGetsRank2Bonus`; `Rank_HostCountsInRankingButNotRewarded` |
| 009 | `LevelFromXp_0_Returns1`; `...199_Returns1`; `...200_Returns2`; `...599_Returns2`; `...600_Returns3`; `XpForLevel_RoundTripsWithLevelFromXp` |
| 013 | `Calculate_DenialReasonPrecedence_GuestThenHostThenNoAnswersThenNotEnoughAccounts` |

**Service (`Tests/Game/GameServiceTests.cs` mở rộng, repo giả tự viết; sửa `FakeGameRepository` thêm Users/MatchRewards/CoinTransactions)**:

| FR | Test |
|---|---|
| 001/002 | `CreatePlayer_LoggedInUser_SetsUserId`; `CreatePlayer_GuestWithoutToken_UserIdNull`; `CreatePlayer_SameAccountAgain_ReusesPlayerAndRotatesToken`; `CreatePlayer_RejoinWithDifferentName_KeepsOriginalName`; `CreatePlayer_RejoinDuringInProgress_Allowed`; `CreatePlayer_NewAccountDuringInProgress_Conflict` |
| 003 | `AdvanceToFinished_AwardsEachEligiblePlayerOnce`; `NextQuestion_CalledTwiceAtEnd_SecondConflictAndNoDoubleReward`; `TryAutoAdvance_AfterHostFinished_ReturnsNullAndNoDoubleReward`; `AdvanceToFinished_RewardsAndStatusPersistedTogether` (fake repo: lỗi ở bước ghi thưởng thì `Status` không đổi) |
| 003 (đồng thời) | `NextQuestionAndAutoAdvance_RunConcurrently_RewardedExactlyOnce` (fake repo mô phỏng khóa bằng `SemaphoreSlim`, chạy `Task.WhenAll`) |
| 004–006 | `GameEnded_InvalidMatch_AllRewardsHaveReason`; `GameEnded_HostAsPlayer_HostRewardNotAwarded` |
| 007/008 | `GameEnded_RewardsMatchFormula` (số trong DTO khớp XP/Xu đã cộng vào User) |
| 009/010 | `GameEnded_LevelUp_FlagsLeveledUp`; `GameEnded_CoinTransactionHasReasonAmountRoom`; `GameEnded_ZeroCoins_NoCoinTransactionCreated` |
| 012 | `GameEnded_GuestPlayer_NoMatchRewardRow`; `Finished_OldRoomWithoutRewards_NotRewardedOnRoomState` |
| 018 | `SubmitAnswer_ScoreNotTakenFromClient` (đã có, giữ) |
| 019 | `GameEnded_PlayerLeftAfterAnswering_StillRewarded` (LeaveRoom không xóa Player) |
| 020 | `SubmitAnswer_StaleSessionHash_ThrowsSessionReplaced`; `SubmitAnswer_CurrentSessionHash_Works`; `JoinAsPlayer_ReturnsCurrentTokenHash` |
| 021 | `CreatePlayer_NoToken_IsGuest` (qua `userId null`) ; `CreatePlayer_UserNoLongerExists_ThrowsTokenInvalid` |

**Api (`Tests/Api/`, không cần DB)**:

| FR | Test |
|---|---|
| 021 | `GetOptionalUserId_NoAuthorizationHeader_ReturnsNull`; `..._ValidToken_ReturnsUserId`; `..._ExpiredToken_ThrowsTokenExpired`; `..._GarbageToken_ThrowsTokenInvalid`; `GlobalExceptionHandler_UnauthorizedWithCode_WritesCodeExtension` |
| 020 | `PlayerConnectionRegistry_NewConnectionSameHash_NoKick`; `..._NewConnectionDifferentHash_ReturnsOldConnection`; `..._DisconnectOfStaleConnection_DoesNotRemoveNewEntry` |
| - | `GameHubExceptionFilter_SessionReplaced_SendsSessionReplacedCode` |

**Tích hợp Postgres (`Tests/Integration/`, mới)** — chạy trên DB thật, kiểm tra điều mà bản giả không chứng minh được:

| Test | Chứng minh |
|---|---|
| `NextQuestion_TwoConcurrentCallsAtFinalQuestion_RewardedExactlyOnce` | khóa `FOR UPDATE` thật + chỉ một lần cộng; `CoinBalance == SUM(CoinTransactions)` |
| `TwoRoomsFinishingConcurrentlyForSameUser_NoLostUpdate` | R2 (khóa `Users` theo `Id`), không deadlock |
| `MatchRewards_DuplicatePlayerId_ViolatesUniqueIndex` | lớp chặn 2 |
| `Users_NegativeCoinBalance_ViolatesCheck` | CHECK `CoinBalance >= 0` |
| `Players_SameUserSameRoom_ViolatesPartialUnique`; `Players_ManyGuestsSameRoom_Allowed` | index lọc `UserId IS NOT NULL` |
| `Players_RejoinWithOtherName_DoesNotTouchNameIndex` | R4 |
| `XpLeaderboard_ExcludesZeroXpAndReturnsTop50InOrder` | FR-016 + index |

Hạ tầng: lớp `RequiresDatabaseFactAttribute : FactAttribute` đặt `Skip` trong constructor khi biến `QUIZBATTLE_TEST_DB` không có (xUnit 2.9 thuần, không thêm package). Mỗi test tạo schema riêng (`CREATE SCHEMA test_<guid>` hoặc DB tạm) rồi `Migrate()`, để cũng kiểm chứng luôn migration chạy từ đầu. CI: thêm `services: postgres:16-alpine` vào job backend của `ci.yml` và đặt biến môi trường. Test cục bộ: dùng DB docker compose (cổng 5433).

**Đã có và giữ**: `ScoreCalculatorTests`, chặn trả lời trùng (`IX_Answers_PlayerId_QuestionId`), `AuthServiceTests`, `TokenServiceTests`.

## R11. Các quyết định nhỏ khác

- **Bảng xếp hạng công khai** (`GET /api/leaderboards/xp`, `[AllowAnonymous]`): chỉ trả `displayName`, `level`, `totalXp`; không email, không Id người dùng. Khách xem được (khuyến khích đăng ký). Nếu muốn riêng tư: đổi sang `[Authorize]`, không ảnh hưởng thiết kế khác.
- **Hồ sơ**: `GET /api/users/me` (cấp, tổng XP, tiến trình, số dư Xu) và `GET /api/users/me/rewards?limit=20` (lịch sử các trận gần đây, từ `MatchRewards` + `Rooms` qua projection một truy vấn, không N+1, `AsNoTracking`). Chỉ liệt kê trận có `Status = Awarded`. Không làm API lịch sử Xu chung (tính năng 2).
- **Cấp độ cạnh tên** (FR-015): `PlayerSummaryDto`, `PlayerJoinedDto`, `LeaderboardEntryDto` thêm `Level` (`int?`, `null` với khách). Cấp chỉ đổi khi trận kết thúc nên không cần sự kiện cập nhật cấp giữa trận.
- **/join** (FR-017): thông điệp "tính thưởng" chỉ dựa vào việc đã đăng nhập (client biết), không cần API. Thông điệp không cam kết có thưởng: "XP và Xu được tính khi trận có ít nhất 2 tài khoản tham gia trả lời", vì điều kiện thật chỉ biết khi trận kết thúc.
- **Rate limit**: `POST .../players` giữ chính sách `join-room` (theo IP). Endpoint đọc hồ sơ/bảng xếp hạng rẻ (đi index, tối đa 50 dòng) nên không thêm chính sách mới.
- **`Player.UserId` đổi ghi chú**: bỏ comment "CHƯA dùng ở bước này".
