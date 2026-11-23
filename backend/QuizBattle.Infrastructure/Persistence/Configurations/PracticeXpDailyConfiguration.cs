using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class PracticeXpDailyConfiguration : IEntityTypeConfiguration<PracticeXpDaily>
{
    public void Configure(EntityTypeBuilder<PracticeXpDaily> builder)
    {
        // Trần (Practice:DailyXpCap) kiểm bằng SQL lúc giữ chỗ, không bằng CHECK cứng, để đổi cấu hình không cần migration.
        builder.ToTable("PracticeXpDaily", t => t.HasCheckConstraint("CK_PracticeXpDaily_Xp_NonNegative", "\"Xp\" >= 0"));

        builder.HasKey(d => new { d.UserId, d.Day });
        builder.Property(d => d.Day).HasColumnType("date");

        builder.HasOne<User>().WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
