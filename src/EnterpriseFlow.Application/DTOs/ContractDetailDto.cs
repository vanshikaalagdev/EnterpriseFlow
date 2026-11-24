using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Application.DTOs;

public record ContractDetailDto(
    Guid Id,
    string Title,
    string Description,
    decimal ValueAmount,
    string Currency,
    ContractStatus Status,
    Guid CreatedById,
    string CreatedByName,
    string CreatedByEmail,
    Guid? AssignedApproverId,
    string? AssignedApproverName,
    DateTime ValidFromUtc,
    DateTime ValidToUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<ContractAuditLogDto> AuditLogs
);
