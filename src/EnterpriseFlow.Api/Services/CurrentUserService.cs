using System.Security.Claims;
using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(idClaim, out var parsedId) ? parsedId : null;
        }
    }

    public string? UserEmail => User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email);

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);

    public UserRole? Role
    {
        get
        {
            var roleClaim = User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
