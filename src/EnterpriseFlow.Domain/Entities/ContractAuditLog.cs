using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// Immutable audit trail record of contract status transitions and lifecycle actions.
/// </summary>
public class ContractAuditLog
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public Guid ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;
    public string ChangedByName { get; set; } = string.Empty;

    public ContractStatus PreviousStatus { get; set; }
    public ContractStatus NewStatus { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
