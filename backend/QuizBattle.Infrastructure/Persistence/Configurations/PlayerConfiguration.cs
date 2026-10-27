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

        builder.HasIndex(p => new { p.RoomId, p.DisplayName }).IsUnique();
        builder.HasIndex(p => p.SessionTokenHash).IsUnique();

        builder.HasMany(p => p.Answers)
            .WithOne(a => a.Player)
            .HasForeignKey(a => a.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
