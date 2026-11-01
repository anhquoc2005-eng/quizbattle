using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Game;

public interface IGameRepository
{
    /// <summary>Đọc trạng thái phòng, không khóa - dùng cho JoinRoom (chỉ hiển thị, không sửa gì).</summary>
    Task<GameSnapshot?> FindSnapshotByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Player?> FindPlayerByTokenHashAsync(string sessionTokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Khóa dòng Room (SELECT ... FOR UPDATE) trong 1 transaction, nạp Room/câu Approved/Players/Answers
    /// đang được EF theo dõi, gọi work để đọc và SỬA TRỰC TIẾP lên các entity đó (ném exception thì
    /// rollback, không lưu gì), rồi SaveChanges + commit. Dùng cho open/start/end/next - thao tác hiếm của
    /// chủ phòng, cần chặn 2 lệnh bấm cùng lúc làm nhảy 2 câu.
    /// </summary>
    Task<TResult> WithLockedRoomAsync<TResult>(string code, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken);

    /// <summary>
    /// Thêm người chơi mới: khóa dòng Room bằng FOR UPDATE, nạp snapshot, gọi buildPlayer để quyết định
    /// (kiểm tra Lobby/giới hạn 50 người/tên trùng - ném exception thì rollback), insert, commit.
    /// </summary>
    Task<Player> AddPlayerAsync(string code, Func<GameSnapshot, Player> buildPlayer, CancellationToken cancellationToken);

    /// <summary>
    /// Nộp đáp án: khóa dòng Room bằng SELECT ... FOR SHARE (không phải FOR UPDATE) - nhiều người trả lời
    /// cùng lúc giữ khóa chia sẻ này đồng thời, không xếp hàng chờ nhau. Nhưng EndQuestion/NextQuestion
    /// khóa Room bằng FOR UPDATE (độc quyền) nên phải đợi mọi SubmitAnswer đang lưu dở (đang giữ FOR SHARE)
    /// commit xong mới chạy tiếp - nhờ vậy QuestionEnded/LeaderboardUpdated không bao giờ gửi đi trước khi
    /// một câu trả lời đang bay tới kịp lưu. work ném exception thì rollback, không insert gì. Thành công
    /// thì insert Answer + cộng Player.Score trong CÙNG 1 lần SaveChanges (1 transaction). Trả lời trùng
    /// (vi phạm unique index PlayerId+QuestionId) -> ném ConflictException.
    /// </summary>
    Task<TResult> SubmitAnswerAsync<TResult>(
        Guid playerId,
        Func<SubmitAnswerSnapshot, (Answer Answer, TResult Result)> work,
        CancellationToken cancellationToken);
}
