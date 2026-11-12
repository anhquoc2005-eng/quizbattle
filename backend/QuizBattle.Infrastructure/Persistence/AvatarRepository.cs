using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Shop;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class AvatarRepository(AppDbContext db) : IAvatarRepository
{
    public async Task<IReadOnlyList<AvatarItem>> GetCatalogAsync(CancellationToken cancellationToken) =>
        await db.AvatarItems.AsNoTracking().ToListAsync(cancellationToken);
}
