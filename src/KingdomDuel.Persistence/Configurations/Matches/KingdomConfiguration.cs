using KingdomDuel.Domain.Entities.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingdomDuel.Persistence.Configurations.Matches;

internal sealed class KingdomConfiguration : IEntityTypeConfiguration<Kingdom>
{
    public void Configure(EntityTypeBuilder<Kingdom> builder)
    {
        builder.ToTable("kingdoms");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Gold);
        builder.Property(k => k.Troops);
        builder.Property(k => k.Morale);
        builder.Property(k => k.DefenseStreak);

        // Derived from the resource values - never stored.
        builder.Ignore(k => k.TotalPower);
        builder.Ignore(k => k.EquipmentLevel);
    }
}
