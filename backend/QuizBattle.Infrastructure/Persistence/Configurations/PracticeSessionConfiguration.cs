using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class PracticeSessionConfiguration : IEntityTypeConfiguration<PracticeSession>
{
    public void Configure(EntityTypeBuilder<PracticeSession> builder)
    {
        builder.ToTable("PracticeSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RoomCode).HasMaxLength(6).IsRequired();
        builder.Property(s => s.RoomTopic).HasMaxLength(200).IsRequired();
        builder.Property(s => s.QuestionIds).HasColumnType("uuid[]").IsRequired();
        builder.Property(s => s.StartedAt).HasColumnType("timestamptz");
        builder.Property(s => s.CompletedAt).HasColumnType("timestamptz");

        // Mỗi (user, phòng) tối đa một lượt đang mở. RoomId NULL (phòng đã xóa) không bao giờ xung đột trong unique index của Postgres.
        builder.HasIndex(s => new { s.UserId, s.RoomId })
            .IsUnique()
            .HasDatabaseName("IX_PracticeSessions_OpenPerUserRoom")
            .HasFilter("\"CompletedAt\" IS NULL");

        // "Lượt gần đây" của một người dùng, mới nhất trước.
        builder.HasIndex(s => new { s.UserId, s.StartedAt })
            .HasDatabaseName("IX_PracticeSessions_UserId_StartedAt")
            .IsDescending(false, true);

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Room>().WithMany().HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.SetNull);
    }
}
