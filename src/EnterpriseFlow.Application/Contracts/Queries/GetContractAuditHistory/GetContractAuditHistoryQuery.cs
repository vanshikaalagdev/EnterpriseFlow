using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Queries.GetContractAuditHistory;

public record GetContractAuditHistoryQuery(Guid ContractId) : IRequest<IReadOnlyCollection<ContractAuditLogDto>>;

public class GetContractAuditHistoryQueryHandler : IRequestHandler<GetContractAuditHistoryQuery, IReadOnlyCollection<ContractAuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetContractAuditHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ContractAuditLogDto>> Handle(GetContractAuditHistoryQuery request, CancellationToken cancellationToken)
    {
        var contractExists = await _context.Contracts
            .AnyAsync(c => c.Id == request.ContractId, cancellationToken);

        if (!contractExists)
        {
            throw new EntityNotFoundException("Contract", request.ContractId);
        }

        var logs = await _context.ContractAuditLogs
            .AsNoTracking()
            .Where(a => a.ContractId == request.ContractId)
            .OrderByDescending(a => a.TimestampUtc)
            .Select(a => new ContractAuditLogDto(
                a.Id,
                a.ContractId,
                a.ChangedById,
                a.ChangedByName,
                a.PreviousStatus,
                a.NewStatus,
                a.Comment,
                a.TimestampUtc
            ))
            .ToListAsync(cancellationToken);

        return logs;
    }
}
