using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Rounds;
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

        // Alocarea, starea de după și evenimentele sunt date de afișare: nu se
        // interoghează după ele, iar forma li se mai schimbă la fiecare regulă nouă.
        builder.Property(r => r.Player1)
            .HasColumnType("jsonb")
            .HasConversion(JsonColumns.Converter<RoundSide>(), JsonColumns.Comparer<RoundSide>());

        builder.Property(r => r.Player2)
            .HasColumnType("jsonb")
            .HasConversion(JsonColumns.Converter<RoundSide>(), JsonColumns.Comparer<RoundSide>());

        builder.Property(r => r.Events)
            .HasColumnType("jsonb")
            .HasConversion(JsonColumns.Converter<List<RoundEvent>>(), JsonColumns.Comparer<List<RoundEvent>>());

        // Note: the FK back to matches (MatchId) is defined from MatchConfiguration's
        // HasMany(m => m.History) relationship.
    }
}
