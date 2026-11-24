using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.Common.Models;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Queries.GetContractsPaged;

public record GetContractsPagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    ContractStatus? Status = null,
    string? SearchTerm = null,
    Guid? CreatedById = null,
    Guid? AssignedApproverId = null,
    string? SortBy = "CreatedAtUtc",
    bool IsAscending = false
) : IRequest<PagedResult<ContractDto>>;

public class GetContractsPagedQueryValidator : AbstractValidator<GetContractsPagedQuery>
{
    public GetContractsPagedQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetContractsPagedQueryHandler : IRequestHandler<GetContractsPagedQuery, PagedResult<ContractDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public GetContractsPagedQueryHandler(
        IApplicationDbContext context,
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<PagedResult<ContractDto>> Handle(GetContractsPagedQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"contracts:paged:p{request.PageNumber}:s{request.PageSize}:st{request.Status}:q{request.SearchTerm?.Trim()}:u{request.CreatedById}:a{request.AssignedApproverId}:sort{request.SortBy}_{request.IsAscending}";

        var cached = await _cacheService.GetAsync<PagedResult<ContractDto>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var query = _context.Contracts
            .AsNoTracking()
            .Include(c => c.CreatedBy)
            .Include(c => c.AssignedApprover)
            .AsQueryable();

        // Filtering
        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(search) ||
                c.Description.ToLower().Contains(search) ||
                c.Currency.ToLower().Contains(search));
        }

        if (request.CreatedById.HasValue)
        {
            query = query.Where(c => c.CreatedById == request.CreatedById.Value);
        }

        if (request.AssignedApproverId.HasValue)
        {
            query = query.Where(c => c.AssignedApproverId == request.AssignedApproverId.Value);
        }

        // Sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "title" => request.IsAscending ? query.OrderBy(c => c.Title) : query.OrderByDescending(c => c.Title),
            "value" or "valueamount" => request.IsAscending ? query.OrderBy(c => c.ValueAmount) : query.OrderByDescending(c => c.ValueAmount),
            "status" => request.IsAscending ? query.OrderBy(c => c.Status) : query.OrderByDescending(c => c.Status),
            "validto" or "validtoutc" => request.IsAscending ? query.OrderBy(c => c.ValidToUtc) : query.OrderByDescending(c => c.ValidToUtc),
            _ => request.IsAscending ? query.OrderBy(c => c.CreatedAtUtc) : query.OrderByDescending(c => c.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ContractDto(
                c.Id,
                c.Title,
                c.Description,
                c.ValueAmount,
                c.Currency,
                c.Status,
                c.CreatedById,
                c.CreatedBy.FullName,
                c.AssignedApproverId,
                c.AssignedApprover != null ? c.AssignedApprover.FullName : null,
                c.ValidFromUtc,
                c.ValidToUtc,
                c.CreatedAtUtc,
                c.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ContractDto>(items, totalCount, request.PageNumber, request.PageSize);

        await _cacheService.SetAsync(
            cacheKey,
            result,
            slidingExpiration: TimeSpan.FromMinutes(5),
            cancellationToken: cancellationToken);

        return result;
    }
}
