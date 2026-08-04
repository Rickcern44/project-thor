using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProjectThor.Api.Features.Auth;
using ProjectThor.Api.Features.Charges;
using ProjectThor.Api.Features.GameTemplates;
using ProjectThor.Api.Features.Games;
using ProjectThor.Api.Features.Health;
using ProjectThor.Api.Features.Invites;
using ProjectThor.Api.Features.Notifications;
using ProjectThor.Api.Features.SignUps;
using ProjectThor.Api.Infrastructure.Email;
using ProjectThor.Api.Infrastructure.Notifications;
using ProjectThor.Api.Infrastructure.Payments;
using ProjectThor.Api.Infrastructure.Scheduling;
using ProjectThor.Data.Entities;
using ProjectThor.Data;
using WebPush;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Resolved lazily via IConfiguration at first use (not read eagerly here) so that
// WebApplicationFactory-based tests can override ConnectionStrings:Default after Program.cs runs.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default");
    options.UseNpgsql(string.IsNullOrWhiteSpace(connectionString) ? "Host=unconfigured" : connectionString);
});

builder.Services.Configure<ResendOptions>(builder.Configuration.GetSection(ResendOptions.SectionName));
builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
{
    var resendOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResendOptions>>().Value;
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resendOptions.ApiKey);
});

builder.Services.Configure<VapidOptions>(builder.Configuration.GetSection(VapidOptions.SectionName));
builder.Services.AddSingleton<WebPushClient>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<SignupOpenNotificationService>();
builder.Services.AddHostedService<NotificationBackgroundService>();

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

builder.Services.AddScoped<GameSchedulingService>();
builder.Services.AddHostedService<GameSchedulingBackgroundService>();

builder.Services.AddScoped<PaymentReconciliationService>();
builder.Services.AddHostedService<PaymentReconciliationBackgroundService>();

var app = builder.Build();

// Local dev convenience under Aspire: apply migrations automatically instead of a manual
// `dotnet ef database update` step. Not relevant to production (K8s runs a built, migrated image).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGetHealth();
app.MapRequestLoginLink();
app.MapConsumeMagicLink();
app.MapLogout();
app.MapMe();
app.MapIssueInvite();

app.MapCreateGameTemplate();
app.MapGetActiveGameTemplate();
app.MapUpdateGameTemplate();
app.MapDeactivateGameTemplate();

app.MapGetLiveGame();
app.MapCreateAdHocGame();
app.MapUpdateGame();
app.MapCancelGame();
app.MapListPastGames();

app.MapSignUpForGame();
app.MapCancelSignUp();
app.MapGetGameRoster();
app.MapAdminAddPlayer();
app.MapAdminRemovePlayer();
app.MapAdminPromoteFromWaitlist();

app.MapWaiveCharge();
app.MapPayCharge();
app.MapGetPlayerBalance();
app.MapListOutstandingBalances();

app.MapGetNotifications();
app.MapMarkNotificationRead();
app.MapGetVapidPublicKey();
app.MapSubscribeToPush();
app.MapUnsubscribeFromPush();

app.Run();

public partial class Program;
