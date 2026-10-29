using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Application.Auth;
using QuizBattle.Application.Common;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Hai request đăng ký cùng email cùng lúc: cả hai qua được bước kiểm tra trước, DB chặn ở đây.
            throw new ConflictException("Email đã được sử dụng.");
        }
    }
}
