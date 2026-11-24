using System.Reflection;
using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractAuditLog> ContractAuditLogs => Set<ContractAuditLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
