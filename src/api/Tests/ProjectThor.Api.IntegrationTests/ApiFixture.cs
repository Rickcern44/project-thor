using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectThor.Data;
using Testcontainers.PostgreSql;

namespace ProjectThor.Api.IntegrationTests;

/// <summary>Boots a real Postgres (Testcontainers) and the full app pipeline against it, migrated.</summary>
public class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("projectthor")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public ApiWebApplicationFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new ApiWebApplicationFactory(_container.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}
