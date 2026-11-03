using QuizBattle.Application.Common;
using QuizBattle.Application.Rooms;
using QuizBattle.Domain;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Game;

public sealed class GameService(
    IGameRepository gameRepository,
    ISessionTokenGenerator sessionTokenGenerator,
    TimeProvider timeProvider) : IGameService
{
    public Task<RoomResponse> OpenRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
        gameRepository.WithLockedRoomAsync(code.ToUpperInvariant(), snapshot =>
        {
            if (snapshot.Room.HostUserId != hostUserId)
            {
                throw new NotFoundException("Không tìm thấy phòng.");
            }

            if (snapshot.Room.Status != RoomStatus.Draft)
            {
                throw new ConflictException("Phòng phải đang ở trạng thái soạn câu hỏi.");
            }

            if (snapshot.ApprovedQuestions.Count == 0)
            {
                throw new ConflictException("Phòng chưa có câu hỏi nào được duyệt.");
            }

            snapshot.Room.Status = RoomStatus.Lobby;

            return new RoomResponse(
                snapshot.Room.Id,
                snapshot.Room.Code,
                snapshot.Room.Topic,
                snapshot.Room.Status.ToString(),
                snapshot.Room.QuestionTimeLimitSeconds,
                snapshot.Room.CreatedAt,
                snapshot.TotalQuestionCount,
                snapshot.Players.Count);
        }, cancellationToken);

    public async Task<CreatePlayerResponse> CreatePlayerAsync(string code, CreatePlayerRequest request, CancellationToken cancellationToken)
    {
        var normalizedCode = code.ToUpperInvariant();
        var displayName = request.DisplayName.Trim();
        var token = sessionTokenGenerator.Generate();
        var tokenHash = SessionTokenHasher.Hash(token);
        var joinedAt = timeProvider.GetUtcNow();

        var player = await gameRepository.AddPlayerAsync(normalizedCode, snapshot =>
        {
            if (snapshot.Room.Status != RoomStatus.Lobby)
            {
                throw new ConflictException("Phòng chưa mở hoặc đã bắt đầu, không thể vào.");
            }

            if (snapshot.Players.Count >= Room.MAX_PLAYERS)
            {
                throw new ConflictException($"Phòng đã đủ {Room.MAX_PLAYERS} người chơi.");
            }

            if (snapshot.Players.Any(p => p.DisplayName == displayName))
            {
                throw new ConflictException("Tên hiển thị đã có người dùng trong phòng.");
            }

            return new Player
            {
                Id = Guid.NewGuid(),
                RoomId = snapshot.Room.Id,
                DisplayName = displayName,
                SessionTokenHash = tokenHash,
                Score = 0,
                JoinedAt = joinedAt,
            };
        }, cancellationToken);

        return new CreatePlayerResponse(player.Id, token, normalizedCode);
    }

    public async Task<RoomStateDto> JoinAsHostAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
    {
        var snapshot = await gameRepository.FindSnapshotByCodeAsync(code.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

        if (snapshot.Room.HostUserId != hostUserId)
        {
            throw new NotFoundException("Không tìm thấy phòng.");
        }

        return BuildRoomState(snapshot, isHost: true, playerId: null, timeProvider.GetUtcNow());
    }

    public async Task<(RoomStateDto State, PlayerJoinedDto Broadcast)> JoinAsPlayerAsync(string code, string sessionToken, CancellationToken cancellationToken)
    {
        var tokenHash = SessionTokenHasher.Hash(sessionToken);
        var player = await gameRepository.FindPlayerByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Phiên không hợp lệ.");

        var snapshot = await gameRepository.FindSnapshotByCodeAsync(code.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

        if (snapshot.Room.Id != player.RoomId)
        {
            throw new UnauthorizedException("Phiên không khớp với phòng này.");
        }

        var state = BuildRoomState(snapshot, isHost: false, playerId: player.Id, timeProvider.GetUtcNow());
        var broadcast = new PlayerJoinedDto(player.Id, player.DisplayName);
        return (state, broadcast);
    }

    public Task<QuestionStartedDto> StartGameAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
        gameRepository.WithLockedRoomAsync(code.ToUpperInvariant(), snapshot =>
        {
            RequireHost(snapshot, hostUserId);

            if (snapshot.Room.Status != RoomStatus.Lobby)
            {
                throw new ConflictException("Phòng phải đang ở trạng thái chờ.");
            }

            if (snapshot.ApprovedQuestions.Count == 0)
            {
                throw new ConflictException("Phòng chưa có câu hỏi nào được duyệt.");
            }

            var now = timeProvider.GetUtcNow();
            snapshot.Room.Status = RoomStatus.InProgress;
            snapshot.Room.StartedAt = now;

            var first = snapshot.ApprovedQuestions[0];
            first.StartedAt = now;

            return ToQuestionStartedDto(first, index: 0, snapshot);
        }, cancellationToken);

    public Task<(QuestionEndedDto Ended, LeaderboardUpdatedDto Leaderboard)> EndQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
        gameRepository.WithLockedRoomAsync(code.ToUpperInvariant(), snapshot =>
        {
            RequireHost(snapshot, hostUserId);

            var current = snapshot.CurrentQuestion;
            if (current is null || current.EndedAt is not null)
            {
                throw new ConflictException("Không có câu nào đang chạy.");
            }

            // Chủ phòng được kết thúc sớm bất cứ lúc nào - không cần quá hạn hay đủ người trả lời.
            return EndCurrentQuestion(snapshot, current, timeProvider.GetUtcNow());
        }, cancellationToken);

    public Task<NextQuestionOutcome> NextQuestionAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
        gameRepository.WithLockedRoomAsync(code.ToUpperInvariant(), snapshot =>
        {
            RequireHost(snapshot, hostUserId);

            var current = snapshot.CurrentQuestion;
            if (current is null || current.EndedAt is null)
            {
                throw new ConflictException("Câu hiện tại chưa kết thúc.");
            }

            return AdvanceFrom(snapshot, current, timeProvider.GetUtcNow());
        }, cancellationToken);

    // Hai thao tác hệ thống cho GameTimerService / SubmitAnswer: không có host, điều kiện không còn đúng
    // (host hoặc timer khác đã làm trước) thì trả null chứ không ném lỗi - kiểm tra lại trong khóa FOR UPDATE.
    public Task<(QuestionEndedDto Ended, LeaderboardUpdatedDto Leaderboard)?> TryAutoEndQuestionAsync(Guid roomId, CancellationToken cancellationToken) =>
        gameRepository.WithLockedRoomByIdAsync<(QuestionEndedDto, LeaderboardUpdatedDto)?>(roomId, snapshot =>
        {
            var current = snapshot.CurrentQuestion;
            if (snapshot.Room.Status != RoomStatus.InProgress || current is null
                || !GameTimeline.ShouldEndQuestion(
                    current.StartedAt!.Value, current.EndedAt, snapshot.Room.QuestionTimeLimitSeconds,
                    snapshot.AnsweredCountForCurrentQuestion, snapshot.Players.Count, timeProvider.GetUtcNow()))
            {
                return null;
            }

            return EndCurrentQuestion(snapshot, current, timeProvider.GetUtcNow());
        }, cancellationToken);

    public Task<NextQuestionOutcome?> TryAutoAdvanceAsync(Guid roomId, CancellationToken cancellationToken) =>
        gameRepository.WithLockedRoomByIdAsync<NextQuestionOutcome?>(roomId, snapshot =>
        {
            var current = snapshot.CurrentQuestion;
            if (snapshot.Room.Status != RoomStatus.InProgress || current is null
                || !GameTimeline.ShouldAdvance(current.EndedAt, timeProvider.GetUtcNow()))
            {
                return null;
            }

            return AdvanceFrom(snapshot, current, timeProvider.GetUtcNow());
        }, cancellationToken);

    public Task<IReadOnlyList<DueRoom>> FindDueRoomsAsync(CancellationToken cancellationToken) =>
        gameRepository.FindDueRoomsAsync(timeProvider.GetUtcNow(), cancellationToken);

    public Task<(AnswerAckDto Ack, AnswerProgressDto Progress, Guid RoomId)> SubmitAnswerAsync(Guid playerId, Guid questionId, int selectedIndex, CancellationToken cancellationToken) =>
        gameRepository.SubmitAnswerAsync(playerId, snapshot =>
        {
            if (snapshot.Question is null || snapshot.Question.Id != questionId || snapshot.Question.EndedAt is not null)
            {
                throw new ConflictException("Không phải câu đang chạy.");
            }

            if (selectedIndex is < 0 || selectedIndex >= Question.OPTION_COUNT)
            {
                throw new ConflictException("Đáp án không hợp lệ.");
            }

            var now = timeProvider.GetUtcNow();
            var deadline = snapshot.Question.StartedAt!.Value.AddSeconds(snapshot.Room.QuestionTimeLimitSeconds);
            if (now > deadline)
            {
                throw new ConflictException("Đã hết hạn trả lời.");
            }

            var isCorrect = selectedIndex == snapshot.Question.CorrectIndex;
            var points = ScoreCalculator.Calculate(isCorrect, snapshot.Question.StartedAt.Value, now, snapshot.Room.QuestionTimeLimitSeconds);

            var answer = new Answer
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                QuestionId = questionId,
                SelectedIndex = selectedIndex,
                AnsweredAt = now,
                IsCorrect = isCorrect,
                PointsAwarded = points,
            };
            snapshot.Player.Score += points;

            var progress = new AnswerProgressDto(snapshot.AnsweredCountForCurrentQuestion + 1, snapshot.TotalPlayers);
            return (answer, (new AnswerAckDto(true), progress, snapshot.Room.Id));
        }, cancellationToken);

    private static (QuestionEndedDto, LeaderboardUpdatedDto) EndCurrentQuestion(GameSnapshot snapshot, Question current, DateTimeOffset now)
    {
        current.EndedAt = now;

        var ended = new QuestionEndedDto(
            current.Id, current.CorrectIndex, snapshot.AnsweredCountForCurrentQuestion, snapshot.Players.Count,
            now, now.AddSeconds(Room.RESULT_DISPLAY_SECONDS));
        var leaderboard = BuildLeaderboard(snapshot.Players, snapshot.Answers, snapshot.ApprovedQuestions);
        return (ended, new LeaderboardUpdatedDto(leaderboard));
    }

    private static NextQuestionOutcome AdvanceFrom(GameSnapshot snapshot, Question current, DateTimeOffset now)
    {
        var questions = snapshot.ApprovedQuestions;
        var currentIndex = questions.ToList().IndexOf(current);
        var next = currentIndex + 1 < questions.Count ? questions[currentIndex + 1] : null;

        if (next is not null)
        {
            next.StartedAt = now;
            return new NextQuestionOutcome(ToQuestionStartedDto(next, currentIndex + 1, snapshot), null);
        }

        snapshot.Room.Status = RoomStatus.Finished;
        snapshot.Room.EndedAt = now;
        var leaderboard = BuildLeaderboard(snapshot.Players, snapshot.Answers, snapshot.ApprovedQuestions);
        return new NextQuestionOutcome(null, new GameEndedDto(leaderboard));
    }

    private static void RequireHost(GameSnapshot snapshot, Guid hostUserId)
    {
        if (snapshot.Room.HostUserId != hostUserId)
        {
            throw new UnauthorizedException("Bạn không phải chủ phòng này.");
        }
    }

    private static QuestionStartedDto ToQuestionStartedDto(Question question, int index, GameSnapshot snapshot) => new(
        question.Id,
        index,
        snapshot.ApprovedQuestions.Count,
        question.Text,
        question.Options,
        question.StartedAt!.Value,
        question.StartedAt.Value.AddSeconds(snapshot.Room.QuestionTimeLimitSeconds),
        question.StartedAt.Value);

    private static RoomStateDto BuildRoomState(GameSnapshot snapshot, bool isHost, Guid? playerId, DateTimeOffset now)
    {
        var leaderboard = BuildLeaderboard(snapshot.Players, snapshot.Answers, snapshot.ApprovedQuestions);
        var current = snapshot.CurrentQuestion;
        CurrentQuestionStateDto? currentDto = null;
        bool? hasAnswered = null;

        if (current is not null)
        {
            var index = snapshot.ApprovedQuestions.ToList().IndexOf(current);
            var ended = current.EndedAt is not null;
            var deadline = current.StartedAt!.Value.AddSeconds(snapshot.Room.QuestionTimeLimitSeconds);
            currentDto = new CurrentQuestionStateDto(
                current.Id, index, snapshot.ApprovedQuestions.Count, current.Text, current.Options,
                current.StartedAt.Value, deadline, ended, ended ? current.CorrectIndex : null,
                ended ? current.EndedAt!.Value.AddSeconds(Room.RESULT_DISPLAY_SECONDS) : null);

            if (playerId is not null)
            {
                hasAnswered = snapshot.Answers.Any(a => a.PlayerId == playerId.Value && a.QuestionId == current.Id);
            }
        }

        return new RoomStateDto(
            snapshot.Room.Code,
            snapshot.Room.Status.ToString(),
            snapshot.Room.QuestionTimeLimitSeconds,
            snapshot.Players.Select(p => new PlayerSummaryDto(p.Id, p.DisplayName, p.Score)).ToList(),
            currentDto,
            leaderboard,
            isHost,
            playerId,
            hasAnswered,
            now);
    }

    // Hòa điểm: tổng thời gian trả lời đúng ít hơn đứng trên - chỉ tính các câu đã trả lời đúng.
    private static IReadOnlyList<LeaderboardEntryDto> BuildLeaderboard(
        IReadOnlyList<Player> players, IReadOnlyList<Answer> answers, IReadOnlyList<Question> questions)
    {
        var startedAtByQuestionId = questions
            .Where(q => q.StartedAt is not null)
            .ToDictionary(q => q.Id, q => q.StartedAt!.Value);

        return players
            .Select(p => new
            {
                Player = p,
                TotalCorrectMs = answers
                    .Where(a => a.PlayerId == p.Id && a.IsCorrect && startedAtByQuestionId.ContainsKey(a.QuestionId))
                    .Sum(a => (a.AnsweredAt - startedAtByQuestionId[a.QuestionId]).TotalMilliseconds),
            })
            .OrderByDescending(x => x.Player.Score)
            .ThenBy(x => x.TotalCorrectMs)
            .Select((x, i) => new LeaderboardEntryDto(x.Player.Id, x.Player.DisplayName, x.Player.Score, i + 1))
            .ToList();
    }
}
