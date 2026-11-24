using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Token)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(r => r.Token)
            .IsUnique();

        builder.Property(r => r.JwtId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.ReplacedByToken)
            .HasMaxLength(255);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.ExpiresAtUtc)
            .IsRequired();
    }
}
