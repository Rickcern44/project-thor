using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Features.Health;
using ProjectThor.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(string.IsNullOrWhiteSpace(connectionString) ? "Host=unconfigured" : connectionString));

var app = builder.Build();

app.MapGetHealth();

app.Run();

public partial class Program;
