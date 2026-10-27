using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("Answers", t => t.HasCheckConstraint(
            "CK_Answers_SelectedIndex_Range",
            "\"SelectedIndex\" BETWEEN 0 AND 3"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AnsweredAt).HasColumnType("timestamptz");

        builder.HasIndex(a => new { a.PlayerId, a.QuestionId }).IsUnique();
    }
}
