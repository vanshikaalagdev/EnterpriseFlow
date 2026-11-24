using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>
/// Thrown when an actor attempts an action without the required enterprise role or authorization.
/// </summary>
public class UnauthorizedWorkflowActionException : DomainException
{
    public UnauthorizedWorkflowActionException(string action, UserRole actorRole, string requiredRoles)
        : base($"User with role '{actorRole}' is not authorized to perform workflow action '{action}'. Required role(s): {requiredRoles}.")
    {
    }

    public UnauthorizedWorkflowActionException(string message) : base(message)
    {
    }
}
