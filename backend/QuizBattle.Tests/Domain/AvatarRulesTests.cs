using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Domain;

public sealed class AvatarRulesTests
{
    // Nhân vật hợp lệ chỉ gồm món miễn phí; áo variant01 in được hình.
    private static AvatarSelection Valid() => AvatarSelection.Empty
        .With(AvatarSlot.Eyes, "variant01")
        .With(AvatarSlot.Eyebrows, "variant01")
        .With(AvatarSlot.Mouth, "variant01")
        .With(AvatarSlot.Nose, "variant01")
        .With(AvatarSlot.Hair, "variant01")
        .With(AvatarSlot.Clothes, "variant01");

    public static TheoryData<AvatarSlot> RequiredSlots => new(AvatarSlots.Required);

    [Fact]
    public void Validate_AllRequiredFreeItems_NoErrors() => Assert.Empty(AvatarRules.Validate(Valid()));

    [Theory]
    [MemberData(nameof(RequiredSlots))]
    public void Validate_MissingRequiredSlot_ReturnsValidationFailedForThatSlot(AvatarSlot missing)
    {
        var errors = AvatarRules.Validate(Valid().With(missing, null));

        var error = Assert.Single(errors);
        Assert.Equal(missing, error.Slot);
        Assert.Equal(AvatarErrorCodes.ValidationFailed, error.Code);
    }

    [Fact]
    public void Validate_UnknownVariantInSlot_ReturnsUnknownVariant()
    {
        var error = Assert.Single(AvatarRules.Validate(Valid().With(AvatarSlot.Eyes, "variant99")));

        Assert.Equal(AvatarSlot.Eyes, error.Slot);
        Assert.Equal(AvatarErrorCodes.UnknownVariant, error.Code);
    }

    [Fact]
    public void Validate_OptionalSlotsNull_NoErrors()
    {
        var selection = Valid()
            .With(AvatarSlot.Glasses, null).With(AvatarSlot.Beard, null).With(AvatarSlot.Gesture, null)
            .With(AvatarSlot.ClothesGraphic, null).With(AvatarSlot.Background, null);

        Assert.Empty(AvatarRules.Validate(selection));
    }

    [Fact]
    public void Validate_VariantOfAnotherSlot_ReturnsUnknownVariant()
    {
        // "ok" là khóa của cử chỉ, nên không hợp lệ ở ô kính nhưng hợp lệ ở ô cử chỉ.
        var error = Assert.Single(AvatarRules.Validate(Valid().With(AvatarSlot.Glasses, "ok")));

        Assert.Equal(AvatarSlot.Glasses, error.Slot);
        Assert.Equal(AvatarErrorCodes.UnknownVariant, error.Code);
        Assert.Empty(AvatarRules.Validate(Valid().With(AvatarSlot.Gesture, "ok")));
    }

    [Fact]
    public void Validate_GraphicOnClothesVariant10_NoErrors() =>
        Assert.Empty(AvatarRules.Validate(Valid().With(AvatarSlot.Clothes, "variant10").With(AvatarSlot.ClothesGraphic, "galaxy")));

    [Fact]
    public void Validate_GraphicOnClothesVariant11_ReturnsGraphicIncompatibleClothes()
    {
        var error = Assert.Single(AvatarRules.Validate(Valid().With(AvatarSlot.Clothes, "variant11").With(AvatarSlot.ClothesGraphic, "galaxy")));

        Assert.Equal(AvatarSlot.ClothesGraphic, error.Slot);
        Assert.Equal(AvatarErrorCodes.GraphicIncompatibleClothes, error.Code);
    }

    [Fact]
    public void Validate_GraphicOnFreeClothesVariant14_ReturnsGraphicIncompatibleClothes()
    {
        // variant14 là áo miễn phí nhưng không in được hình.
        var error = Assert.Single(AvatarRules.Validate(Valid().With(AvatarSlot.Clothes, "variant14").With(AvatarSlot.ClothesGraphic, "saturn")));

        Assert.Equal(AvatarErrorCodes.GraphicIncompatibleClothes, error.Code);
    }

    [Theory]
    [InlineData("variant01")]
    [InlineData("variant14")]
    [InlineData("variant25")]
    public void Validate_NoGraphic_AnyClothesOk(string clothes) =>
        Assert.Empty(AvatarRules.Validate(Valid().With(AvatarSlot.Clothes, clothes)));

    [Fact]
    public void Validate_GraphicCheckRunsAfterSlotChecks()
    {
        // Có lỗi ô thì không báo thêm lỗi tương thích: người dùng sửa lỗi ô trước, rồi mới gặp lỗi tương thích.
        var selection = Valid().With(AvatarSlot.Hair, "variant99").With(AvatarSlot.Clothes, "variant11").With(AvatarSlot.ClothesGraphic, "galaxy");

        var error = Assert.Single(AvatarRules.Validate(selection));

        Assert.Equal(AvatarSlot.Hair, error.Slot);
        Assert.Equal(AvatarErrorCodes.UnknownVariant, error.Code);
    }

    [Fact]
    public void Validate_ReturnsErrorsInFixedSlotOrder()
    {
        var selection = Valid().With(AvatarSlot.Background, "ffffff").With(AvatarSlot.Mouth, null).With(AvatarSlot.Eyes, "variant99");

        var slots = AvatarRules.Validate(selection).Select(e => e.Slot).ToList();

        Assert.Equal([AvatarSlot.Eyes, AvatarSlot.Mouth, AvatarSlot.Background], slots);
    }
}
