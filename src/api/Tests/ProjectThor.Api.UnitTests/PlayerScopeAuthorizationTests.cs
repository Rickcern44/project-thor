using System.Security.Claims;
using ProjectThor.Api.Infrastructure.Auth;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.UnitTests;

public class PlayerScopeAuthorizationTests
{
    [Fact]
    public void Admin_can_act_on_any_player()
    {
        var admin = BuildPrincipal(Guid.NewGuid(), UserRole.Admin);

        Assert.True(PlayerScopeAuthorization.CanActOnPlayer(admin, Guid.NewGuid()));
    }

    [Fact]
    public void Player_can_act_on_their_own_account()
    {
        var playerId = Guid.NewGuid();
        var player = BuildPrincipal(playerId, UserRole.Player);

        Assert.True(PlayerScopeAuthorization.CanActOnPlayer(player, playerId));
    }

    [Fact]
    public void Player_cannot_act_on_another_players_account()
    {
        var player = BuildPrincipal(Guid.NewGuid(), UserRole.Player);

        Assert.False(PlayerScopeAuthorization.CanActOnPlayer(player, Guid.NewGuid()));
    }

    private static ClaimsPrincipal BuildPrincipal(Guid userId, UserRole role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}
