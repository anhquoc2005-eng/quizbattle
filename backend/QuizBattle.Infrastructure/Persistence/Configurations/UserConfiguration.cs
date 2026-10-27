using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Email_Lowercase", "\"Email\" = lower(\"Email\")"));

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.HostedRooms)
            .WithOne(r => r.HostUser)
            .HasForeignKey(r => r.HostUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
