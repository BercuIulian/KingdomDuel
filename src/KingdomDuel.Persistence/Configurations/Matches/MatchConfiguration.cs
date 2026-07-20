using System.Text.Json;
using System.Text.Json.Serialization;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KingdomDuel.Persistence.Configurations.Matches;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).HasMaxLength(16).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();

        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.CurrentEvent).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.GameOverReason).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(m => m.Status);

        // The two seats are separate rows in "player_slots". The FK lives on matches
        // (Player1Id/Player2Id) - a match points at its two players.
        builder.HasOne(m => m.Player1)
            .WithOne()
            .HasForeignKey<Match>("Player1Id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.Player2)
            .WithOne()
            .HasForeignKey<Match>("Player2Id")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(m => m.Player1).IsRequired(false);
        builder.Navigation(m => m.Player2).IsRequired(false);

        // History is a one-to-many of resolved rounds in "round_results" (FK MatchId there).
        builder.HasMany(m => m.History)
            .WithOne()
            .HasForeignKey("MatchId")
            .OnDelete(DeleteBehavior.Cascade);

        // Round-in-progress actions are a transient map of primitives, not an entity - kept as
        // jsonb. A serialize-based ValueComparer gives EF a real snapshot so in-place mutations
        // (dict indexer, Clear) are detected.
        builder.Property(m => m.PendingActions)
            .HasColumnName("PendingActions")
            .HasColumnType("jsonb")
            .HasConversion(
                new ValueConverter<Dictionary<string, ActionType>, string>(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<Dictionary<string, ActionType>>(v, JsonOptions) ?? new()),
                CreateComparer<Dictionary<string, ActionType>>());
    }

    private static ValueComparer<T> CreateComparer<T>() => new(
        (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
        v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
        v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, JsonOptions), JsonOptions)!);
}
