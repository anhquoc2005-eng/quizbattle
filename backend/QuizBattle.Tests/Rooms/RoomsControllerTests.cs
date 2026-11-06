using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Controllers;
using QuizBattle.Application.Rooms;

namespace QuizBattle.Tests.Rooms;

public sealed class RoomsControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedAtGetRoomWithCode()
    {
        var response = new RoomResponse(Guid.NewGuid(), "ABCDEF", "Java", "Draft", 20, DateTimeOffset.UtcNow, 0, 0);
        var controller = new RoomsController(new StubRoomService(response))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())])),
                },
            },
        };

        var result = await controller.Create(new CreateRoomRequest { Topic = "Java" }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(nameof(RoomsController.Get), created.ActionName);
        Assert.Equal("ABCDEF", created.RouteValues!["code"]);
        Assert.Same(response, created.Value);
    }

    private sealed class StubRoomService(RoomResponse response) : IRoomService
    {
        public Task<RoomResponse> CreateRoomAsync(Guid hostUserId, CreateRoomRequest request, CancellationToken cancellationToken) => Task.FromResult(response);

        public Task<IReadOnlyList<RoomResponse>> GetRoomsAsync(Guid hostUserId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<RoomResponse> GetRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteRoomAsync(Guid hostUserId, string code, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
