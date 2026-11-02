namespace QuizBattle.Application.Game;

/// <summary>Sinh sessionToken gốc cho người chơi (32 byte ngẫu nhiên, base64url). Implement ở Infrastructure.</summary>
public interface ISessionTokenGenerator
{
    string Generate();
}
