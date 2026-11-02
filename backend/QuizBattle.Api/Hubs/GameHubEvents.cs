namespace QuizBattle.Api.Hubs;

/// <summary>
/// Hằng số tên sự kiện Server → Client của GameHub (CLAUDE.md: "hằng số tương ứng ở backend").
/// Tên method Client → Server (JoinRoom, StartGame, EndQuestion, NextQuestion, SubmitAnswer, LeaveRoom)
/// không cần hằng số riêng - SignalR gọi thẳng theo tên method C# của GameHub.
/// </summary>
public static class GameHubEvents
{
    public const string ROOM_STATE = "RoomState";
    public const string PLAYER_JOINED = "PlayerJoined";
    public const string PLAYER_LEFT = "PlayerLeft";
    public const string QUESTION_STARTED = "QuestionStarted";
    public const string QUESTION_ENDED = "QuestionEnded";
    public const string LEADERBOARD_UPDATED = "LeaderboardUpdated";
    public const string GAME_ENDED = "GameEnded";
    public const string ANSWER_PROGRESS = "AnswerProgress";
    public const string ERROR = "Error";
}
