using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class PracticeAnswerConfiguration : IEntityTypeConfiguration<PracticeAnswer>
{
    public void Configure(EntityTypeBuilder<PracticeAnswer> builder)
    {
        builder.ToTable("PracticeAnswers", t =>
        {
            t.HasCheckConstraint("CK_PracticeAnswers_SelectedIndex_Range", $"\"SelectedIndex\" BETWEEN 0 AND {Question.OPTION_COUNT - 1}");
            t.HasCheckConstraint("CK_PracticeAnswers_XpAwarded_NonNegative", "\"XpAwarded\" >= 0");

            // XpReason chỉ nhận 4 giá trị hoặc NULL; NULL (nhận đủ XP) chỉ hợp lệ với câu đúng. DB không biết XpPerCorrect
            // (cấu hình) nên việc "đủ" = đúng bằng XpPerCorrect do PracticeRewardCalculator và repository bảo đảm.
            // Nhánh "XpReason IS NOT NULL" là bắt buộc: CHECK của Postgres cho qua khi biểu thức ra NULL, mà mọi so sánh
            // "XpReason = ..." đều ra NULL khi XpReason NULL, nên thiếu nó thì (NULL, câu sai) lọt qua.
            t.HasCheckConstraint(
                "CK_PracticeAnswers_XpReason",
                "(\"XpReason\" IS NULL AND \"IsCorrect\") OR (\"XpReason\" IS NOT NULL AND ("
                + "(\"XpReason\" = 'wrong' AND NOT \"IsCorrect\" AND \"XpAwarded\" = 0) "
                + "OR (\"XpReason\" IN ('already_today', 'daily_cap') AND \"IsCorrect\" AND \"XpAwarded\" = 0) "
                + "OR (\"XpReason\" = 'daily_cap_partial' AND \"IsCorrect\" AND \"XpAwarded\" > 0)))");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.XpReason)
            .HasMaxLength(20)
            .HasConversion(new ValueConverter<PracticeXpReason?, string?>(
                v => v == null ? null : v.Value.ToCode(),
                s => s == null ? null : PracticeXpReasonCodes.FromCode(s)));
        builder.Property(a => a.Day).HasColumnType("date");
        builder.Property(a => a.AnsweredAt).HasColumnType("timestamptz");

        // Mỗi câu chấm tối đa một lần trong một lượt. SessionId NULL (phiên đã gỡ) không xung đột.
        builder.HasIndex(a => new { a.SessionId, a.QuestionId })
            .IsUnique()
            .HasDatabaseName("IX_PracticeAnswers_SessionId_QuestionId");

        // XP chỉ một lần cho mỗi (user, câu, ngày VN). Dòng 0 XP không bị ràng buộc.
        builder.HasIndex(a => new { a.UserId, a.QuestionId, a.Day })
            .IsUnique()
            .HasDatabaseName("IX_PracticeAnswers_UserId_QuestionId_Day_Awarded")
            .HasFilter("\"XpAwarded\" > 0");

        // Cascade CHỈ theo User. Xóa phòng/câu/lượt chỉ gỡ liên kết để XP đã cộng vẫn còn trong tổng.
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PracticeSession>().WithMany().HasForeignKey(a => a.SessionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Question>().WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.SetNull);
    }
}
