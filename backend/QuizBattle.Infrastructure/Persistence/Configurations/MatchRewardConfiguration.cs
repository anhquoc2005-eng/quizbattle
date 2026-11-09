using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class MatchRewardConfiguration : IEntityTypeConfiguration<MatchReward>
{
    public void Configure(EntityTypeBuilder<MatchReward> builder)
    {
        builder.ToTable("MatchRewards", t =>
        {
            t.HasCheckConstraint("CK_MatchRewards_Amounts_NonNegative", "\"Xp\" >= 0 AND \"Coins\" >= 0 AND \"RankBonus\" >= 0");
            t.HasCheckConstraint("CK_MatchRewards_DenialReason_MatchesStatus", "(\"Status\" = 'Awarded') = (\"DenialReason\" IS NULL)");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoomCode).HasMaxLength(6).IsRequired();
        builder.Property(r => r.RoomTopic).HasMaxLength(200).IsRequired();
        builder.Property(r => r.FinishedAt).HasColumnType("timestamptz");
        // Lưu chuỗi dạng text (data-model.md): thêm giá trị enum không cần đổi kiểu cột.
        builder.Property(r => r.Status).HasConversion<string>().IsRequired();
        builder.Property(r => r.DenialReason).HasConversion<string>();

        // Chặn cộng đôi: mỗi Player tối đa một khoản thưởng. Nhiều dòng PlayerId NULL (phòng đã xóa) vẫn hợp lệ.
        builder.HasIndex(r => r.PlayerId).IsUnique().HasDatabaseName("IX_MatchRewards_PlayerId");

        // Lịch sử hồ sơ: đọc thẳng từ bảng này theo người dùng, mới nhất trước.
        builder.HasIndex(r => new { r.UserId, r.FinishedAt })
            .HasDatabaseName("IX_MatchRewards_UserId_FinishedAt")
            .IsDescending(false, true);

        // Xóa phòng/Player cascade thì thưởng phải còn: chỉ gỡ liên kết. Chỉ xóa tài khoản mới xóa thưởng.
        builder.HasOne<Player>().WithMany().HasForeignKey(r => r.PlayerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Room>().WithMany().HasForeignKey(r => r.RoomId).OnDelete(DeleteBehavior.SetNull);
    }
}
