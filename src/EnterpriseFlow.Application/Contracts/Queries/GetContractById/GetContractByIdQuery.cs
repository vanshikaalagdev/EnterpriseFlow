using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Queries.GetContractById;

public record GetContractByIdQuery(Guid Id) : IRequest<ContractDetailDto>;

public class GetContractByIdQueryHandler : IRequestHandler<GetContractByIdQuery, ContractDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public GetContractByIdQueryHandler(
        IApplicationDbContext context,
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<ContractDetailDto> Handle(GetContractByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"contracts:id:{request.Id}";

        var cached = await _cacheService.GetAsync<ContractDetailDto>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var contract = await _context.Contracts
            .AsNoTracking()
            .Include(c => c.CreatedBy)
            .Include(c => c.AssignedApprover)
            .Include(c => c.AuditLogs.OrderByDescending(a => a.TimestampUtc))
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (contract == null)
        {
            throw new EntityNotFoundException("Contract", request.Id);
        }

        var auditLogs = contract.AuditLogs.Select(a => new ContractAuditLogDto(
            a.Id,
            a.ContractId,
            a.ChangedById,
            a.ChangedByName,
            a.PreviousStatus,
            a.NewStatus,
            a.Comment,
            a.TimestampUtc
        )).ToList();

        var dto = new ContractDetailDto(
            contract.Id,
            contract.Title,
            contract.Description,
            contract.ValueAmount,
            contract.Currency,
            contract.Status,
            contract.CreatedById,
            contract.CreatedBy?.FullName ?? "Requester",
            contract.CreatedBy?.Email ?? string.Empty,
            contract.AssignedApproverId,
            contract.AssignedApprover?.FullName,
            contract.ValidFromUtc,
            contract.ValidToUtc,
            contract.CreatedAtUtc,
            contract.UpdatedAtUtc,
            auditLogs
        );

        await _cacheService.SetAsync(
            cacheKey,
            dto,
            slidingExpiration: TimeSpan.FromMinutes(5),
            cancellationToken: cancellationToken);

        return dto;
    }
}
