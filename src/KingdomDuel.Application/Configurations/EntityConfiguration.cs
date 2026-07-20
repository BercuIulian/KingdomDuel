using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Configurations;

internal static class EntityConfiguration
{
    /// <summary>
    /// Loads every <see cref="IEntityTypeConfiguration{TEntity}"/> from the Persistence
    /// assembly. Persistence is referenced at runtime only (loaded by name) to keep the
    /// Application → Persistence direction out of the compile graph.
    /// </summary>
    internal static void ApplyEntityConfigurations(this ModelBuilder modelBuilder)
    {
        var assembly = Assembly.Load("KingdomDuel.Persistence");
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
