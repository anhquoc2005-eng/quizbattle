using QuizBattle.Domain.Enums;

namespace QuizBattle.Domain.Avatars;

/// <summary>Mã lỗi máy đọc được (khớp trường "code" của ProblemDetails).</summary>
public static class AvatarErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string UnknownVariant = "unknown_variant";
    public const string GraphicIncompatibleClothes = "graphic_incompatible_clothes";
}

public sealed record AvatarRuleError(AvatarSlot Slot, string Code);

/// <summary>
/// Luật hợp lệ của bộ chọn nhân vật - hàm thuần, không đụng DB. Không kiểm sở hữu (cần dữ liệu người dùng,
/// do service làm). Lỗi trả theo thứ tự ô cố định; kiểm tương thích hình in/áo chỉ chạy khi mọi ô đều hợp lệ.
/// </summary>
public static class AvatarRules
{
    public static IReadOnlyList<AvatarRuleError> Validate(AvatarSelection selection)
    {
        var errors = new List<AvatarRuleError>();

        foreach (var slot in AvatarSlots.All)
        {
            var variantKey = selection.Get(slot);
            if (variantKey is null)
            {
                if (AvatarSlots.IsRequired(slot))
                {
                    errors.Add(new AvatarRuleError(slot, AvatarErrorCodes.ValidationFailed));
                }

                continue;
            }

            if (AvatarCatalog.Find(slot, variantKey) is null)
            {
                errors.Add(new AvatarRuleError(slot, AvatarErrorCodes.UnknownVariant));
            }
        }

        if (errors.Count == 0
            && selection.Get(AvatarSlot.ClothesGraphic) is not null
            && !AvatarCatalog.PrintableClothesVariantKeys.Contains(selection.Get(AvatarSlot.Clothes)!))
        {
            errors.Add(new AvatarRuleError(AvatarSlot.ClothesGraphic, AvatarErrorCodes.GraphicIncompatibleClothes));
        }

        return errors;
    }
}
