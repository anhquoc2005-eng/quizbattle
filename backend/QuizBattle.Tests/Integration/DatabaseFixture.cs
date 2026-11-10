using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuizBattle.Infrastructure.Persistence;

namespace QuizBattle.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}

/// <summary>
/// Một database tạm (qb_test_&lt;guid&gt;) cho cả lần chạy test: migrate bằng chuỗi migration thật, xóa khi xong.
/// Thiếu QUIZBATTLE_TEST_DB thì không tạo gì (các test dùng RequiresDatabaseFact đã tự bỏ qua).
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private string? _adminConnectionString;
    private string? _testDatabaseName;

    public string? ConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable(RequiresDatabaseFactAttribute.EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(_adminConnectionString))
        {
            return;
        }

        _testDatabaseName = $"qb_test_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_testDatabaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _testDatabaseName,
        }.ConnectionString;

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_adminConnectionString is null || _testDatabaseName is null)
        {
            return;
        }

        try
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_adminConnectionString);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{_testDatabaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // Không làm test fail vì dọn dẹp; ghi tên DB để dọn tay.
            Console.Error.WriteLine($"Không xóa được database tạm {_testDatabaseName}: {ex.Message}");
        }
    }

    /// <summary>Mỗi lần gọi trả một DbContext mới (test đồng thời cần nhiều context riêng).</summary>
    public AppDbContext CreateContext()
    {
        if (ConnectionString is null)
        {
            throw new InvalidOperationException($"Chưa đặt {RequiresDatabaseFactAttribute.EnvironmentVariable}.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options;
        return new AppDbContext(options);
    }
}
