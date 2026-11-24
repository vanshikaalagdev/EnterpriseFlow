using EnterpriseFlow.Application.Common.Interfaces;

namespace EnterpriseFlow.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
