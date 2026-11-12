using QuizBattle.Domain.Avatars;
using QuizBattle.Domain.Enums;

namespace QuizBattle.Tests.Domain;

public sealed class AvatarSelectionTests
{
    private static AvatarSelection Sample() => AvatarSelection.Empty
        .With(AvatarSlot.Eyes, "variant01")
        .With(AvatarSlot.Hair, "variant05")
        .With(AvatarSlot.Gesture, "ok");

    [Fact]
    public void Equals_SameContentDifferentInstances_AreEqual()
    {
        var a = Sample();
        var b = Sample();

        Assert.NotSame(a, b);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_InsertionOrderDoesNotMatter()
    {
        var a = AvatarSelection.Empty.With(AvatarSlot.Hair, "variant05").With(AvatarSlot.Eyes, "variant01");
        var b = AvatarSelection.Empty.With(AvatarSlot.Eyes, "variant01").With(AvatarSlot.Hair, "variant05");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_AbsentSlotEqualsExplicitNull()
    {
        var absent = AvatarSelection.Empty.With(AvatarSlot.Eyes, "variant01");
        var explicitNull = absent.With(AvatarSlot.Glasses, null).With(AvatarSlot.Background, null);

        Assert.Equal(absent, explicitNull);
        Assert.Equal(absent.GetHashCode(), explicitNull.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentValueInAnySlot_AreNotEqual()
    {
        foreach (var slot in AvatarSlots.All)
        {
            var a = Sample().With(slot, "variant01");
            var b = Sample().With(slot, "variant02");

            Assert.NotEqual(a, b);
        }
    }

    [Fact]
    public void Equals_SlotSetVersusEmpty_AreNotEqual()
    {
        Assert.NotEqual(Sample(), AvatarSelection.Empty);
        Assert.NotEqual(Sample(), Sample().With(AvatarSlot.Beard, "variant01"));
    }

    [Fact]
    public void Equals_Null_IsFalse() => Assert.False(Sample().Equals(null));

    [Fact]
    public void DefaultGenerator_SameInput_ProducesEqualSelections()
    {
        var id = Guid.Parse("3f2c9a1e-8b47-4d65-9c1a-5e0d7a6b2f10");

        Assert.Equal(DefaultAvatarGenerator.ForAccount(id), DefaultAvatarGenerator.ForAccount(id));
        Assert.Equal(DefaultAvatarGenerator.ForGuest("An"), DefaultAvatarGenerator.ForGuest(" an "));
    }
}
