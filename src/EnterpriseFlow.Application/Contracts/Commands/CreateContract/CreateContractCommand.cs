using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Contracts.Commands.CreateContract;

public record CreateContractCommand(
    string Title,
    string Description,
    decimal ValueAmount,
    string Currency,
    DateTime ValidFromUtc,
    DateTime ValidToUtc,
    Guid? AssignedApproverId = null
) : IRequest<ContractDto>;

public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.ValueAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Value amount must be non-negative.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g., INR, USD, EUR).");

        RuleFor(x => x.ValidToUtc)
            .GreaterThan(x => x.ValidFromUtc)
            .WithMessage("ValidToUtc must be later than ValidFromUtc.");
    }
}

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, ContractDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public CreateContractCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<ContractDto> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user ID is required to create a contract.");

        var currentUserName = _currentUserService.UserName ?? "Enterprise Requester";

        if (request.AssignedApproverId.HasValue)
        {
            var approverExists = await _context.Users
                .AnyAsync(u => u.Id == request.AssignedApproverId.Value, cancellationToken);
            if (!approverExists)
            {
                throw new ArgumentException($"Assigned approver with ID '{request.AssignedApproverId}' does not exist.");
            }
        }

        var contract = Contract.Create(
            request.Title,
            request.Description,
            request.ValueAmount,
            request.Currency,
            request.ValidFromUtc,
            request.ValidToUtc,
            currentUserId,
            currentUserName,
            request.AssignedApproverId
        );

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveByPrefixAsync("contracts:", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("dashboard:", cancellationToken);

        string? approverName = null;
        if (contract.AssignedApproverId.HasValue)
        {
            approverName = await _context.Users
                .Where(u => u.Id == contract.AssignedApproverId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ContractDto(
            contract.Id,
            contract.Title,
            contract.Description,
            contract.ValueAmount,
            contract.Currency,
            contract.Status,
            contract.CreatedById,
            currentUserName,
            contract.AssignedApproverId,
            approverName,
            contract.ValidFromUtc,
            contract.ValidToUtc,
            contract.CreatedAtUtc,
            contract.UpdatedAtUtc
        );
    }
}
