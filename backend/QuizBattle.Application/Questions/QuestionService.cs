using QuizBattle.Application.Common;
using QuizBattle.Application.Rooms;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Application.Questions;

public sealed class QuestionService(
    IQuestionRepository questionRepository,
    IRoomRepository roomRepository,
    IQuestionGenerator questionGenerator) : IQuestionService
{
    private const int MaxGenerateAttempts = 2; // gọi lần đầu + thử lại tối đa 1 lần

    public async Task<IReadOnlyList<QuestionResponse>> GetQuestionsAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
    {
        var room = await GetOwnedRoomAsync(hostUserId, code, cancellationToken);
        var questions = await questionRepository.ListByRoomIdAsync(room.Id, cancellationToken);
        return questions.OrderBy(q => q.OrderIndex).Select(ToResponse).ToList();
    }

    public async Task<GenerateQuestionsResponse> GenerateAsync(Guid hostUserId, string code, GenerateQuestionsRequest request, CancellationToken cancellationToken)
    {
        var room = await GetOwnedDraftRoomAsync(hostUserId, code, cancellationToken);

        // Kiểm tra sớm để không gọi AI lãng phí khi đã rõ vượt giới hạn - vẫn kiểm tra lại chắc chắn
        // trong lock ở InsertWithRoomLockAsync, vì giữa lúc này và lúc đó có thể có request khác chen vào.
        var currentCount = await questionRepository.CountByRoomIdAsync(room.Id, cancellationToken);
        if (currentCount + request.Count > Room.MAX_QUESTIONS_PER_ROOM)
        {
            throw new ConflictException($"Phòng chỉ còn chỗ cho tối đa {Room.MAX_QUESTIONS_PER_ROOM} câu hỏi.");
        }

        var existingTexts = await questionRepository.ListTextsByRoomIdAsync(room.Id, cancellationToken);

        // Gọi Gemini và validate TRƯỚC, không giữ transaction/khóa trong lúc chờ AI (có thể chậm, tối đa 30s).
        var candidates = await GenerateValidCandidatesAsync(room.Topic, request.Count, existingTexts, cancellationToken);

        var toInsert = candidates.Select(c =>
        {
            var (options, correctIndex) = OptionShuffler.Shuffle(c.Options, c.CorrectIndex);
            return new Question
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                Text = c.Text.Trim(),
                Options = options,
                CorrectIndex = correctIndex,
                ReviewStatus = QuestionReviewStatus.Pending,
            };
        }).ToList();

        var inserted = await questionRepository.InsertWithRoomLockAsync(room.Id, snapshot =>
        {
            if (snapshot.Status != RoomStatus.Draft)
            {
                throw new ConflictException("Chỉ thao tác được với câu hỏi khi phòng đang ở trạng thái Draft.");
            }

            // Lọc lại trùng theo dữ liệu mới nhất ngay trong lock - phòng trường hợp có request khác vừa
            // thêm câu vào phòng này trong lúc ta đang chờ AI trả lời (TOCTOU).
            var freshExisting = new HashSet<string>(snapshot.ExistingTexts.Select(t => t.Trim().ToUpperInvariant()));
            var fresh = toInsert.Where(q => freshExisting.Add(q.Text.ToUpperInvariant())).ToList();

            if (snapshot.QuestionCount + fresh.Count > Room.MAX_QUESTIONS_PER_ROOM)
            {
                throw new ConflictException($"Phòng chỉ còn chỗ cho tối đa {Room.MAX_QUESTIONS_PER_ROOM} câu hỏi.");
            }

            return fresh;
        }, cancellationToken);

        return new GenerateQuestionsResponse(request.Count, inserted.Count, inserted.OrderBy(q => q.OrderIndex).Select(ToResponse).ToList());
    }

    public async Task<QuestionResponse> AddAsync(Guid hostUserId, string code, QuestionInput input, CancellationToken cancellationToken)
    {
        var room = await GetOwnedDraftRoomAsync(hostUserId, code, cancellationToken);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Text = input.Text.Trim(),
            Options = input.Options.Select(o => o.Trim()).ToArray(),
            CorrectIndex = input.CorrectIndex,
            ReviewStatus = QuestionReviewStatus.Approved,
        };

        var inserted = await questionRepository.InsertWithRoomLockAsync(room.Id, snapshot =>
        {
            if (snapshot.Status != RoomStatus.Draft)
            {
                throw new ConflictException("Chỉ thao tác được với câu hỏi khi phòng đang ở trạng thái Draft.");
            }

            if (snapshot.QuestionCount + 1 > Room.MAX_QUESTIONS_PER_ROOM)
            {
                throw new ConflictException($"Phòng đã đủ {Room.MAX_QUESTIONS_PER_ROOM} câu hỏi.");
            }

            return [question];
        }, cancellationToken);

        return ToResponse(inserted[0]);
    }

    public async Task<QuestionResponse> UpdateAsync(Guid hostUserId, string code, Guid questionId, QuestionInput input, CancellationToken cancellationToken)
    {
        var room = await GetOwnedDraftRoomAsync(hostUserId, code, cancellationToken);
        var question = await questionRepository.FindByIdAsync(room.Id, questionId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi.");

        question.Text = input.Text.Trim();
        question.Options = input.Options.Select(o => o.Trim()).ToArray();
        question.CorrectIndex = input.CorrectIndex;
        question.ReviewStatus = QuestionReviewStatus.Approved;

        await questionRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(question);
    }

    public async Task<QuestionResponse> ApproveAsync(Guid hostUserId, string code, Guid questionId, CancellationToken cancellationToken)
    {
        var room = await GetOwnedDraftRoomAsync(hostUserId, code, cancellationToken);
        var question = await questionRepository.FindByIdAsync(room.Id, questionId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi.");

        question.ReviewStatus = QuestionReviewStatus.Approved;
        await questionRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(question);
    }

    public async Task DeleteAsync(Guid hostUserId, string code, Guid questionId, CancellationToken cancellationToken)
    {
        var room = await GetOwnedDraftRoomAsync(hostUserId, code, cancellationToken);
        var question = await questionRepository.FindByIdAsync(room.Id, questionId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy câu hỏi.");

        await questionRepository.DeleteAsync(question, cancellationToken);
    }

    /// <summary>Gọi AI và validate kết quả, thử lại tối đa 1 lần nếu JSON sai định dạng hoặc không còn candidate hợp lệ.</summary>
    private async Task<IReadOnlyList<QuestionCandidate>> GenerateValidCandidatesAsync(
        string topic, int count, IReadOnlyCollection<string> existingTexts, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxGenerateAttempts; attempt++)
        {
            try
            {
                var raw = await questionGenerator.GenerateAsync(topic, count, cancellationToken);
                var valid = QuestionCandidateValidator.FilterValid(raw, existingTexts);
                if (valid.Count > 0)
                {
                    return valid;
                }
            }
            catch (AiResponseFormatException)
            {
                // JSON sai định dạng: rơi xuống dưới, thử lại nếu còn lượt.
            }
        }

        throw new BadGatewayException("AI không tạo được câu hỏi hợp lệ, thử lại sau.");
    }

    private async Task<Room> GetOwnedRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken) =>
        await roomRepository.FindEntityByCodeForUserAsync(hostUserId, code.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phòng.");

    private async Task<Room> GetOwnedDraftRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken)
    {
        var room = await GetOwnedRoomAsync(hostUserId, code, cancellationToken);
        if (room.Status != RoomStatus.Draft)
        {
            throw new ConflictException("Chỉ thao tác được với câu hỏi khi phòng đang ở trạng thái Draft.");
        }

        return room;
    }

    private static QuestionResponse ToResponse(Question question) => new(
        question.Id,
        question.OrderIndex,
        question.Text,
        question.Options,
        question.CorrectIndex,
        question.ReviewStatus.ToString());
}
