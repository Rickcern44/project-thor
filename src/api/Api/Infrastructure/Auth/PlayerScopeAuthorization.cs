using System.Security.Claims;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.Infrastructure.Auth;

/// <summary>
/// Enforces that a Player can only act on their own account; an Admin can act on any account.
/// Used by self-service endpoints (sign-up, cancel) that take a target player id.
/// </summary>
public static class PlayerScopeAuthorization
{
    public static bool CanActOnPlayer(ClaimsPrincipal principal, Guid targetPlayerId)
    {
        if (principal.IsInRole(nameof(UserRole.Admin)))
        {
            return true;
        }

        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(currentUserId, out var parsed) && parsed == targetPlayerId;
    }
}
