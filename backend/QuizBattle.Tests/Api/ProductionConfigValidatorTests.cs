using Microsoft.Extensions.Configuration;
using QuizBattle.Api.Extensions;

namespace QuizBattle.Tests.Api;

public sealed class ProductionConfigValidatorTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> Complete() => new()
    {
        ["Cors:AllowedOrigins"] = "https://quizbattle.vercel.app",
        ["Ai:ApiKey"] = "k",
        ["Ai:Model"] = "m",
    };

    [Fact]
    public void Validate_DailyLimitsNotSet_DoesNotThrow()
    {
        // Trần AI theo ngày có mặc định (20/300) nên không bắt buộc phải đặt.
        var values = Complete();
        Assert.False(values.ContainsKey("Ai:DailyLimitPerUser"));

        ProductionConfigValidator.Validate(Config(values));
    }

    [Fact]
    public void Validate_DailyLimitsValid_DoesNotThrow()
    {
        var values = Complete();
        values["Ai:DailyLimitPerUser"] = "3";
        values["Ai:DailyLimitGlobal"] = "5";

        ProductionConfigValidator.Validate(Config(values));
    }

    [Theory]
    [InlineData("Ai:DailyLimitPerUser", "Ai__DailyLimitPerUser", "0")]
    [InlineData("Ai:DailyLimitPerUser", "Ai__DailyLimitPerUser", "-1")]
    [InlineData("Ai:DailyLimitGlobal", "Ai__DailyLimitGlobal", "abc")]
    [InlineData("Ai:DailyLimitGlobal", "Ai__DailyLimitGlobal", "0")]
    public void Validate_DailyLimitInvalid_ThrowsNamingEnvVariable(string key, string envName, string value)
    {
        var values = Complete();
        values[key] = value;

        var ex = Assert.Throws<InvalidOperationException>(() => ProductionConfigValidator.Validate(Config(values)));

        Assert.Contains(envName, ex.Message);
    }

    [Fact]
    public void Validate_AllPresent_DoesNotThrow() => ProductionConfigValidator.Validate(Config(Complete()));

    [Theory]
    [InlineData("Cors:AllowedOrigins", "Cors__AllowedOrigins")]
    [InlineData("Ai:ApiKey", "Ai__ApiKey")]
    [InlineData("Ai:Model", "Ai__Model")]
    public void Validate_MissingKey_ThrowsNamingEnvVariable(string key, string envName)
    {
        var values = Complete();
        values.Remove(key);

        var ex = Assert.Throws<InvalidOperationException>(() => ProductionConfigValidator.Validate(Config(values)));

        Assert.Contains(envName, ex.Message);
    }

    [Fact]
    public void Validate_WildcardOrigin_Throws()
    {
        var values = Complete();
        values["Cors:AllowedOrigins"] = "https://a.app, *";

        Assert.Throws<InvalidOperationException>(() => ProductionConfigValidator.Validate(Config(values)));
    }
}
