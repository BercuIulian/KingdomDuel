using KingdomDuel.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingdomDuel.Persistence.Configurations.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName).HasMaxLength(24).IsRequired();

        builder.Property(u => u.NormalizedUserName).HasMaxLength(24).IsRequired();
        builder.HasIndex(u => u.NormalizedUserName).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.TokenVersion);
        builder.Property(u => u.CreatedAt);
    }
}
