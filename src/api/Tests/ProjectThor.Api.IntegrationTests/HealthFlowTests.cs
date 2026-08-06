using System.Net.Http.Json;
using ProjectThor.Api.Features.Health;

namespace ProjectThor.Api.IntegrationTests;

public class HealthFlowTests : IAsyncLifetime
{
    private readonly ApiFixture _fixture = new();

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Status_reflects_database_reachability()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health", cancellationToken);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);

        Assert.True(health!.DatabaseReachable);
        Assert.Equal("healthy", health.Status);
    }
}
