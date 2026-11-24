using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public class ContractAuditLogConfiguration : IEntityTypeConfiguration<ContractAuditLog>
{
    public void Configure(EntityTypeBuilder<ContractAuditLog> builder)
    {
        builder.ToTable("contract_audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ChangedByName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.PreviousStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.NewStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Comment)
            .HasMaxLength(1000);

        builder.Property(a => a.TimestampUtc)
            .IsRequired();

        builder.HasIndex(a => a.ContractId);
        builder.HasIndex(a => a.ChangedById);
        builder.HasIndex(a => a.TimestampUtc);

        builder.HasOne(a => a.ChangedBy)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
