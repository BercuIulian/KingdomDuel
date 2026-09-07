using KingdomDuel.Application.Configurations;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<PlayerSlot> PlayerSlots => Set<PlayerSlot>();
    public DbSet<RoundResult> RoundResults => Set<RoundResult>();
    public DbSet<MatchRecord> MatchRecords => Set<MatchRecord>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyEntityConfigurations();
        base.OnModelCreating(modelBuilder);
    }
}
