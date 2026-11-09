using QuizBattle.Api.Hubs;

namespace QuizBattle.Tests.Api;

public sealed class PlayerConnectionRegistryTests
{
    [Fact]
    public void PlayerConnectionRegistry_FirstConnection_NoKick()
    {
        var registry = new PlayerConnectionRegistry();

        Assert.Null(registry.Register(Guid.NewGuid(), "conn-1", "hash-a"));
    }

    [Fact]
    public void PlayerConnectionRegistry_NewConnectionSameHash_NoKick()
    {
        // Làm mới trang trên cùng thiết bị: cùng token nên không đá ai, chỉ thay kết nối.
        var registry = new PlayerConnectionRegistry();
        var playerId = Guid.NewGuid();
        registry.Register(playerId, "conn-1", "hash-a");

        Assert.Null(registry.Register(playerId, "conn-2", "hash-a"));
    }

    [Fact]
    public void PlayerConnectionRegistry_NewConnectionDifferentHash_ReturnsOldConnection()
    {
        var registry = new PlayerConnectionRegistry();
        var playerId = Guid.NewGuid();
        registry.Register(playerId, "conn-1", "hash-a");

        Assert.Equal("conn-1", registry.Register(playerId, "conn-2", "hash-b"));
    }

    [Fact]
    public void PlayerConnectionRegistry_DisconnectOfStaleConnection_DoesNotRemoveNewEntry()
    {
        var registry = new PlayerConnectionRegistry();
        var playerId = Guid.NewGuid();
        registry.Register(playerId, "conn-1", "hash-a");
        registry.Register(playerId, "conn-2", "hash-b");

        Assert.False(registry.Unregister(playerId, "conn-1")); // kết nối cũ ngắt: không phải chủ hiện tại
        Assert.Equal("conn-2", registry.Register(playerId, "conn-3", "hash-c")); // entry của conn-2 vẫn còn
    }

    [Fact]
    public void PlayerConnectionRegistry_DisconnectOfCurrentConnection_RemovesEntry()
    {
        var registry = new PlayerConnectionRegistry();
        var playerId = Guid.NewGuid();
        registry.Register(playerId, "conn-1", "hash-a");

        Assert.True(registry.Unregister(playerId, "conn-1"));
        Assert.Null(registry.Register(playerId, "conn-2", "hash-b")); // không còn gì để đá
    }
}
