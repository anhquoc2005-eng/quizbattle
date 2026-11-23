using QuizBattle.Application.Common;
using QuizBattle.Domain.AiQuota;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Practice;

public sealed class PracticeService(
    IPracticeRepository repository,
    PracticeOptions options,
    TimeProvider timeProvider,
    Random? random = null) : IPracticeService
{
    private readonly Random _random = random ?? Random.Shared;

    public async Task<PracticeRoomsResponse> GetRoomsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var day = VietnamDay.DayOf(now);
        var rows = await repository.ListRoomsAsync(userId, cancellationToken);
        var today = await repository.GetXpTodayAsync(userId, day, cancellationToken);

        return new PracticeRoomsResponse(
            options.XpPerCorrect,
            options.DailyXpCap,
            today,
            Remaining(today),
            VietnamDay.NextResetAt(day),
            rows.Select(r => new PracticeRoomDto(r.Code, r.Topic, r.FinishedAt, r.QuestionCount, r.Role, r.OpenSessionId)).ToList());
    }

    public async Task<PracticeSessionStartDto> StartSessionAsync(Guid userId, StartPracticeSessionRequest request, CancellationToken cancellationToken)
    {
        // Cùng một phản hồi cho "không có phòng" và "không đủ điều kiện" để không lộ phòng của người khác.
        var code = (request.RoomCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsRoomCodeShape(code))
        {
            throw new RequestValidationException("roomCode", "Mã phòng gồm 6 ký tự chữ và số.", "invalid_room_code");
        }

        var room = await repository.FindPlayableRoomAsync(userId, code, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng để ôn tập.", "not_found");

        var shuffled = PracticeShuffler.Shuffle(room.QuestionIds, _random);
        var started = await repository.CreateOrGetSessionAsync(userId, room, shuffled, timeProvider.GetUtcNow(), cancellationToken);
        return new PracticeSessionStartDto(new PracticeSessionRefDto(started.SessionId, started.Total), started.Created);
    }

    public async Task<PracticeSessionDto> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var state = await repository.GetSessionAsync(userId, sessionId, now, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lượt ôn tập.", "not_found");

        PracticeSummaryDto? summary = null;
        if (state.Completed)
        {
            var day = VietnamDay.DayOf(now);
            var today = await repository.GetXpTodayAsync(userId, day, cancellationToken);
            summary = new PracticeSummaryDto(state.CorrectCount, state.XpGainedInSession, today, Remaining(today), VietnamDay.NextResetAt(day));
        }

        return new PracticeSessionDto(state.SessionId, state.Topic, state.Total, state.AnsweredCount, state.Completed, state.Current, state.LastResult, summary);
    }

    public async Task<PracticeAnswerResultDto> SubmitAnswerAsync(Guid userId, Guid sessionId, SubmitPracticeAnswerRequest request, CancellationToken cancellationToken)
    {
        if (request.QuestionId == Guid.Empty)
        {
            throw new RequestValidationException("questionId", "Thiếu questionId.", "missing_question_id");
        }

        if (request.SelectedIndex is < 0 or >= Question.OPTION_COUNT)
        {
            throw new RequestValidationException("selectedIndex", $"selectedIndex phải từ 0 đến {Question.OPTION_COUNT - 1}.", "invalid_selected_index");
        }

        var now = timeProvider.GetUtcNow();
        var day = VietnamDay.DayOf(now);
        var outcome = await repository.SubmitAnswerAsync(
            new PracticeSubmitCommand(userId, sessionId, request.QuestionId, request.SelectedIndex, now, day, options.XpPerCorrect, options.DailyXpCap),
            cancellationToken);

        // Số tổng của ngày đọc lại SAU khi nộp (kể cả phát lại): luôn là số hiện tại, không đóng băng.
        var today = await repository.GetXpTodayAsync(userId, day, cancellationToken);
        return new PracticeAnswerResultDto(
            outcome.IsCorrect, outcome.CorrectIndex, outcome.XpAwarded, outcome.XpReason?.ToCode(), today, Remaining(today), outcome.Completed);
    }

    // Chỉ kiểm dạng (độ dài 6, chữ và số); bảng chữ cái thật của mã phòng nằm ở RoomCodeGenerator. Đúng dạng mà không có phòng thì vẫn 404.
    private static bool IsRoomCodeShape(string code) => code.Length == 6 && code.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9');

    private int Remaining(int xpToday) => Math.Max(options.DailyXpCap - xpToday, 0);
}
