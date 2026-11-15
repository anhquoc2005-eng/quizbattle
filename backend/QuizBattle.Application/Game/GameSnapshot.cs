using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Game;

/// <summary>
/// Trạng thái phòng đọc được khi khóa dòng Room (SELECT ... FOR UPDATE) hoặc khi đọc không khóa cho
/// JoinRoom. Room/ApprovedQuestions/Players là entity đang được EF theo dõi (trong IGameRepository.WithLockedRoomAsync) -
/// Service sửa trực tiếp lên chúng, repository tự lưu sau khi gọi callback.
/// </summary>
public sealed class GameSnapshot
{
    public required Room Room { get; init; }

    /// <summary>Câu đã duyệt, theo đúng OrderIndex.</summary>
    public required IReadOnlyList<Question> ApprovedQuestions { get; init; }

    public required IReadOnlyList<Player> Players { get; init; }

    /// <summary>Toàn bộ đáp án đã nộp trong phòng (để tính bảng xếp hạng, hòa điểm theo tổng thời gian trả lời đúng).</summary>
    public required IReadOnlyList<Answer> Answers { get; init; }

    /// <summary>Tổng số câu hỏi trong phòng (cả Pending lẫn Approved) - dùng để map RoomResponse.QuestionCount.</summary>
    public int TotalQuestionCount { get; init; }

    public int AnsweredCountForCurrentQuestion { get; init; }

    /// <summary>Tài khoản của những người chơi có UserId, theo User.Id. Khách không có trong từ điển.</summary>
    public IReadOnlyDictionary<Guid, User> Users { get; init; } = new Dictionary<Guid, User>();

    /// <summary>
    /// Nhân vật đã lưu của những người chơi có tài khoản, theo User.Id (một truy vấn, không khóa). Tài khoản chưa từng lưu
    /// và khách không có trong từ điển: service tự sinh nhân vật mặc định cho họ.
    /// </summary>
    public IReadOnlyDictionary<Guid, UserAvatar> Avatars { get; init; } = new Dictionary<Guid, UserAvatar>();

    /// <summary>
    /// Phòng đang InProgress và câu đã duyệt cuối cùng đã StartedAt + EndedAt - lần AdvanceFrom kế tiếp sẽ kết thúc
    /// trận. Chỉ khi đó repository mới nạp <see cref="Users"/> có khóa FOR UPDATE và theo dõi để cộng thưởng.
    /// </summary>
    public bool IsFinalAdvance { get; init; }

    /// <summary>Thưởng đã lưu của trận (chỉ nạp khi phòng Finished); trận cũ trước tính năng thì rỗng.</summary>
    public IReadOnlyList<MatchReward> Rewards { get; init; } = [];

    /// <summary>Service thêm vào khi kết thúc trận; repository lưu cùng SaveChanges với Room.Status = Finished.</summary>
    public List<MatchReward> NewMatchRewards { get; } = [];

    public List<CoinTransaction> NewCoinTransactions { get; } = [];

    /// <summary>
    /// Câu mới bắt đầu gần nhất - có thể đang chạy (EndedAt null) hoặc vừa kết thúc nhưng chủ phòng chưa
    /// bấm câu tiếp (EndedAt có giá trị). Dùng broadened definition này để người vào lại phòng ngay sau khi
    /// EndQuestion vẫn thấy đúng câu đó (kèm đáp án đúng), không bị null.
    /// </summary>
    public Question? CurrentQuestion => ApprovedQuestions
        .Where(q => q.StartedAt is not null)
        .OrderByDescending(q => q.StartedAt)
        .FirstOrDefault();
}
