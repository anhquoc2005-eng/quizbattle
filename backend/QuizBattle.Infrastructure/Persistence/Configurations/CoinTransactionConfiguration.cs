using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class CoinTransactionConfiguration : IEntityTypeConfiguration<CoinTransaction>
{
    public void Configure(EntityTypeBuilder<CoinTransaction> builder)
    {
        builder.ToTable("CoinTransactions", t =>
        {
            t.HasCheckConstraint("CK_CoinTransactions_Amount_NonZero", "\"Amount\" <> 0");
            // Thưởng trận: số dương, không gắn món đồ. Mua đồ: số âm, bắt buộc gắn bản ghi sở hữu.
            t.HasCheckConstraint(
                "CK_CoinTransactions_ReasonShape",
                "(\"Reason\" = 'MatchReward' AND \"Amount\" > 0 AND \"UserAvatarItemId\" IS NULL) OR (\"Reason\" = 'ShopPurchase' AND \"Amount\" < 0 AND \"UserAvatarItemId\" IS NOT NULL)");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Reason).HasConversion<string>().IsRequired(); // text, theo data-model.md
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.UserId, c.CreatedAt })
            .HasDatabaseName("IX_CoinTransactions_UserId_CreatedAt")
            .IsDescending(false, true);

        // Một khoản thưởng sinh tối đa một dòng sổ cái; dòng không gắn thưởng (mua đồ) không bị ràng buộc.
        builder.HasIndex(c => c.MatchRewardId)
            .IsUnique()
            .HasDatabaseName("IX_CoinTransactions_MatchRewardId")
            .HasFilter("\"MatchRewardId\" IS NOT NULL");

        // Mỗi lần mua sinh đúng một dòng sổ cái.
        builder.HasIndex(c => c.UserAvatarItemId)
            .IsUnique()
            .HasDatabaseName("IX_CoinTransactions_UserAvatarItemId")
            .HasFilter("\"UserAvatarItemId\" IS NOT NULL");

        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Room>().WithMany().HasForeignKey(c => c.RoomId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<MatchReward>().WithMany().HasForeignKey(c => c.MatchRewardId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<UserAvatarItem>().WithMany().HasForeignKey(c => c.UserAvatarItemId).OnDelete(DeleteBehavior.NoAction);
    }
}
