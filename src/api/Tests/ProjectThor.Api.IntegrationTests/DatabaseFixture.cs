using Microsoft.EntityFrameworkCore;
using ProjectThor.Data;
using Testcontainers.PostgreSql;

namespace ProjectThor.Api.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("projectthor")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public AppDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(_container.GetConnectionString());
        return new AppDbContext(optionsBuilder.Options);
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
