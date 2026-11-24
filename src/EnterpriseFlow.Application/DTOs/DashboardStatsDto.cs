namespace EnterpriseFlow.Application.DTOs;

public record DashboardStatsDto(
    int TotalContracts,
    int DraftContracts,
    int PendingReviewContracts,
    int ApprovedContracts,
    int ActiveContracts,
    int RejectedContracts,
    int ExpiredContracts,
    int TerminatedContracts,
    decimal TotalActiveValue,
    string Currency
);
