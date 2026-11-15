using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class UserAvatarItemConfiguration : IEntityTypeConfiguration<UserAvatarItem>
{
    public void Configure(EntityTypeBuilder<UserAvatarItem> builder)
    {
        builder.ToTable("UserAvatarItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ItemKey).HasMaxLength(80).IsRequired();
        builder.Property(i => i.AcquiredAt).HasColumnType("timestamptz");

        // Chặn sở hữu hai lần ở tầng DB (lưới an toàn sau khóa dòng Users khi mua).
        builder.HasIndex(i => new { i.UserId, i.ItemKey }).IsUnique().HasDatabaseName("IX_UserAvatarItems_UserId_ItemKey");

        builder.HasOne<User>().WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AvatarItem>().WithMany().HasForeignKey(i => i.ItemKey).OnDelete(DeleteBehavior.Restrict);
    }
}
