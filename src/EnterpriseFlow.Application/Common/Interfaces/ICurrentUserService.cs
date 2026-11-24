using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? UserName { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
