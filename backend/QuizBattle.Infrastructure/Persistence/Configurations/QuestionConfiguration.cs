using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions", t =>
        {
            t.HasCheckConstraint("CK_Questions_CorrectIndex_Range", "\"CorrectIndex\" BETWEEN 0 AND 3");
            t.HasCheckConstraint("CK_Questions_Options_Count", "cardinality(\"Options\") = 4");
        });

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text).HasMaxLength(500).IsRequired();
        builder.Property(q => q.Options).HasColumnType("text[]").IsRequired();
        builder.Property(q => q.ReviewStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(q => q.StartedAt).HasColumnType("timestamptz");

        builder.HasIndex(q => new { q.RoomId, q.OrderIndex }).IsUnique();

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
