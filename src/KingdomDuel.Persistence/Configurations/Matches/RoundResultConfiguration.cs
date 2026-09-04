using KingdomDuel.Domain.Entities.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingdomDuel.Persistence.Configurations.Matches;

internal sealed class RoundResultConfiguration : IEntityTypeConfiguration<RoundResult>
{
    public void Configure(EntityTypeBuilder<RoundResult> builder)
    {
        builder.ToTable("round_results");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoundNumber);
        builder.Property(r => r.Event).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.Player1Action).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.Player2Action).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.Player1AttackTroops);
        builder.Property(r => r.Player2AttackTroops);
        builder.Property(r => r.GameOverReason).HasConversion<string>().HasMaxLength(32);

        // The per-round log lines are primitives - stored as a Postgres text[] column.
        builder.Property(r => r.Log).HasColumnType("text[]");

        // Note: the FK back to matches (MatchId) is defined from MatchConfiguration's
        // HasMany(m => m.History) relationship.
    }
}
