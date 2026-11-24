using EnterpriseFlow.Application.Auth.Commands.Login;
using EnterpriseFlow.Application.Auth.Commands.RefreshToken;
using EnterpriseFlow.Application.Auth.Commands.RegisterUser;
using EnterpriseFlow.Application.Auth.Commands.RevokeToken;
using EnterpriseFlow.Application.Auth.Queries.GetCurrentUser;
using EnterpriseFlow.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EnterpriseFlow.Api.Controllers;

[SwaggerTag("Enterprise Authentication & Role-Based Access Control (RBAC)")]
public class AuthController : ApiControllerBase
{
    /// <summary>
    /// Register a new enterprise user.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Authenticate user credentials and return JWT Access and Refresh Tokens.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Rotate an expired JWT Access Token using a valid Refresh Token.
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Revoke an active Refresh Token.
    /// </summary>
    [HttpPost("revoke-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RevokeToken([FromBody] RevokeTokenCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(new { success = result, message = result ? "Token revoked successfully." : "Token was not found or already revoked." });
    }

    /// <summary>
    /// Get the profile of the currently authenticated enterprise user.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var result = await Mediator.Send(new GetCurrentUserQuery());
        return Ok(result);
    }
}
