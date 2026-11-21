using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Api.Extensions;
using QuizBattle.Application.Practice;
using QuizBattle.Infrastructure;

namespace QuizBattle.Tests.Practice;

public sealed class PracticeOptionsTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    // AddInfrastructure chỉ cần chuỗi kết nối và Jwt hợp lệ để chạy tới các bước Parse; không mở kết nối DB.
    private static Dictionary<string, string?> BaseSettings() => new()
    {
        ["ConnectionStrings:Default"] = "Host=localhost;Database=x;Username=x;Password=x",
        ["Jwt:Key"] = "test-only-jwt-key-0123456789-abcdefghij",
        ["Jwt:Issuer"] = "quizbattle-test",
        ["Jwt:Audience"] = "quizbattle-test",
    };

    [Fact]
    public void Parse_Missing_UsesDefaults5And100() =>
        Assert.Equal(new PracticeOptions(5, 100), PracticeOptions.Parse(null, ""));

    [Fact]
    public void Parse_Zero_IsValid() =>
        Assert.Equal(new PracticeOptions(0, 0), PracticeOptions.Parse("0", "0"));

    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("1.5")]
    public void Parse_InvalidValue_ThrowsNamingEnvVariable(string raw)
    {
        Assert.Contains("Practice__XpPerCorrect", Assert.Throws<InvalidOperationException>(() => PracticeOptions.Parse(raw, null)).Message);
        Assert.Contains("Practice__DailyXpCap", Assert.Throws<InvalidOperationException>(() => PracticeOptions.Parse(null, raw)).Message);
    }

    [Fact]
    public void Startup_NegativeXpPerCorrect_FailsWithClearMessage()
    {
        var values = BaseSettings();
        values["Practice:XpPerCorrect"] = "-1";

        var ex = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(Config(values)));

        Assert.Contains("Practice__XpPerCorrect", ex.Message);
    }

    [Fact]
    public void Startup_NegativeDailyXpCap_FailsWithClearMessage()
    {
        var values = BaseSettings();
        values["Practice:DailyXpCap"] = "-5";

        var ex = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(Config(values)));

        Assert.Contains("Practice__DailyXpCap", ex.Message);
    }

    [Fact]
    public void Startup_ZeroValues_RegistersOptions()
    {
        var values = BaseSettings();
        values["Practice:XpPerCorrect"] = "0";
        values["Practice:DailyXpCap"] = "0";
        var services = new ServiceCollection();

        services.AddInfrastructure(Config(values));

        using var provider = services.BuildServiceProvider();
        Assert.Equal(new PracticeOptions(0, 0), provider.GetRequiredService<PracticeOptions>());
    }

    [Fact]
    public void ProductionConfigValidator_DoesNotRequirePracticeKeys()
    {
        var production = Config(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins"] = "https://quizbattle.vercel.app",
            ["Ai:ApiKey"] = "k",
            ["Ai:Model"] = "m",
        });

        ProductionConfigValidator.Validate(production); // không ném dù thiếu Practice:*
    }
}
