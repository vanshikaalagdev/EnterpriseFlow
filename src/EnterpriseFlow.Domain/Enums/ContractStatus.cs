namespace EnterpriseFlow.Domain.Enums;

/// <summary>
/// Deterministic lifecycle status of an enterprise contract.
/// </summary>
public enum ContractStatus
{
    Draft = 0,
    PendingReview = 1,
    Approved = 2,
    Rejected = 3,
    Active = 4,
    Expired = 5,
    Terminated = 6
}
