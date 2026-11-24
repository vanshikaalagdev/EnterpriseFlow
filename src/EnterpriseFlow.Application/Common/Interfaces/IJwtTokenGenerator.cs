using System.Security.Claims;
using EnterpriseFlow.Domain.Entities;

namespace EnterpriseFlow.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string token, string jwtId, DateTime expiresAtUtc) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
