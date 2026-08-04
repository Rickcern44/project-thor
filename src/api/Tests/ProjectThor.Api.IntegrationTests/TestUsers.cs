using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Api.Infrastructure.Auth;
using ProjectThor.Data;
using ProjectThor.Data.Entities;

namespace ProjectThor.Api.IntegrationTests;

/// <summary>Shared test helpers for seeding an active user and getting a signed-in HttpClient for them.</summary>
public static class TestUsers
{
    public static async Task<User> SeedActiveUserAsync(ApiWebApplicationFactory factory, UserRole role, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Phone = "555-0100",
            Name = "Test User",
            Role = role,
            Status = UserStatus.Active,
            ActivatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public static async Task<HttpClient> SignedInClientAsync(ApiWebApplicationFactory factory, User user, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (rawToken, hash) = MagicLinkTokenGenerator.Generate();
        dbContext.MagicLinkTokens.Add(new MagicLinkToken
        {
            UserId = user.Id,
            TokenHash = hash,
            Purpose = MagicLinkPurpose.Login,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/consume", new { Token = rawToken }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return client;
    }

    public static async Task<HttpClient> ActiveClientAsync(ApiWebApplicationFactory factory, UserRole role, CancellationToken cancellationToken)
    {
        var user = await SeedActiveUserAsync(factory, role, cancellationToken);
        return await SignedInClientAsync(factory, user, cancellationToken);
    }

    /// <summary>RosterRecord + linked Pending User, matching what roster-import resolution creates.</summary>
    public static async Task<(RosterRecord RosterRecord, User User)> SeedPendingPlayerAsync(
        ApiWebApplicationFactory factory, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rosterRecord = new RosterRecord
        {
            Name = "Imported Player",
            Email = $"{Guid.NewGuid()}@example.com",
            Phone = "555-0101",
            LegacyBalance = 0m
        };
        dbContext.RosterRecords.Add(rosterRecord);

        var user = new User
        {
            Email = rosterRecord.Email,
            Phone = rosterRecord.Phone,
            Name = rosterRecord.Name,
            Role = UserRole.Player,
            Status = UserStatus.Pending,
            RosterRecordId = rosterRecord.Id
        };
        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);
        return (rosterRecord, user);
    }
}
