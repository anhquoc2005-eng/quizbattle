using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage>
{
    public void Configure(EntityTypeBuilder<AiUsage> builder)
    {
        builder.ToTable("AiUsageDaily", t => t.HasCheckConstraint("CK_AiUsageDaily_Count_NonNegative", "\"Count\" >= 0"));

        // Scope là chuỗi NOT NULL chứ không phải UserId nullable: NULL trong unique index của Postgres không bao giờ
        // xung đột, nên ON CONFLICT sẽ không bắt được dòng "global".
        builder.HasKey(a => new { a.Day, a.Scope });
        builder.Property(a => a.Day).HasColumnType("date");
        builder.Property(a => a.Scope).HasMaxLength(50).IsRequired();
    }
}
