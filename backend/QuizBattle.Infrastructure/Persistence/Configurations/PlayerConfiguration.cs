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

        // Giữ index FK theo phòng: index lọc bên dưới không phục vụ được truy vấn theo RoomId cho khách (UserId null).
        builder.HasIndex(p => p.RoomId);

        // Mỗi tài khoản tối đa một Player mỗi phòng; khách (UserId null) không bị ràng buộc.
        builder.HasIndex(p => new { p.RoomId, p.UserId })
            .HasDatabaseName("IX_Players_RoomId_UserId")
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");

        builder.HasMany(p => p.Answers)
            .WithOne(a => a.Player)
            .HasForeignKey(a => a.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Người chơi có tài khoản - xóa User thì chỉ gỡ liên kết, không xóa Player/điểm.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
