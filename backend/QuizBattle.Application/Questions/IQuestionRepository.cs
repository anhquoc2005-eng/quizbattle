using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Questions;

public interface IQuestionRepository
{
    Task<IReadOnlyList<Question>> ListByRoomIdAsync(Guid roomId, CancellationToken cancellationToken);

    Task<Question?> FindByIdAsync(Guid roomId, Guid questionId, CancellationToken cancellationToken);

    Task<int> CountByRoomIdAsync(Guid roomId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListTextsByRoomIdAsync(Guid roomId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task DeleteAsync(Question question, CancellationToken cancellationToken);

    /// <summary>
    /// Khóa dòng Room (SELECT ... FOR UPDATE) trong 1 transaction, đọc lại Status/số câu hiện có/mã câu đã
    /// có NGAY LÚC ĐÓ (chống TOCTOU khi 2 request cùng thao tác 1 phòng), gọi buildQuestions để quyết định
    /// danh sách Question cần lưu, gán OrderIndex tăng dần từ NextOrderIndex, insert rồi commit.
    /// buildQuestions ném exception thì rollback, không insert gì.
    /// </summary>
    Task<IReadOnlyList<Question>> InsertWithRoomLockAsync(
        Guid roomId,
        Func<RoomSnapshot, IReadOnlyList<Question>> buildQuestions,
        CancellationToken cancellationToken);
}
