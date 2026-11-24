using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Commands.TerminateContract;

public record TerminateContractCommand(Guid Id, string Reason) : IRequest<ContractDto>;

public class TerminateContractCommandValidator : AbstractValidator<TerminateContractCommand>
{
    public TerminateContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Contract ID is required.");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Termination reason is required.")
            .MaximumLength(1000).WithMessage("Termination reason cannot exceed 1000 characters.");
    }
}

public class TerminateContractCommandHandler : IRequestHandler<TerminateContractCommand, ContractDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public TerminateContractCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<ContractDto> Handle(TerminateContractCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user ID is required.");
        var currentUserName = _currentUserService.UserName ?? "Enterprise Operator";
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

        contract.Terminate(currentUserId, currentUserName, currentUserRole, request.Reason);

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
            contract.AssignedApprover?.FullName,
            contract.ValidFromUtc,
            contract.ValidToUtc,
            contract.CreatedAtUtc,
            contract.UpdatedAtUtc
        );
    }
}
