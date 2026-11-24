using EnterpriseFlow.Application.Contracts.Commands.ActivateContract;
using EnterpriseFlow.Application.Contracts.Commands.ApproveContract;
using EnterpriseFlow.Application.Contracts.Commands.RejectContract;
using EnterpriseFlow.Application.Contracts.Commands.SubmitForReview;
using EnterpriseFlow.Application.Contracts.Commands.TerminateContract;
using EnterpriseFlow.Application.Contracts.Queries.GetContractAuditHistory;
using EnterpriseFlow.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseFlow.Api.Controllers;

[Authorize]
[Route("api/contracts/{id:guid}")]
[SwaggerTag("Deterministic Workflow State Transitions & Audit Trail")]
public class WorkflowController : ApiControllerBase
{
    public record WorkflowCommentRequest(string? Comment);
    public record WorkflowReasonRequest(string Reason);

    /// <summary>
    /// Transition contract from Draft or Rejected to PendingReview.
    /// </summary>
    [HttpPost("submit-review")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> SubmitForReview(Guid id, [FromBody] WorkflowCommentRequest? request)
    {
        var result = await Mediator.Send(new SubmitForReviewCommand(id, request?.Comment));
        return Ok(result);
    }

    /// <summary>
    /// Legally approve contract review. Restricted to LegalApprovers and Admins.
    /// </summary>
    [HttpPost("approve")]
    [Authorize(Roles = "LegalApprover,Admin")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Approve(Guid id, [FromBody] WorkflowCommentRequest? request)
    {
        var result = await Mediator.Send(new ApproveContractCommand(id, request?.Comment));
        return Ok(result);
    }

    /// <summary>
    /// Reject contract review with mandatory compliance or legal reason. Restricted to LegalApprovers and Admins.
    /// </summary>
    [HttpPost("reject")]
    [Authorize(Roles = "LegalApprover,Admin")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Reject(Guid id, [FromBody] WorkflowReasonRequest request)
    {
        var result = await Mediator.Send(new RejectContractCommand(id, request.Reason));
        return Ok(result);
    }

    /// <summary>
    /// Activate an approved contract into operational Active status.
    /// </summary>
    [HttpPost("activate")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Activate(Guid id, [FromBody] WorkflowCommentRequest? request)
    {
        var result = await Mediator.Send(new ActivateContractCommand(id, request?.Comment));
        return Ok(result);
    }

    /// <summary>
    /// Terminate contract early with mandatory audit reason.
    /// </summary>
    [HttpPost("terminate")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Terminate(Guid id, [FromBody] WorkflowReasonRequest request)
    {
        var result = await Mediator.Send(new TerminateContractCommand(id, request.Reason));
        return Ok(result);
    }

    /// <summary>
    /// Retrieve the complete immutable audit trail history for a contract.
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ContractAuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ContractAuditLogDto>>> GetAuditLogs(Guid id)
    {
        var result = await Mediator.Send(new GetContractAuditHistoryQuery(id));
        return Ok(result);
    }
}
