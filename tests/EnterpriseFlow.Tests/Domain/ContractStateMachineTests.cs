using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EnterpriseFlow.Tests.Domain;

public class ContractStateMachineTests
{
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _approverId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldInitializeInDraftStatus_AndRecordInitialAuditLog()
    {
        // Act
        var contract = Contract.Create(
            title: "Master Services Agreement",
            description: "Enterprise software services",
            valueAmount: 1500000m,
            currency: "INR",
            validFromUtc: DateTime.UtcNow,
            validToUtc: DateTime.UtcNow.AddYears(1),
            createdById: _requesterId,
            createdByName: "Requester User",
            assignedApproverId: _approverId
        );

        // Assert
        contract.Status.Should().Be(ContractStatus.Draft);
        contract.AuditLogs.Should().HaveCount(1);
        var audit = contract.AuditLogs.First();
        audit.PreviousStatus.Should().Be(ContractStatus.Draft);
        audit.NewStatus.Should().Be(ContractStatus.Draft);
        audit.ChangedById.Should().Be(_requesterId);
        audit.Comment.Should().Contain("created");
    }

    [Fact]
    public void SubmitForReview_FromDraft_ShouldTransitionToPendingReview()
    {
        // Arrange
        var contract = CreateSampleDraftContract();

        // Act
        contract.SubmitForReview(_requesterId, "Requester User", "Please review and approve.");

        // Assert
        contract.Status.Should().Be(ContractStatus.PendingReview);
        contract.AuditLogs.Should().HaveCount(2);
        var latestLog = contract.AuditLogs.Last();
        latestLog.PreviousStatus.Should().Be(ContractStatus.Draft);
        latestLog.NewStatus.Should().Be(ContractStatus.PendingReview);
    }

    [Fact]
    public void SubmitForReview_FromActive_ShouldThrowInvalidStateTransitionException()
    {
        // Arrange
        var contract = CreateSampleDraftContract();
        contract.SubmitForReview(_requesterId, "Requester");
        contract.Approve(_approverId, "Approver", UserRole.LegalApprover);
        contract.Activate(_adminId, "Admin", UserRole.Admin, DateTime.UtcNow);

        // Act
        Action act = () => contract.SubmitForReview(_requesterId, "Requester");

        // Assert
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*'Active' to 'PendingReview'*");
    }

    [Fact]
    public void Approve_ByLegalApprover_ShouldTransitionToApproved()
    {
        // Arrange
        var contract = CreateSampleDraftContract();
        contract.SubmitForReview(_requesterId, "Requester");

        // Act
        contract.Approve(_approverId, "Legal Approver", UserRole.LegalApprover, "Terms verified.");

        // Assert
        contract.Status.Should().Be(ContractStatus.Approved);
        contract.AuditLogs.Should().HaveCount(3);
        var latestLog = contract.AuditLogs.Last();
        latestLog.PreviousStatus.Should().Be(ContractStatus.PendingReview);
        latestLog.NewStatus.Should().Be(ContractStatus.Approved);
        latestLog.ChangedById.Should().Be(_approverId);
    }

    [Fact]
    public void Approve_ByRequester_ShouldThrowUnauthorizedWorkflowActionException()
    {
        // Arrange
        var contract = CreateSampleDraftContract();
        contract.SubmitForReview(_requesterId, "Requester");

        // Act
        Action act = () => contract.Approve(_requesterId, "Requester User", UserRole.Requester);

        // Assert
        act.Should().Throw<UnauthorizedWorkflowActionException>()
            .WithMessage("*Requester*is not authorized*");
    }

    [Fact]
    public void Reject_ByLegalApprover_ShouldTransitionToRejected()
    {
        // Arrange
        var contract = CreateSampleDraftContract();
        contract.SubmitForReview(_requesterId, "Requester");

        // Act
        contract.Reject(_approverId, "Legal Approver", UserRole.LegalApprover, "Liability cap exceeds policy threshold.");

        // Assert
        contract.Status.Should().Be(ContractStatus.Rejected);
        var latestLog = contract.AuditLogs.Last();
        latestLog.PreviousStatus.Should().Be(ContractStatus.PendingReview);
        latestLog.NewStatus.Should().Be(ContractStatus.Rejected);
        latestLog.Comment.Should().Contain("Liability cap exceeds");
    }

