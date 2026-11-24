using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Application.DTOs;

public record ContractDto(
    Guid Id,
    string Title,
    string Description,
    decimal ValueAmount,
    string Currency,
    ContractStatus Status,
    Guid CreatedById,
    string CreatedByName,
    Guid? AssignedApproverId,
    string? AssignedApproverName,
    DateTime ValidFromUtc,
    DateTime ValidToUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
