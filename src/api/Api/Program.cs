using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Features.Health;
using ProjectThor.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}

var app = builder.Build();

app.MapGetHealth();

app.Run();

public partial class Program;
