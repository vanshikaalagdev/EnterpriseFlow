using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Commands.UpdateContract;

public record UpdateContractCommand(
    Guid Id,
    string Title,
    string Description,
    decimal ValueAmount,
    string Currency,
    DateTime ValidFromUtc,
    DateTime ValidToUtc,
    Guid? AssignedApproverId = null
) : IRequest<ContractDto>;

public class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
{
    public UpdateContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Contract ID is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ValueAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ValidToUtc).GreaterThan(x => x.ValidFromUtc);
    }
}

public class UpdateContractCommandHandler : IRequestHandler<UpdateContractCommand, ContractDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public UpdateContractCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<ContractDto> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user ID is required.");
        var currentUserName = _currentUserService.UserName ?? "Enterprise User";
        var currentUserRole = _currentUserService.Role;

        var contract = await _context.Contracts
            .Include(c => c.CreatedBy)
            .Include(c => c.AssignedApprover)
            .Include(c => c.AuditLogs)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (contract == null)
        {
            throw new EntityNotFoundException("Contract", request.Id);
        }

        // Only creator or Admin can edit draft/rejected contracts
        if (contract.CreatedById != currentUserId && currentUserRole != UserRole.Admin)
        {
            throw new UnauthorizedWorkflowActionException("Only the contract creator or an Admin can modify contract details.");
        }

        contract.UpdateDetails(
            request.Title,
            request.Description,
            request.ValueAmount,
            request.Currency,
            request.ValidFromUtc,
            request.ValidToUtc,
            request.AssignedApproverId,
            currentUserId,
            currentUserName
        );

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
            contract.CreatedBy?.FullName ?? currentUserName,
            contract.AssignedApproverId,
            contract.AssignedApprover?.FullName,
            contract.ValidFromUtc,
            contract.ValidToUtc,
            contract.CreatedAtUtc,
            contract.UpdatedAtUtc
        );
    }
}
