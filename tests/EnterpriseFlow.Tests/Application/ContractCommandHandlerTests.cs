using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.Contracts.Commands.ApproveContract;
using EnterpriseFlow.Application.Contracts.Commands.CreateContract;
using EnterpriseFlow.Application.Contracts.Commands.RejectContract;
using EnterpriseFlow.Application.Contracts.Commands.SubmitForReview;
using EnterpriseFlow.Application.Contracts.Queries.GetContractById;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EnterpriseFlow.Tests.Application;

public class ContractCommandHandlerTests
{
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _approverId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        // Seed basic users
        context.Users.AddRange(
            new User
            {
                Id = _requesterId,
                FullName = "Vanshika Requester",
                Email = "requester@test.internal",
                Role = UserRole.Requester,
                IsActive = true
            },
            new User
            {
                Id = _approverId,
                FullName = "Adv. Deshmukh",
                Email = "approver@test.internal",
                Role = UserRole.LegalApprover,
                IsActive = true
            }
        );

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task CreateContractCommand_ShouldPersistContract_AndInvalidateCache()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_requesterId);
        _currentUserServiceMock.Setup(s => s.UserName).Returns("Vanshika Requester");

        var handler = new CreateContractCommandHandler(context, _currentUserServiceMock.Object, _cacheServiceMock.Object);

        var command = new CreateContractCommand(
            Title: "Persistent Systems Talent SLA",
            Description: "Enterprise staffing",
            ValueAmount: 5000000m,
            Currency: "INR",
            ValidFromUtc: DateTime.UtcNow,
            ValidToUtc: DateTime.UtcNow.AddYears(1),
            AssignedApproverId: _approverId
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Persistent Systems Talent SLA");
        result.Status.Should().Be(ContractStatus.Draft);
        result.CreatedById.Should().Be(_requesterId);

        var savedInDb = await context.Contracts.FirstOrDefaultAsync(c => c.Id == result.Id);
        savedInDb.Should().NotBeNull();
        savedInDb!.AuditLogs.Should().HaveCount(1);

        _cacheServiceMock.Verify(c => c.RemoveByPrefixAsync("contracts:", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveByPrefixAsync("dashboard:", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitForReviewCommand_ShouldTransitionToPendingReview()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var contract = Contract.Create(
            "Draft Contract", "Desc", 1000m, "INR", DateTime.UtcNow, DateTime.UtcNow.AddMonths(6), _requesterId, "Vanshika");
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_requesterId);
        _currentUserServiceMock.Setup(s => s.UserName).Returns("Vanshika");
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRole.Requester);

        var handler = new SubmitForReviewCommandHandler(context, _currentUserServiceMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new SubmitForReviewCommand(contract.Id, "Ready for legal review"), CancellationToken.None);

        // Assert
        result.Status.Should().Be(ContractStatus.PendingReview);
        var inDb = await context.Contracts.Include(c => c.AuditLogs).FirstAsync(c => c.Id == contract.Id);
        inDb.Status.Should().Be(ContractStatus.PendingReview);
        inDb.AuditLogs.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApproveContractCommand_ShouldUpdateStatusToApproved_AndRecordAuditLog()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var contract = Contract.Create(
            "Review Contract", "Desc", 1000m, "INR", DateTime.UtcNow, DateTime.UtcNow.AddMonths(6), _requesterId, "Vanshika");
        contract.SubmitForReview(_requesterId, "Vanshika");
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_approverId);
        _currentUserServiceMock.Setup(s => s.UserName).Returns("Adv. Deshmukh");
        _currentUserServiceMock.Setup(s => s.Role).Returns(UserRole.LegalApprover);

        var handler = new ApproveContractCommandHandler(context, _currentUserServiceMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new ApproveContractCommand(contract.Id, "Legally compliant"), CancellationToken.None);

        // Assert
        result.Status.Should().Be(ContractStatus.Approved);
        var inDb = await context.Contracts.Include(c => c.AuditLogs).FirstAsync(c => c.Id == contract.Id);
        inDb.Status.Should().Be(ContractStatus.Approved);
        inDb.AuditLogs.Last().Comment.Should().Contain("Legally compliant");
    }

    [Fact]
    public async Task GetContractByIdQuery_WhenCacheHit_ShouldReturnCachedDataDirectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var contractId = Guid.NewGuid();
        var cachedDto = new ContractDetailDto(
            contractId,
            "Cached Contract",
            "From Redis Cache",
            9999m,
            "INR",
            ContractStatus.Active,
            _requesterId,
            "Requester",
            "req@internal.com",
            _approverId,
            "Approver",
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1),
            DateTime.UtcNow,
            null,
            Array.Empty<ContractAuditLogDto>()
        );

        _cacheServiceMock
            .Setup(c => c.GetAsync<ContractDetailDto>($"contracts:id:{contractId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        var handler = new GetContractByIdQueryHandler(context, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetContractByIdQuery(contractId), CancellationToken.None);

        // Assert
        result.Should().Be(cachedDto);
        result.Title.Should().Be("Cached Contract");
        // Verify DB was NOT touched!
        context.Contracts.Should().BeEmpty();
    }
}
