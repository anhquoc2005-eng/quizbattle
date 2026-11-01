using QuizBattle.Application.Game;

namespace QuizBattle.Tests.Game;

public sealed class SessionTokenHasherTests
{
    [Fact]
    public void Hash_SameToken_ReturnsSameHash()
    {
        Assert.Equal(SessionTokenHasher.Hash("abc123"), SessionTokenHasher.Hash("abc123"));
    }

    [Fact]
    public void Hash_DifferentTokens_ReturnDifferentHash()
    {
        Assert.NotEqual(SessionTokenHasher.Hash("abc123"), SessionTokenHasher.Hash("xyz789"));
    }
}
