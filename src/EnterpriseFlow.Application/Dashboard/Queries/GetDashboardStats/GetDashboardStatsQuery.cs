using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Dashboard.Queries.GetDashboardStats;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public GetDashboardStatsQueryHandler(
        IApplicationDbContext context,
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "dashboard:stats";

        var cached = await _cacheService.GetAsync<DashboardStatsDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var contracts = await _context.Contracts
            .AsNoTracking()
            .Select(c => new { c.Status, c.ValueAmount, c.Currency })
            .ToListAsync(cancellationToken);

        var total = contracts.Count;
        var drafts = contracts.Count(c => c.Status == ContractStatus.Draft);
        var pending = contracts.Count(c => c.Status == ContractStatus.PendingReview);
        var approved = contracts.Count(c => c.Status == ContractStatus.Approved);
        var active = contracts.Count(c => c.Status == ContractStatus.Active);
        var rejected = contracts.Count(c => c.Status == ContractStatus.Rejected);
        var expired = contracts.Count(c => c.Status == ContractStatus.Expired);
        var terminated = contracts.Count(c => c.Status == ContractStatus.Terminated);

        var totalActiveValue = contracts
            .Where(c => c.Status == ContractStatus.Active)
            .Sum(c => c.ValueAmount);

        var stats = new DashboardStatsDto(
            TotalContracts: total,
            DraftContracts: drafts,
            PendingReviewContracts: pending,
            ApprovedContracts: approved,
            ActiveContracts: active,
            RejectedContracts: rejected,
            ExpiredContracts: expired,
            TerminatedContracts: terminated,
            TotalActiveValue: totalActiveValue,
            Currency: "INR"
        );

        await _cacheService.SetAsync(
            cacheKey,
            stats,
            slidingExpiration: TimeSpan.FromMinutes(5),
            cancellationToken: cancellationToken);

        return stats;
    }
}
