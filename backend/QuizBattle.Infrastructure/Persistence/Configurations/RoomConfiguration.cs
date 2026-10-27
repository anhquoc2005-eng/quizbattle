using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms", t => t.HasCheckConstraint(
            "CK_Rooms_QuestionTimeLimitSeconds_Range",
            "\"QuestionTimeLimitSeconds\" BETWEEN 5 AND 120"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).HasMaxLength(6).IsRequired();
        builder.Property(r => r.Topic).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnType("timestamptz");
        builder.Property(r => r.StartedAt).HasColumnType("timestamptz");
        builder.Property(r => r.EndedAt).HasColumnType("timestamptz");

        builder.HasIndex(r => r.Code).IsUnique();

        builder.HasMany(r => r.Questions)
            .WithOne(q => q.Room)
            .HasForeignKey(q => q.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Players)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
