using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingdomDuel.Persistence.Configurations.Matches;

internal sealed class MatchRecordConfiguration : IEntityTypeConfiguration<MatchRecord>
{
    public void Configure(EntityTypeBuilder<MatchRecord> builder)
    {
        builder.ToTable("match_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OpponentName).HasMaxLength(24).IsRequired();

        builder.Property(r => r.Outcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(32);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.UserId, r.StartedAt })
            .IsDescending(false, true)
            .IsUnique();
    }
}
