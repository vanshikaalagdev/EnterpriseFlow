using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>
/// Thrown when an illegal lifecycle state transition is requested.
/// </summary>
public class InvalidStateTransitionException : DomainException
{
    public ContractStatus CurrentStatus { get; }
    public ContractStatus TargetStatus { get; }

    public InvalidStateTransitionException(ContractStatus currentStatus, ContractStatus targetStatus, string? reason = null)
        : base(string.IsNullOrWhiteSpace(reason)
            ? $"Invalid state transition from '{currentStatus}' to '{targetStatus}'."
            : $"Invalid state transition from '{currentStatus}' to '{targetStatus}'. Reason: {reason}")
    {
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }
}
