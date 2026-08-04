using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Features.Auth;
using ProjectThor.Api.Features.Health;
using ProjectThor.Api.Features.Invites;
using ProjectThor.Api.Infrastructure.Email;
using ProjectThor.Data.Entities;
using ProjectThor.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(string.IsNullOrWhiteSpace(connectionString) ? "Host=unconfigured" : connectionString));

builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection(ResendOptions.SectionName));
builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
{
    var resendOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResendOptions>>().Value;
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resendOptions.ApiKey);
});

var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(frontendOrigin)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "pt_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));

var app = builder.Build();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGetHealth();
app.MapRequestLoginLink();
app.MapConsumeMagicLink();
app.MapLogout();
app.MapMe();
app.MapIssueInvite();

app.Run();

public partial class Program;
