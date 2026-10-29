using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Auth;

public interface ITokenService
{
    AuthToken CreateToken(User user);
}
