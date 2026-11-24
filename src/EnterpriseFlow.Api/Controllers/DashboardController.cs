using EnterpriseFlow.Application.Dashboard.Queries.GetDashboardStats;
using EnterpriseFlow.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseFlow.Api.Controllers;

[Authorize]
[SwaggerTag("Executive Dashboard & Workflow Analytics")]
public class DashboardController : ApiControllerBase
{
    /// <summary>
    /// Retrieve real-time contract counts by status and total active monetary value. Cached in Redis for 5 minutes.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var result = await Mediator.Send(new GetDashboardStatsQuery());
        return Ok(result);
    }
}
