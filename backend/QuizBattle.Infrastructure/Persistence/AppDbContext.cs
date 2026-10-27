using Microsoft.EntityFrameworkCore;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mỗi entity có một lớp cấu hình riêng (IEntityTypeConfiguration) trong project này.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