    [Fact]
    public void Reject_WithoutReason_ShouldThrowArgumentException()
    {
        // Arrange
        var contract = CreateSampleDraftContract();
        contract.SubmitForReview(_requesterId, "Requester");

        // Act
        Action act = () => contract.Reject(_approverId, "Legal Approver", UserRole.LegalApprover, "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*rejection reason must be provided*");
    }

    [Fact]
    public void Activate_FromApproved_ShouldTransitionToActive()
    {
        // Arrange
        var contract = CreateSampleDraftContract();
        contract.SubmitForReview(_requesterId, "Requester");
        contract.Approve(_approverId, "Legal Approver", UserRole.LegalApprover);

        // Act
        contract.Activate(_adminId, "Admin User", UserRole.Admin, DateTime.UtcNow, "Contract fully executed.");

        // Assert
        contract.Status.Should().Be(ContractStatus.Active);
        var latestLog = contract.AuditLogs.Last();
        latestLog.PreviousStatus.Should().Be(ContractStatus.Approved);
        latestLog.NewStatus.Should().Be(ContractStatus.Active);
    }

    [Fact]
    public void Activate_WhenValidityExpired_ShouldThrowInvalidStateTransitionException()
    {
        // Arrange
        var contract = Contract.Create(
            title: "Past Agreement",
            description: "Test description",
            valueAmount: 1000m,
            currency: "INR",
            validFromUtc: DateTime.UtcNow.AddDays(-60),
            validToUtc: DateTime.UtcNow.AddDays(-10),
            createdById: _requesterId,
            createdByName: "Requester"
        );
        contract.SubmitForReview(_requesterId, "Requester");
        contract.Approve(_approverId, "Approver", UserRole.LegalApprover);

        // Act
        Action act = () => contract.Activate(_adminId, "Admin", UserRole.Admin, DateTime.UtcNow);

        // Assert
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*Cannot activate expired contract*");
    }

    [Fact]
    public void Terminate_ByAdmin_ShouldTransitionToTerminated()
    {
        // Arrange
        var contract = CreateSampleDraftContract();

        // Act
        contract.Terminate(_adminId, "Admin User", UserRole.Admin, "Project cancelled by client.");

        // Assert
        contract.Status.Should().Be(ContractStatus.Terminated);
        var latestLog = contract.AuditLogs.Last();
        latestLog.NewStatus.Should().Be(ContractStatus.Terminated);
        latestLog.Comment.Should().Contain("Project cancelled");
    }

    [Fact]
    public void CheckAndExpire_WhenValidityPassed_ShouldTransitionToExpired()
    {
        // Arrange
        var contract = Contract.Create(
            title: "Expiring Agreement",
            description: "Test",
            valueAmount: 10000m,
            currency: "INR",
            validFromUtc: DateTime.UtcNow.AddDays(-40),
            validToUtc: DateTime.UtcNow.AddDays(-5),
            createdById: _requesterId,
            createdByName: "Requester"
        );
        contract.SubmitForReview(_requesterId, "Requester");
        contract.Approve(_approverId, "Approver", UserRole.LegalApprover);
        // Force set status to active for expiration check test
        contract.Status = ContractStatus.Active;

        // Act
        var expired = contract.CheckAndExpire(DateTime.UtcNow);

        // Assert
        expired.Should().BeTrue();
        contract.Status.Should().Be(ContractStatus.Expired);
    }

    private Contract CreateSampleDraftContract()
    {
        return Contract.Create(
            title: "Standard SLA Agreement",
            description: "Enterprise 99.99% uptime guarantee",
            valueAmount: 2500000m,
            currency: "INR",
            validFromUtc: DateTime.UtcNow,
            validToUtc: DateTime.UtcNow.AddYears(1),
            createdById: _requesterId,
            createdByName: "Requester User",
            assignedApproverId: _approverId
        );
    }
}
