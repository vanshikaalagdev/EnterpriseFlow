using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Contract> Contracts { get; }
    DbSet<ContractAuditLog> ContractAuditLogs { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
