using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Commands.RejectContract;

public record RejectContractCommand(Guid Id, string Reason) : IRequest<ContractDto>;

public class RejectContractCommandValidator : AbstractValidator<RejectContractCommand>
{
    public RejectContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Contract ID is required.");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Rejection reason cannot exceed 1000 characters.");
    }
}

public class RejectContractCommandHandler : IRequestHandler<RejectContractCommand, ContractDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public RejectContractCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<ContractDto> Handle(RejectContractCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user ID is required.");
        var currentUserName = _currentUserService.UserName ?? "Legal Approver";
        var currentUserRole = _currentUserService.Role
            ?? throw new UnauthorizedAccessException("User role is required.");

        var contract = await _context.Contracts
            .Include(c => c.CreatedBy)
            .Include(c => c.AssignedApprover)
            .Include(c => c.AuditLogs)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (contract == null)
        {
            throw new EntityNotFoundException("Contract", request.Id);
        }

        // Domain guard clause: only LegalApprover or Admin
        contract.Reject(currentUserId, currentUserName, currentUserRole, request.Reason);

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPrefixAsync("contracts:", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("dashboard:", cancellationToken);

        return new ContractDto(
            contract.Id,
            contract.Title,
            contract.Description,
            contract.ValueAmount,
            contract.Currency,
            contract.Status,
            contract.CreatedById,
            contract.CreatedBy?.FullName ?? "Requester",
            contract.AssignedApproverId,
            currentUserName,
            contract.ValidFromUtc,
            contract.ValidToUtc,
            contract.CreatedAtUtc,
            contract.UpdatedAtUtc
        );
    }
}
