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

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.UserId);

        builder.HasOne(p => p.Kingdom)
            .WithOne()
            .HasForeignKey<Kingdom>("PlayerSlotId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Kingdom).IsRequired();
    }
}
