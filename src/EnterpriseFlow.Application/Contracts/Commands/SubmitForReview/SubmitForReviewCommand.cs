using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Commands.SubmitForReview;

public record SubmitForReviewCommand(Guid Id, string? Comment = null) : IRequest<ContractDto>;

public class SubmitForReviewCommandValidator : AbstractValidator<SubmitForReviewCommand>
{
    public SubmitForReviewCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Contract ID is required.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class SubmitForReviewCommandHandler : IRequestHandler<SubmitForReviewCommand, ContractDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public SubmitForReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<ContractDto> Handle(SubmitForReviewCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user ID is required.");
        var currentUserName = _currentUserService.UserName ?? "Enterprise Requester";
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

        if (contract.CreatedById != currentUserId && currentUserRole != UserRole.Admin)
        {
            throw new UnauthorizedWorkflowActionException("Only the contract creator or an Admin can submit the contract for review.");
        }

        contract.SubmitForReview(currentUserId, currentUserName, request.Comment);

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
