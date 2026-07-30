using ProjectThor.Api.Features.Health;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGetHealth();

app.Run();

public partial class Program;
