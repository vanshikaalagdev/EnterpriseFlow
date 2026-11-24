using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Commands.ActivateContract;

public record ActivateContractCommand(Guid Id, string? Comment = null) : IRequest<ContractDto>;

public class ActivateContractCommandValidator : AbstractValidator<ActivateContractCommand>
{
    public ActivateContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Contract ID is required.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class ActivateContractCommandHandler : IRequestHandler<ActivateContractCommand, ContractDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ActivateContractCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ContractDto> Handle(ActivateContractCommand request, CancellationToken cancellationToken)
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

        // Domain activation
        contract.Activate(currentUserId, currentUserName, currentUserRole, _dateTimeProvider.UtcNow, request.Comment);

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
