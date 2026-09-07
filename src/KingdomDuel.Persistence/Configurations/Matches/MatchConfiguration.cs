using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingdomDuel.Persistence.Configurations.Matches;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(m => m.Id);

        builder.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(m => m.Code).HasMaxLength(16).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();

        builder.Property(m => m.RoundSeconds)
            .HasDefaultValue(GameConstants.DefaultRoundSeconds);

        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.CurrentEvent).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.GameOverReason).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(m => m.Status);

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

        builder.HasMany(m => m.History)
            .WithOne()
            .HasForeignKey("MatchId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
