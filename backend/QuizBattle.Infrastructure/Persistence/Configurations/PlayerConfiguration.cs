using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.DisplayName).HasMaxLength(50).IsRequired();
        builder.Property(p => p.SessionTokenHash).IsRequired();
        builder.Property(p => p.Score).HasDefaultValue(0);
        builder.Property(p => p.JoinedAt).HasColumnType("timestamptz");

        // Unique (RoomId, lower(DisplayName)) là index theo biểu thức, EF không khai báo được bằng fluent API:
        // tạo bằng SQL thô trong migration UseCaseInsensitivePlayerName.
        builder.HasIndex(p => p.SessionTokenHash).IsUnique();

        builder.HasMany(p => p.Answers)
            .WithOne(a => a.Player)
            .HasForeignKey(a => a.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chuẩn bị người chơi có tài khoản sau này - xóa User thì chỉ gỡ liên kết, không xóa Player/điểm.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
