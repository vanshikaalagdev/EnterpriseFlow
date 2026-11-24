using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

public class Contract
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ValueAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    public Guid? AssignedApproverId { get; set; }
    public User? AssignedApprover { get; set; }

    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ContractAuditLog> AuditLogs { get; set; } = new List<ContractAuditLog>();

    // Parameterless constructor for EF Core
    public Contract()
    {
    }

    /// <summary>
    /// Factory method to create a new draft contract with initial audit log.
    /// </summary>
    public static Contract Create(
        string title,
        string description,
        decimal valueAmount,
        string currency,
        DateTime validFromUtc,
        DateTime validToUtc,
        Guid createdById,
        string createdByName,
        Guid? assignedApproverId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Contract title cannot be empty.", nameof(title));

        if (valueAmount < 0)
            throw new ArgumentException("Contract value amount cannot be negative.", nameof(valueAmount));

        if (validToUtc <= validFromUtc)
            throw new ArgumentException("ValidToUtc must be greater than ValidFromUtc.", nameof(validToUtc));

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            ValueAmount = valueAmount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "INR" : currency.Trim().ToUpperInvariant(),
            Status = ContractStatus.Draft,
            CreatedById = createdById,
            AssignedApproverId = assignedApproverId,
            ValidFromUtc = validFromUtc,
            ValidToUtc = validToUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        contract.AuditLogs.Add(new ContractAuditLog
        {
            ContractId = contract.Id,
            ChangedById = createdById,
            ChangedByName = createdByName,
            PreviousStatus = ContractStatus.Draft,
            NewStatus = ContractStatus.Draft,
            Comment = "Contract draft created.",
            TimestampUtc = contract.CreatedAtUtc
        });

        return contract;
    }

    /// <summary>
    /// Update contract editable fields while in Draft or Rejected status.
    /// </summary>
    public void UpdateDetails(
        string title,
        string description,
        decimal valueAmount,
        string currency,
        DateTime validFromUtc,
        DateTime validToUtc,
        Guid? assignedApproverId,
        Guid modifiedById,
        string modifiedByName)
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.Rejected)
        {
            throw new InvalidStateTransitionException(
                Status,
                Status,
                $"Contract details can only be modified in 'Draft' or 'Rejected' state. Current state: '{Status}'.");
        }

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Contract title cannot be empty.", nameof(title));

        if (valueAmount < 0)
            throw new ArgumentException("Contract value amount cannot be negative.", nameof(valueAmount));

        if (validToUtc <= validFromUtc)
            throw new ArgumentException("ValidToUtc must be greater than ValidFromUtc.", nameof(validToUtc));

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        ValueAmount = valueAmount;
        Currency = string.IsNullOrWhiteSpace(currency) ? "INR" : currency.Trim().ToUpperInvariant();
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        AssignedApproverId = assignedApproverId;
        UpdatedAtUtc = DateTime.UtcNow;

        AuditLogs.Add(new ContractAuditLog
        {
            ContractId = Id,
            ChangedById = modifiedById,
            ChangedByName = modifiedByName,
            PreviousStatus = Status,
            NewStatus = Status,
            Comment = "Contract details updated.",
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Submit draft or rejected contract for legal review.
    /// </summary>
    public void SubmitForReview(Guid changedById, string changedByName, string? comment = null)
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.Rejected)
        {
            throw new InvalidStateTransitionException(
                Status,
                ContractStatus.PendingReview,
                "Only contracts in 'Draft' or 'Rejected' state can be submitted for review.");
        }

        var previousStatus = Status;
        Status = ContractStatus.PendingReview;
        UpdatedAtUtc = DateTime.UtcNow;

        AuditLogs.Add(new ContractAuditLog
        {
            ContractId = Id,
            ChangedById = changedById,
            ChangedByName = changedByName,
            PreviousStatus = previousStatus,
            NewStatus = ContractStatus.PendingReview,
            Comment = string.IsNullOrWhiteSpace(comment) ? "Contract submitted for legal review." : comment.Trim(),
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Approve contract review (Legal Approver or Admin only).
    /// </summary>
    public void Approve(Guid approverId, string approverName, UserRole approverRole, string? comment = null)
    {
        if (approverRole != UserRole.LegalApprover && approverRole != UserRole.Admin)
        {
            throw new UnauthorizedWorkflowActionException("Approve", approverRole, "LegalApprover, Admin");
        }

        if (Status != ContractStatus.PendingReview)
        {
            throw new InvalidStateTransitionException(
                Status,
                ContractStatus.Approved,
                "Only contracts in 'PendingReview' state can be approved.");
        }

        var previousStatus = Status;
        Status = ContractStatus.Approved;
        AssignedApproverId ??= approverId;
        UpdatedAtUtc = DateTime.UtcNow;

        AuditLogs.Add(new ContractAuditLog
        {
            ContractId = Id,
            ChangedById = approverId,
            ChangedByName = approverName,
            PreviousStatus = previousStatus,
            NewStatus = ContractStatus.Approved,
            Comment = string.IsNullOrWhiteSpace(comment) ? "Contract legally approved." : comment.Trim(),
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Reject contract review (Legal Approver or Admin only).
    /// </summary>
    public void Reject(Guid approverId, string approverName, UserRole approverRole, string reason)
    {
        if (approverRole != UserRole.LegalApprover && approverRole != UserRole.Admin)
        {
            throw new UnauthorizedWorkflowActionException("Reject", approverRole, "LegalApprover, Admin");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A rejection reason must be provided.", nameof(reason));
        }

        if (Status != ContractStatus.PendingReview)
        {
            throw new InvalidStateTransitionException(
                Status,
                ContractStatus.Rejected,
                "Only contracts in 'PendingReview' state can be rejected.");
        }

        var previousStatus = Status;
        Status = ContractStatus.Rejected;
        AssignedApproverId ??= approverId;
        UpdatedAtUtc = DateTime.UtcNow;

        AuditLogs.Add(new ContractAuditLog
        {
            ContractId = Id,
            ChangedById = approverId,
            ChangedByName = approverName,
            PreviousStatus = previousStatus,
            NewStatus = ContractStatus.Rejected,
            Comment = $"Rejected: {reason.Trim()}",
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Activate approved contract.
    /// </summary>
    public void Activate(Guid actorId, string actorName, UserRole actorRole, DateTime utcNow, string? comment = null)
    {
        if (Status != ContractStatus.Approved)
        {
            throw new InvalidStateTransitionException(
                Status,
                ContractStatus.Active,
                "Only contracts in 'Approved' state can be activated.");
        }

        if (utcNow > ValidToUtc)
        {
            throw new InvalidStateTransitionException(
                Status,
                ContractStatus.Active,
                $"Cannot activate expired contract. ValidToUtc was '{ValidToUtc:yyyy-MM-dd HH:mm:ss UTC}' and current time is '{utcNow:yyyy-MM-dd HH:mm:ss UTC}'.");
        }

        var previousStatus = Status;
        Status = ContractStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;

        AuditLogs.Add(new ContractAuditLog
        {
            ContractId = Id,
            ChangedById = actorId,
            ChangedByName = actorName,
            PreviousStatus = previousStatus,
            NewStatus = ContractStatus.Active,
            Comment = string.IsNullOrWhiteSpace(comment) ? "Contract activated." : comment.Trim(),
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Terminate contract early (Admin or Legal Approver, or creator in Draft).
    /// </summary>
    public void Terminate(Guid actorId, string actorName, UserRole actorRole, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A termination reason must be provided.", nameof(reason));

        if (Status == ContractStatus.Terminated || Status == ContractStatus.Expired)
        {
            throw new InvalidStateTransitionException(
                Status,
                ContractStatus.Terminated,
                $"Contract is already in terminal state '{Status}'.");
        }

        if (actorRole != UserRole.Admin && actorRole != UserRole.LegalApprover && actorId != CreatedById)
        {
            throw new UnauthorizedWorkflowActionException("Terminate", actorRole, "Admin, LegalApprover or Contract Creator");
        }

        var previousStatus = Status;
        Status = ContractStatus.Terminated;
        UpdatedAtUtc = DateTime.UtcNow;

        AuditLogs.Add(new ContractAuditLog
        {
            ContractId = Id,
            ChangedById = actorId,
            ChangedByName = actorName,
            PreviousStatus = previousStatus,
            NewStatus = ContractStatus.Terminated,
            Comment = $"Terminated: {reason.Trim()}",
            TimestampUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Expire an active contract when its validity date has passed.
    /// </summary>
    public bool CheckAndExpire(DateTime utcNow, Guid? systemUserId = null, string systemUserName = "System Workflow Engine")
    {
        if (Status == ContractStatus.Active && utcNow > ValidToUtc)
        {
            var previousStatus = Status;
            Status = ContractStatus.Expired;
            UpdatedAtUtc = DateTime.UtcNow;

            AuditLogs.Add(new ContractAuditLog
            {
                ContractId = Id,
                ChangedById = systemUserId ?? Guid.Empty,
                ChangedByName = systemUserName,
                PreviousStatus = previousStatus,
                NewStatus = ContractStatus.Expired,
                Comment = "Contract validity expired.",
                TimestampUtc = DateTime.UtcNow
            });
            return true;
        }

        return false;
    }
}
