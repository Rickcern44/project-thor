using Microsoft.EntityFrameworkCore;
using ProjectThor.Data.Entities;

namespace ProjectThor.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<RosterRecord> RosterRecords => Set<RosterRecord>();
    public DbSet<FlaggedImportRow> FlaggedImportRows => Set<FlaggedImportRow>();
    public DbSet<GameTemplate> GameTemplates => Set<GameTemplate>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<SignUp> SignUps => Set<SignUp>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
