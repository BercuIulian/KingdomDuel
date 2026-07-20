using KingdomDuel.Application;
using KingdomDuel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Api.Configurations;

public static class DatabaseConfiguration
{
    public static WebApplicationBuilder AddDatabaseConfiguration(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("KingdomDuelDb")
            ?? throw new InvalidOperationException("Connection string 'KingdomDuelDb' is not configured.");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npg => npg.MigrationsAssembly(PersistenceRoot.AssemblyName)));

        return builder;
    }

    public static async Task ApplyDatabaseMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
