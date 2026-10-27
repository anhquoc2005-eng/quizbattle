using Microsoft.EntityFrameworkCore;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mỗi entity có một lớp cấu hình riêng (IEntityTypeConfiguration) trong project này.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
