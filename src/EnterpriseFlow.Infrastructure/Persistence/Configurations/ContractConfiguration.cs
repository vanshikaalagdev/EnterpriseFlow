using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.ValueAmount)
            .HasPrecision(18, 2);

        builder.Property(c => c.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("INR");

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.ValidFromUtc)
            .IsRequired();

        builder.Property(c => c.ValidToUtc)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedById);
        builder.HasIndex(c => c.AssignedApproverId);
        builder.HasIndex(c => c.CreatedAtUtc);

        builder.HasOne(c => c.CreatedBy)
            .WithMany(u => u.CreatedContracts)
            .HasForeignKey(c => c.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.AssignedApprover)
            .WithMany(u => u.AssignedContracts)
            .HasForeignKey(c => c.AssignedApproverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.AuditLogs)
            .WithOne(a => a.Contract)
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
