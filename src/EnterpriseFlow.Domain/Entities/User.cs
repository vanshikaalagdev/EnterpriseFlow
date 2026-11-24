using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Requester;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation collections
    public ICollection<Contract> CreatedContracts { get; set; } = new List<Contract>();
    public ICollection<Contract> AssignedContracts { get; set; } = new List<Contract>();
    public ICollection<ContractAuditLog> AuditLogs { get; set; } = new List<ContractAuditLog>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
