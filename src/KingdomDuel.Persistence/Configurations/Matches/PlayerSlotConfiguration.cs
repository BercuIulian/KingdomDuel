using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Rounds;
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

        builder.ComplexProperty(p => p.Kingdom, kingdom =>
        {
            kingdom.Property(k => k.Gold).HasColumnName("Gold");
            kingdom.Property(k => k.Troops).HasColumnName("Troops");
            kingdom.Property(k => k.Morale).HasColumnName("Morale");
            kingdom.Property(k => k.Fortification).HasColumnName("Fortification");
            kingdom.Property(k => k.Catapults).HasColumnName("Catapults");
            kingdom.Ignore(k => k.TotalPower);
        });

        builder.Property(p => p.PendingOrder)
            .HasColumnType("jsonb")
            .HasConversion(JsonColumns.Converter<OrderPlan>(), JsonColumns.Comparer<OrderPlan>());

        builder.Property(p => p.Caravan)
            .HasColumnType("jsonb")
            .HasConversion(JsonColumns.Converter<Caravan>(), JsonColumns.Comparer<Caravan>());
    }
}
