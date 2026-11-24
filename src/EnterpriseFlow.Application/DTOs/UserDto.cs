using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Application.DTOs;

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAtUtc
);
