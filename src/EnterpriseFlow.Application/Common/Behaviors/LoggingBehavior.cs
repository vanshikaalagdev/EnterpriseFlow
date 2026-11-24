using System.Diagnostics;
using EnterpriseFlow.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnterpriseFlow.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";
        var userRole = _currentUserService.Role?.ToString() ?? "None";

        _logger.LogInformation(
            "EnterpriseFlow Executing {RequestName} for User {UserId} ({UserRole})",
            requestName, userId, userRole);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "EnterpriseFlow Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) by User {UserId}",
                    requestName, stopwatch.ElapsedMilliseconds, userId);
            }
            else
            {
                _logger.LogInformation(
                    "EnterpriseFlow Executed {RequestName} in {ElapsedMilliseconds} ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "EnterpriseFlow Request Exception: {RequestName} failed after {ElapsedMilliseconds} ms for User {UserId}",
                requestName, stopwatch.ElapsedMilliseconds, userId);
            throw;
        }
    }
}
