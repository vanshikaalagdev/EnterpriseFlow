using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Application.DTOs;

public record ContractAuditLogDto(
    Guid Id,
    Guid ContractId,
    Guid ChangedById,
    string ChangedByName,
    ContractStatus PreviousStatus,
    ContractStatus NewStatus,
    string Comment,
    DateTime TimestampUtc
);
