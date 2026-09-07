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
        builder.Property(r => r.GameOverReason).HasConversion<string>().HasMaxLength(32);

        // Note: the FK back to matches (MatchId) is defined from MatchConfiguration's
        // HasMany(m => m.History) relationship.
    }
}
