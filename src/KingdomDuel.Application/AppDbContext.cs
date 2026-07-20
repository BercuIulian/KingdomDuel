using KingdomDuel.Application.Configurations;
using KingdomDuel.Domain.Entities.Matches;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<PlayerSlot> PlayerSlots => Set<PlayerSlot>();
    public DbSet<Kingdom> Kingdoms => Set<Kingdom>();
    public DbSet<RoundResult> RoundResults => Set<RoundResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyEntityConfigurations();
        base.OnModelCreating(modelBuilder);
    }
}
