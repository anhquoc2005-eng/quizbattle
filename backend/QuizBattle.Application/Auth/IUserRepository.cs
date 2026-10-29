using QuizBattle.Domain.Entities;

namespace QuizBattle.Application.Auth;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lưu user mới. Ném ConflictException nếu email đã tồn tại (bắt unique violation 23505 ở đây).</summary>
    Task AddAsync(User user, CancellationToken cancellationToken);
}
