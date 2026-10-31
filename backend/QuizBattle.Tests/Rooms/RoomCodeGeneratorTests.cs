using QuizBattle.Infrastructure.Security;

namespace QuizBattle.Tests.Rooms;

public sealed class RoomCodeGeneratorTests
{
    private const string Allowed = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private static readonly char[] Forbidden = ['0', 'O', '1', 'I', 'L'];

    [Fact]
    public void RoomCodeGenerator_UsesOnlyAllowedCharacters()
    {
        var generator = new RoomCodeGenerator();

        for (var i = 0; i < 1000; i++)
        {
            var code = generator.Generate();

            Assert.Equal(6, code.Length);
            Assert.All(code, c => Assert.Contains(c, Allowed));
            Assert.All(code, c => Assert.DoesNotContain(c, Forbidden));
        }
    }
}
