namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>
/// Thrown when an expected entity cannot be found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityKey { get; }

    public EntityNotFoundException(string entityName, object entityKey)
        : base($"{entityName} with key '{entityKey}' was not found.")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }
}
