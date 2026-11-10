using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
        {
            t.HasCheckConstraint("CK_Users_Email_Lowercase", "\"Email\" = lower(\"Email\")");
            t.HasCheckConstraint("CK_Users_TotalXp_NonNegative", "\"TotalXp\" >= 0");
            t.HasCheckConstraint("CK_Users_CoinBalance_NonNegative", "\"CoinBalance\" >= 0");
        });

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnType("timestamptz");
        builder.Property(u => u.TotalXp).HasDefaultValue(0);
        builder.Property(u => u.CoinBalance).HasDefaultValue(0);

        builder.HasIndex(u => u.Email).IsUnique();

        // Bảng xếp hạng XP: top theo TotalXp giảm dần; tài khoản 0 XP không nằm trong index.
        builder.HasIndex(u => new { u.TotalXp, u.CreatedAt, u.Id })
            .HasDatabaseName("IX_Users_TotalXp")
            .IsDescending(true, false, false)
            .HasFilter("\"TotalXp\" > 0");

        builder.HasMany(u => u.HostedRooms)
            .WithOne(r => r.HostUser)
            .HasForeignKey(r => r.HostUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
