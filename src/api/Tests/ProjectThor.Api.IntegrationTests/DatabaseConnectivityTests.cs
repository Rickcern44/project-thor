using Microsoft.EntityFrameworkCore;

namespace ProjectThor.Api.IntegrationTests;

public class DatabaseConnectivityTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Migrations_apply_and_database_is_reachable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = _fixture.CreateContext();

        await context.Database.MigrateAsync(cancellationToken);

        var canConnect = await context.Database.CanConnectAsync(cancellationToken);

        Assert.True(canConnect);
    }
}
