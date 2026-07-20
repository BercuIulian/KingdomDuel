using KingdomDuel.Domain.Entities.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingdomDuel.Persistence.Configurations.Matches;

internal sealed class PlayerSlotConfiguration : IEntityTypeConfiguration<PlayerSlot>
{
    public void Configure(EntityTypeBuilder<PlayerSlot> builder)
    {
        builder.ToTable("player_slots");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PlayerId).HasMaxLength(64).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(64).IsRequired();

        // Each slot has exactly one Kingdom, stored in its own "kingdoms" table (FK there).
        builder.HasOne(p => p.Kingdom)
            .WithOne()
            .HasForeignKey<Kingdom>("PlayerSlotId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Kingdom).IsRequired();
    }
}
