using EnterpriseFlow.Application.Common.Models;
using EnterpriseFlow.Application.Contracts.Commands.CreateContract;
using EnterpriseFlow.Application.Contracts.Commands.UpdateContract;
using EnterpriseFlow.Application.Contracts.Queries.GetContractById;
using EnterpriseFlow.Application.Contracts.Queries.GetContractsPaged;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseFlow.Api.Controllers;

[Authorize]
[SwaggerTag("Enterprise Contract Lifecycle & CRUD Operations")]
public class ContractsController : ApiControllerBase
{
    /// <summary>
    /// Retrieve paginated contracts with filtering by status, search term, approver, and dynamic sorting. High-speed Redis cached.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<ContractDto>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ContractStatus? status = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? createdById = null,
        [FromQuery] Guid? assignedApproverId = null,
        [FromQuery] string? sortBy = "CreatedAtUtc",
        [FromQuery] bool isAscending = false)
    {
        var query = new GetContractsPagedQuery(
            pageNumber,
            pageSize,
            status,
            searchTerm,
            createdById,
            assignedApproverId,
            sortBy,
            isAscending);

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve full contract details including immutable audit logs. High-speed Redis cached.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContractDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractDetailDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetContractByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Create a new enterprise contract in Draft status. Restricted to Requesters and Admins.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Requester,Admin")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ContractDto>> Create([FromBody] CreateContractCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update contract editable fields while in Draft or Rejected status.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Update(Guid id, [FromBody] UpdateContractCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("URL route ID and request body ID do not match.");
        }

        var result = await Mediator.Send(command);
        return Ok(result);
    }
}
