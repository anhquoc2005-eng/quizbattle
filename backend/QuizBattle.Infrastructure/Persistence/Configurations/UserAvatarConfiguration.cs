using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizBattle.Domain.Entities;

namespace QuizBattle.Infrastructure.Persistence.Configurations;

public sealed class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
{
    // Cột lưu khóa món đồ, tên ô (khớp tiền tố khóa "ô.variantKey") và ô có bắt buộc không.
    private static readonly (string Column, string Slot, bool Required)[] Slots =
    [
        ("EyesItemKey", "eyes", true),
        ("EyebrowsItemKey", "eyebrows", true),
        ("MouthItemKey", "mouth", true),
        ("NoseItemKey", "nose", true),
        ("HairItemKey", "hair", true),
        ("ClothesItemKey", "clothes", true),
        ("GlassesItemKey", "glasses", false),
        ("BeardItemKey", "beard", false),
        ("GestureItemKey", "gesture", false),
        ("ClothesGraphicItemKey", "clothesGraphic", false),
        ("BackgroundItemKey", "background", false),
    ];

    public void Configure(EntityTypeBuilder<UserAvatar> builder)
    {
        builder.ToTable("UserAvatars", t =>
        {
            // Mỗi cột chỉ nhận món của đúng ô đó (NULL được ở ô tùy chọn); FK bảo đảm món có trong danh mục.
            foreach (var (column, slot, required) in Slots)
            {
                var like = $"\"{column}\" LIKE '{slot}.%'";
                t.HasCheckConstraint($"CK_UserAvatars_{slot}", required ? like : $"\"{column}\" IS NULL OR {like}");
            }
        });

        builder.HasKey(a => a.UserId);
        builder.Property(a => a.UpdatedAt).HasColumnType("timestamptz");

        builder.HasOne<User>().WithOne().HasForeignKey<UserAvatar>(a => a.UserId).OnDelete(DeleteBehavior.Cascade);

        foreach (var (column, _, required) in Slots)
        {
            builder.Property(column).HasMaxLength(80).IsRequired(required);
            builder.HasOne<AvatarItem>().WithMany().HasForeignKey(column).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
