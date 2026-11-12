using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Entities;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class AvatarItemConfiguration : IEntityTypeConfiguration<AvatarItem>
{
    public void Configure(EntityTypeBuilder<AvatarItem> builder)
    {
        builder.ToTable("AvatarItems", t =>
        {
            t.HasCheckConstraint("CK_AvatarItems_Key", "\"Key\" = \"Slot\" || '.' || \"VariantKey\"");
            t.HasCheckConstraint(
                "CK_AvatarItems_Price",
                "(\"Tier\" = 'Free' AND \"Price\" = 0) OR (\"Tier\" <> 'Free' AND \"Price\" > 0)");
        });

        builder.HasKey(i => i.Key);

        builder.Property(i => i.Key).HasMaxLength(80);
        // Cột Slot lưu đúng tên chuỗi camelCase ("hair", "clothesGraphic") để CK_AvatarItems_Key (Key = Slot || '.' || VariantKey) đúng.
        builder.Property(i => i.Slot)
            .HasConversion(new ValueConverter<AvatarSlot, string>(slot => AvatarSlots.ToKey(slot), text => AvatarSlots.FromKey(text)))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(i => i.VariantKey).HasMaxLength(40).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(60).IsRequired();
        builder.Property(i => i.Tier).HasConversion<string>().HasMaxLength(10).IsRequired();

        builder.HasIndex(i => new { i.Slot, i.VariantKey }).IsUnique().HasDatabaseName("IX_AvatarItems_Slot_VariantKey");

        // Seed danh mục từ nguồn duy nhất trong Domain; sửa danh mục sau khi lên production phải qua migration mới.
        builder.HasData(AvatarCatalog.Items.Select(d => new AvatarItem
        {
            Key = d.Key,
            Slot = d.Slot,
            VariantKey = d.VariantKey,
            Name = d.Name,
            Tier = d.Tier,
            Price = d.Price,
            SortOrder = d.SortOrder,
        }));
    }
}
