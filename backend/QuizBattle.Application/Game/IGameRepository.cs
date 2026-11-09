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

    /// <summary>Như WithLockedRoomAsync nhưng tìm phòng theo Id - cho thao tác hệ thống (timer) không có mã phòng từ client.</summary>
    Task<TResult> WithLockedRoomByIdAsync<TResult>(Guid roomId, Func<GameSnapshot, TResult> work, CancellationToken cancellationToken);

    /// <summary>
    /// Một truy vấn AsNoTracking cho mọi phòng InProgress: câu hiện tại quá hạn / đủ người trả lời mà chưa
    /// kết thúc, hoặc đã kết thúc đủ RESULT_DISPLAY_SECONDS. Chỉ là bước lọc thô - điều kiện thật được
    /// kiểm tra lại trong khóa FOR UPDATE khi xử lý từng phòng.
    /// </summary>
    Task<IReadOnlyList<DueRoom>> FindDueRoomsAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Vào phòng: khóa dòng Room bằng FOR UPDATE, nạp snapshot (kèm tài khoản userId nếu có, trong snapshot.Users), gọi
    /// buildOrReusePlayer để quyết định (Lobby/giới hạn 50 người/tên trùng/tài khoản còn tồn tại - ném exception thì
    /// rollback, không lưu gì). Callback trả Player mới (insert) hoặc Player đã có trong snapshot.Players (vào lại: chỉ
    /// lưu phần đã sửa, không insert). Commit.
    /// </summary>
    Task<Player> JoinPlayerAsync(string code, Guid? userId, Func<GameSnapshot, Player> buildOrReusePlayer, CancellationToken cancellationToken);

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
